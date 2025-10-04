using B040.Services.Models;
using B040.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using B040.Authentication.Models;
using Mg.Services;
using B040.Services.Cruds;
using System.Web.Http.Results;
using System.Web.Http.ExceptionHandling;
using System.Web.Compilation;
using Serilog;
using B040.Services.Enums;
using B040.Services.Utilities;
using System.Data.Entity.ModelConfiguration.Configuration;

namespace B040.Authentication.Controllers
{
	[Authorize]
	[RoutePrefix("api/B040")]
	public class B040Controller : ApiController
	{
        [AllowAnonymous]
        [HttpGet]
        [Route("GetAllArtikelsFromWebAccountId")]
        public List<ArtikelModel> GetAllArtikelsFromWebAccountId(string webAccountId)
        {
            var _b040 = DataAccessB040.GetInstance();
            return _b040.GetArtikelsFromWebAccountId(webAccountId);
        }
        [AllowAnonymous]
        [HttpGet]
        [Route("GetAllActiveWebArticles")]
        public List<ArtikelModel> GetAllActiveWebArticles()
        {
            var _b040 = DataAccessB040.GetInstance();
			var t = _b040.GetAllActiveWebArticles();
			return t;
        }
        [AllowAnonymous]
		[HttpGet]
		[Route("GetArtikelXDtoSharedFromId")]
		public ArtikelXDtoShared GetArtikelXDtoSharedFromId(string idString)
		{
			int id = int.Parse(idString);
			var _b040 = DataAccessB040.GetInstance();
			return _b040.GetArtikelXDtoSharedFromId(id);
		}
		[AllowAnonymous]
		[HttpPost]
		[Route("GetWebOrder")]
		public WebOrderDtoShared GetWebOrder(WebOrderParametersModel wp)
		{
			Log.Warning("GetWebOrder");
			Log.Warning($"[{wp.WebAccountId}], {wp.Date.ToString("dd-MMM-yy")}");
			string webAccountId = wp.WebAccountId;
			DateTime date = wp.Date;
			string standardCode= wp.StandardCode;
			var dtoTask = new TaskCompletionSource<WebOrderDtoShared>();
			var dto = new WebOrderDtoShared();
			var _b040 = DataAccessB040.GetInstance();
			var customer = _b040.GetWebCustomerByWebAccountId(webAccountId);
			if (customer == null)
			{
				dto.Success = false;
				dto.Message = $"{webAccountId} is ongeldig.";
				return dto;
			}
			dto.CustomerName = customer.KL_Naam;
			dto.DayOfWeekInDutch = date.ToDutchDayName();
			var bestelHeader = _b040.GetOrderHeaderByCustomerAndDate(customer.KL_ID, date);
            Log.Warning($"[{customer.KL_Nummer},{wp.WebAccountId}], {wp.Date.ToString("dd-MMM-yy")}");
            // 6317.09 Adjust Access Restriction and Enforce Web Order Checkes
            var isRestricted = _b040.IsCustomerAccessRestricted(customer.KL_ID);
            if (isRestricted.Data)
            {
                dto.Success = false;
                dto.Message = $"Uw toegang werd beperkt.";
                return dto;
            }
            int orderId = bestelHeader?.BestH_Id ?? 0;
			if (orderId == 0)
			{
				dto.Success = false;
				dto.Message = $"Uw bestelling voor {date.ToString("dd/MMM/yyyy")} ({dto.DayOfWeekInDutch}) kan nog niet worden aangepast.";
				return dto;
			}

			//
            var q = _b040.GetOrderById(orderId);
			bool locked = new LockService().Lock(0,"BestH", orderId);
			if (locked==false)
			{
				dto.Success = false;
				dto.Message = "Deze bestelling is vergrendeld.";
				return dto;
			}
			dto.Repository = q.CastingList<WebOrderDtoDetailShared, BestDModelX>();
			dto.BestH_Id = orderId;
			dto.Info = bestelHeader.BestH_Info;
			dto.InProduction = bestelHeader.BestH_InProduction ?? false;
			return dto;
		}
		[AllowAnonymous]
		[HttpPost]
		[Route("GetNextDeliveryDate")]
		public DateTime GetNextDeliveryDate()
		{
			var d  =  DataAccessB040.GetInstance().GetNextDeliveryDate();
            return d;
		}
		[AllowAnonymous]
		[HttpPost]
		[Route("UpdateWebOrder")]
		public OpResult UpdateWebOrder(WebOrderDtoShared dto)
		{
            // 6301.07 Web Article Management
            // create a list of the artikels for which we need a notification report
            // 6305.01 Notification Management (revisited)
            // orderlines with quantity = 0 are not considered "to be notified"
            // 6323.01 – Remove Async from Current API
            List<WebOrderDtoDetailShared> weborderdetailsToNotify = new List<WebOrderDtoDetailShared>();
			try
			{
				weborderdetailsToNotify =dto.Repository
					.Where(x => (x.Art_Notify == true) && (x.BestD_Notified == false) && (x.BestD_Hoev1 != 0))
					.ToList();
            }
            catch (Exception ex)
			{
				Serilog.Log.Warning($"Articles to Notify: {ex.Message}");
			}
            Serilog.Log.Warning($"UpdateWebOrder {dto.BestH_Id}");
            BestHModel bH;
			var b040Db = DataAccessB040.GetInstance();
			// modB040Config.lb040Config();
			var opResult = new OpResult();
			int typFId = 1;
			try
			{
				// bH = await Task<BestHModel>.Run(() => b040Db.GetBestHFromId(dto.BestH_Id, typFId));
				bH = b040Db.GetBestHFromId(dto.BestH_Id, typFId);
                Serilog.Log.Warning($"==> GetBestHFromId {bH.BestH_Id}");

                double totalTeBetalen = ComputeTeBetalen();
                Serilog.Log.Warning($"==> Compute Te Betalen {totalTeBetalen:n2}");
                double ComputeTeBetalen()
				{
					KlantenModel k = b040Db.GetKlantenById(bH.BestH_Klant ?? 0);
					var p = new PriceService(k.KL_ID);
					double sumTeBetalen = 0;
					foreach (var d in dto.Repository)
					{
						var toPay = p.GetPriceFromExtended(d.BestD_Artikel ?? 0, ((decimal)d.BestD_Waarde)).ToPay;
						sumTeBetalen += (double) toPay;
					}
					return sumTeBetalen;
				}
				int totalLines = dto.Repository.Count();
				bH.besth_totLijnen = totalLines;
				bH.besth_totTebetalen = totalTeBetalen;
				bH.BestH_Info = dto.Info;
				var cruds = new Cruds(b040Db.GetConnection());
				using (var t = DataAccessB040.BeginTransaction())
				{
					try
					{
						Serilog.Log.Warning("Id:{id}",bH.BestH_Id);
						cruds.UpdateBestH(bH, t);
                        Serilog.Log.Warning($"==> UpdateBestH {bH.BestH_Id}");
                        // B040 6298.4 delete detail lines.
                        cruds.DeteteBestDByBestH_Id(dto.BestH_Id, t);
                        Serilog.Log.Warning($"==> DeleteBestDByBEstH_Id {dto.BestH_Id}");
                        foreach (var l in dto.Repository)
						{
							// Serilog.Log.Warning("Orderline {l}", l.BestD_Omschrijving);
							BestDModel bD = l.Casting<BestDModel>();
							// Serilog.Log.Warning("==> Casted ... ");
							// if (bD.BestD_ID == 0) 
							// B040 6296.3 add header id to added orderline
							bD.BestD_ID = 0;
                            bD.BestD_BestH = dto.BestH_Id;
                            // 6301.07 Web Article Management
                            if (weborderdetailsToNotify.Any(x => x == l))
							{
								Serilog.Log.Warning("==> Notified set");
								bD.BestD_Notified = true; 
							}
                            // Serilog.Log.Warning("==> Inserting ... ");
                            cruds.InsertBestD(bD, t);
                            // Serilog.Log.Warning("==> OK");

                        }
                        var log = new B040.Services.Cruds.CrudModels.SaveWebOrderLogModel()
                        {
                            Sw_Client = bH.BestH_Klant ?? 0,
                            Sw_Station = B040.Services.ConfigurationHelper.Get(ConfigurationEnums.MACHINENAME),
                            Sw_Date = DateTime.Now,
                            Sw_Time = DateTime.Now.ToString("HH:mm")
                        };
						var r = cruds.InsertSaveWebOrderLog(log, t);
                        t.Commit();
                    }
                    catch (Exception ex)
					{
						t.Rollback();
						string msg = $"UpdateWebOrder Rolled Back. ({dto.CustomerName})";
						Serilog.Log.Error(msg);
						opResult.Fail(msg);
						opResult.Fail(ex.Message);
					}
				}
				if (opResult.Success)
				{
					Serilog.Log.Warning("Update succeeded");
                    var logService = new LogService();
                    logService.LogDetailed(dto.CustomerName, "UpdateWebOrder", LogService.LogType.Normal, LogService.LogAction.Update, cruds.GetBestHTableName(), dto.BestH_Id);
                    var lockService = new LockService();
					lockService.Unlock(cruds.GetBestHTableName(), dto.BestH_Id);
                    b040Db.InsertOrderAuditTrail(b040Db.GetOrderDocnr(dto.BestH_Id), "Web");
                }
            }
			catch (Exception ex)
			{
				opResult.Fail(ex.Message);
			}
			if (opResult.Success==false)
			{
				Serilog.Log.Warning(opResult.Message);
				return opResult;
			}
            // 6301.07 Web Article Management
            if (weborderdetailsToNotify.Count()>0)
			{
				try
				{
					opResult = ReportWebOrderNotifications(dto.BestH_Id);
                }
                catch (Exception )
				{

				}
			};
            return opResult;
            // 6301.07 Web Article Management
            OpResult ReportWebOrderNotifications(int bestHId, List<int> artikelIds = null)
            {
                opResult = new OpResult();
                UitzonderlijkDocumentInfoModel info = b040Db.GetUitzonderlijkDocumentInfo(bestHId);
				var parameters = new NotificationService.UitzonderlijkDocumentVariabelen();
                parameters.telefoon = $"Tel: {info.Adr_Telefoon}";
                parameters.komthalen = info.BestH_KomtHalen ? "Komt Halen" : "Sturen";
                parameters.klant_naam = info.Kl_Naam;
                parameters.adres = info.Adr_Adres;
                parameters.klantNummer = $"Klant {info.KL_Nummer}";
                Serilog.Log.Warning($"Notifying {info.Kl_Naam}");
                foreach (var n in weborderdetailsToNotify)
                {
                    parameters.tour = n.BestD_Tour;
                    parameters.hoeveelheid = n.BestD_Hoev1.ToString();
                    parameters.artikel_omschrijving = n.BestD_Omschrijving;
                    parameters.voorafdrukken = "Web";
                    parameters.artikel = n.BestD_Artikel ?? 0;
                    var notificationService = new NotificationService();
                    parameters.postnummer_en_gemeente = (string)notificationService.FormatPostnummerAdres(info.Adr_PostNummer, info.Adr_Gemeente);
                    parameters.datum_levering = notificationService.FormatDate(info.BestH_DatLevering);
					var opschrift = (n.BestD_Opschrift == string.Empty) ? string.Empty : $"Opschrift: {n.BestD_Opschrift}";
                    parameters.info = ConcatenateWithNewLine(info.BestH_Info, opschrift);

                    notificationService.Dispatch(parameters);
                    Serilog.Log.Warning($"==> {parameters.artikel_omschrijving} notified.");
                }
                return opResult;
                string ConcatenateWithNewLine(string str1, string str2)
                {
                    if (string.IsNullOrEmpty(str1))
                    {
                        return str2 ?? string.Empty; // Returns str2 if str1 is empty, but if str2 is also null, it returns an empty string.
                    }
                    if (string.IsNullOrEmpty(str2))
                    {
                        return str1; // Returns str1 if str2 is empty.
                    }
                    return $"{str1}\n{str2}"; // Concatenates str1 and str2 with a newline if neither is empty.
                }
            }
        }
		[AllowAnonymous]
		[HttpPost]
		[Route("LockWebOrder")]
		public OpResult LockWebOrder(LockModel m)
		{
			var or = new OpResult();
			m.Lock_Session = 0;
			m.Lock_Description = "Web";
			or.Success = new LockService().Lock(m);
			if (or.Success == false)
			{
				or.Fail("De vergrendeling is mislukt.");
                Serilog.Log.Warning(or.Message);
            }
			return or;
		}
		[AllowAnonymous]
		[HttpPost]
		[Route("UnlockWebOrder")]
		public OpResult UnlockWebOrder(LockModel m)
		{
			var or = new OpResult();
			new LockService().Unlock(m.Lock_table, m.Lock_LockedPK ?? 0);
			return or;
		}
        [AllowAnonymous]
        [HttpPost]
        [Route("UnlockWebOrdersFromWebAccountId")]
        public void UnlockWebOrdersFromWebAccountId(WebOrderParametersModel wp)
		{
            var b040Db = DataAccessB040.GetInstance();
			b040Db.UnlockFromWebAccountId(wp.WebAccountId);
        }
        /// <summary>
        /// Quick and dirty implementation for prelaunching purposes.   Customer does not access use the automatisch bestellen process.
        /// </summary>
        /// <param name="wp"></param>
        /// <returns></returns>
        /// Commented out 2024-06-10 as not used anymore and depending on b040 (vb).
		//[AllowAnonymous]
        //[HttpPost]
        //[Route("GenerateWebOrderFromStandards")]
        //      public async Task<int> GenerateWebOrderFromStandards(WebOrderParametersModel wp)
        //      {
        //          modB040Config.lb040Config();

        //	Log.Warning("Api - Create Web Order From Standard requested.");
        //          string webAccountId = wp.WebAccountId;
        //          DateTime date = wp.Date;
        //          string standardCode = wp.StandardCode ?? "1";
        //          string dayOfWeekInDutch = modDutch.cDagInDeWeek(date);
        //          var _b040 = DataAccessB040.GetInstance();
        //          var customer = await _b040.GetWebCustomerByWebAccountId(webAccountId);
        //	if (customer == null) {
        //              Log.Warning("  ==> No such customer.");
        //              return 0; }
        //          Log.Warning($"  ==> Getting order for {customer.KL_ID} on {date.ToString("dd/MMM/yyyy)")}.","API");
        //          var orderId = await _b040.GetOrderIdByCustomerAndDate(customer.KL_ID, date);
        //	if (orderId != 0) {
        //              Log.Warning("  ==> Order akready exists.");
        //              return 0; }
        //          var t = new TaskCompletionSource<int>();
        //          var sthId = await _b040.GetStandardHIdByCustomerDayOfWeekCode(customer.KL_ID, dayOfWeekInDutch, standardCode);
        //          if (sthId == 0){
        //              Log.Warning("  ==> No standards.");
        //              return 0; }
        //          var o = new bzBestel();
        //          string document = "";
        //          bool isParticulier = false;
        //          await Task.Run(() => o.createBestelFromStandaard(sthId, date, ref document, ref isParticulier));
        //          Log.Warning($"  ==> Created {document}.");
        //          return await _b040.GetOrderIdByDocument(document);
        //      }
        [AllowAnonymous]
        [HttpGet]
        [Route("GetWebAccountApproved")]
        public Boolean GetWebAccountApproved(string webaccountId)
        {
			bool rv = false;
			var _b040 = DataAccessB040.GetInstance();
			try
			{
				rv = _b040.GetWebAccountApproved(webaccountId);
			}
			catch (Exception ex)
			{
				Log.Warning("GetAccountsApproved Endpoint Failure");
				Log.Warning(ex.Message);
			}
			return rv;
        }
        [AllowAnonymous]
        [HttpGet]
        [Route("GetConfigurationsB040")]
        public List<ConfigurationB040Model> GetConfigurationsB040()
        {
			var c = ConfigurationHelper.GetConfigurations();
			List<ConfigurationB040Model> cList = new List<ConfigurationB040Model>();
			foreach (var item in c)
            {
				cList.Add(new ConfigurationB040Model()
				{
					Key = item.Key.ToString(),
					Value = item.Value.ToString()
				});

            }
			Log.Warning($"Configurations: {cList.Count}");
			return cList;
        }
        [AllowAnonymous]
        [HttpPost]
        [Route("InsertCall")]
        public OpResult InsertCall(CallModel model)
        {
            var op = new OpResult();
            try
            {
                var _b040 = DataAccessB040.GetInstance();
                var result = _b040.InsertCall(model.Call_Telephone, model.Call_RawData, model.Call_ConnectionTimeStamp);

                if (!result.Success)
                {
                    op.Fail(result.GetMessage());
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"InsertCall failed: {ex.Message}");
                op.Fail($"Unhandled exception: {ex.Message}");
            }
            return op;
        }


    }
}