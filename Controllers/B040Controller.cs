using B040.Services.Models;
using B040.Services;
using b040;
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

namespace B040.Authentication.Controllers
{
	[Authorize]
	[RoutePrefix("api/B040")]
	public class B040Controller : ApiController
	{
        [AllowAnonymous]
        [HttpGet]
        [Route("GetAllArtikelsFromWebAccountId")]
        public async Task<List<ArtikelModel>> GetAllArtikelsFromWebAccountId(string webAccountId)
        {
            var _b040 = DataAccessB040.GetInstance();
            return Task.Run(() =>  _b040.GetArtikelsFromWebAccountId(webAccountId)).Result;
        }
        [AllowAnonymous]
        [HttpGet]
        [Route("GetAllActiveWebArticles")]
        public async Task<List<ArtikelModel>> GetAllActiveWebArticles()
        {
            var _b040 = DataAccessB040.GetInstance();
            return Task.Run(() => _b040.GetAllActiveWebArticles()).Result;
        }
        [AllowAnonymous]
		[HttpGet]
		[Route("GetArtikelXDtoSharedFromId")]
		public async Task<ArtikelXDtoShared> GetArtikelXDtoSharedFromId(string idString)
		{
			int id = int.Parse(idString);
			var _b040 = DataAccessB040.GetInstance();
			return Task.Run(() => _b040.GetArtikelXDtoSharedFromId(id)).Result;
		}
		[AllowAnonymous]
		[HttpPost]
		[Route("GetWebOrder")]
		public async Task<WebOrderDtoShared> GetWebOrder(WebOrderParametersModel wp)
		{
			Log.Warning("GetWebOrder");
			Log.Warning($"[{wp.WebAccountId}], {wp.Date.ToString("dd-MMM-yy")}");
			string webAccountId = wp.WebAccountId;
			DateTime date = wp.Date;
			string standardCode= wp.StandardCode;
			var dtoTask = new TaskCompletionSource<WebOrderDtoShared>();
			var dto = new WebOrderDtoShared();
			var _b040 = DataAccessB040.GetInstance();
			var customer = await _b040.GetWebCustomerByWebAccountId(webAccountId);
			if (customer == null)
			{
				dto.Success = false;
				dto.Message = $"{webAccountId} is ongeldig.";
				dtoTask.SetResult(dto);
				return await dtoTask.Task;
			}
			dto.CustomerName = customer.KL_Naam;
			dto.DayOfWeekInDutch = modDutch.cDagInDeWeek(date);
			var bestelHeader = await _b040.GetOrderHeaderByCustomerAndDate(customer.KL_ID, date);
            Log.Warning($"[{customer.KL_Nummer},{wp.WebAccountId}], {wp.Date.ToString("dd-MMM-yy")}");
            int orderId = bestelHeader?.BestH_Id ?? 0;
			if (orderId == 0)
			{
				dto.Success = false;
				dto.Message = $"Uw bestelling voor {date.ToString("dd/MMM/yyyy")} ({dto.DayOfWeekInDutch}) kan nog niet worden aangepast.";
				dtoTask.SetResult(dto);
				return await dtoTask.Task;
			}
			var q = await _b040.GetOrderById(orderId);
			bool locked = await Task<bool>.Run(() => b040.ModLock.lLock(0, "BestH", orderId));
			if (locked==false)
			{
				dto.Success = false;
				dto.Message = "Deze bestelling is vergrendeld.";
				dtoTask.SetResult(dto);
				return await dtoTask.Task;
			}
			dto.Repository = q.CastingList<WebOrderDtoDetailShared, BestDModelX>();
			dto.BestH_Id = orderId;
			dto.Info = bestelHeader.BestH_Info;
			dto.InProduction = bestelHeader.BestH_InProduction ?? false;
			dtoTask.SetResult(dto);
			return await dtoTask.Task;
		}
		[AllowAnonymous]
		[HttpPost]
		[Route("GetNextDeliveryDate")]
		public async Task<DateTime> GetNextDeliveryDate()
		{
			var result = Task.Run(() => bzBestel.dGetLeveringForBestellingDatum()).Result;
			return result;
		}
		[AllowAnonymous]
		[HttpPost]
		[Route("UpdateWebOrder")]
		public async Task<OpResult> UpdateWebOrder(WebOrderDtoShared dto)
		{
            // 6301.07 Web Article Management
            // create a list of the artikels for which we need a notification report
            // 6305.01 Notification Management (revisited)
			// orderlines with quantity = 0 are not considered "to be notified"
            List<int> artikelsToNotify = new List<int>();
			try
			{
                artikelsToNotify = (List<int>)dto.Repository
                    .Where(x =>(x.Art_Notify==true) && (x.BestD_Notified == false) && (x.BestD_Hoev1 !=0))
                    .Select(x => x.BestD_Artikel ?? 0).ToList();
            }
            catch (Exception ex)
			{
				Serilog.Log.Warning($"Articles to Notify: {ex.Message}");
			}
            Serilog.Log.Warning($"UpdateWebOrder {dto.BestH_Id}");
            BestHModel bH;
			var b040Db = DataAccessB040.GetInstance();
			modB040Config.lb040Config();
			var opResult = new OpResult();
			int typFId = 1;
			try
			{
				// bH = await Task<BestHModel>.Run(() => b040Db.GetBestHFromId(dto.BestH_Id, typFId));
				bH = b040Db.GetBestHFromId(dto.BestH_Id, typFId);
                Serilog.Log.Warning($"==> GetBestHFromId {bH.BestH_Id}");

                double totalTeBetalen = await ComputeTeBetalen();
                Serilog.Log.Warning($"==> Compute Te Betalen {totalTeBetalen:n2}");
                async Task<double> ComputeTeBetalen()
				{
					KlantenModel k = await b040Db.GetKlantenById(bH.BestH_Klant ?? 0);
					var p = new bzPrice();
					p.klant = k.KL_Nummer;
					double sumTeBetalen = 0;
					foreach (var d in dto.Repository)
					{
						p.artikel = d.Art_Nr;
						p.compute(d.BestD_Waarde ?? 0);
						sumTeBetalen += p.nTeBetalen;
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
						Serilog.Log.Warning(bH.ToString());
						cruds.UpdateBestH(bH, t);
                        Serilog.Log.Warning($"==> UpdateBestH {bH.BestH_Id}");
                        // B040 6298.4 delete detail lines.
                        cruds.DeteteBestDByBestH_Id(dto.BestH_Id, t);
                        Serilog.Log.Warning($"==> DeleteBestDByBEstH_Id {dto.BestH_Id}");
                        foreach (var l in dto.Repository)
						{
                           
							BestDModel bD = l.Casting<BestDModel>();
							// if (bD.BestD_ID == 0) 
							// B040 6296.3 add header id to added orderline
							bD.BestD_ID = 0;
                            bD.BestD_BestH = dto.BestH_Id;
                            // 6301.07 Web Article Management
                            if (artikelsToNotify.Any(x => x == l.BestD_Artikel))
							{
								Serilog.Log.Warning($"Notified set {bD.BestD_Omschrijving}");
								bD.BestD_Notified = true; 
							}
                            cruds.InsertBestD(bD, t);
						}
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
					ModLock.unLock(cruds.GetBestHTableName(), dto.BestH_Id);
					modLog.nLog(
						 $"{dto.CustomerName}",
						  "UpdateWebOrder",
						  LogType.logNormal,
						  LogAction.logUpdate,
						  cruds.GetBestHTableName(),
						  dto.BestH_Id);
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
            if (artikelsToNotify.Count()>0)
			{
				try
				{
					opResult = await Task.Run(() => ReportWebOrderNotifications(dto.BestH_Id, artikelsToNotify));
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
                List<NotifiedArtikelModel> notifiedArtikels = b040Db.GetNotifiedArtikels(bestHId);
                if (artikelIds == null) { artikelIds = notifiedArtikels
						.Where(x=>x.BestD_Hoev1 != 0)
						.Select(x => x.BestD_Artikel).ToList(); }
                if (artikelIds == null) { return opResult; }
                var parameters = new bzUitzonderlijkDocument.uitzonderlijkdocument_variabelen();
                parameters.telefoon = $"Tel: {info.Adr_Telefoon}";
                parameters.komthalen = info.BestH_KomtHalen ? "Komt Halen" : "Sturen";
                parameters.klant_naam = info.Kl_Naam;
                parameters.adres = info.Adr_Adres;
                parameters.klantNummer = $"Klant {info.KL_Nummer}";
                parameters.info = info.BestH_Info;
                Serilog.Log.Warning($"Notifying {info.Kl_Naam}");
                foreach (var n in notifiedArtikels.Where(x => artikelIds.Contains(x.BestD_Artikel)))
                {
                    parameters.tour = n.BestD_Tour;
                    parameters.hoeveelheid = n.BestD_Hoev1.ToString();
                    parameters.artikel_omschrijving = n.BestD_Omschrijving;
                    parameters.voorafdrukken = "Web";
                    parameters.artikel = n.BestD_Artikel;
                    var u = new bzUitzonderlijkDocument();
                    parameters.postnummer_en_gemeente = (string)u.format_postnummer_adres(info.Adr_PostNummer, info.Adr_Gemeente);
                    parameters.datum_levering = u.format_date(info.BestH_DatLevering);
                    u.Dispatch(parameters);
                    Serilog.Log.Warning($"==> {parameters.artikel_omschrijving} notified.");
                }
                return opResult;
            }
        }
		[AllowAnonymous]
		[HttpPost]
		[Route("LockWebOrder")]
		public async Task<OpResult> LockWebOrder(LockModel m)
		{
			modB040Config.lb040Config();
			var or = new OpResult();
			or.Success = ModLock.lLock(0, m.Lock_table, m.Lock_LockedPK ?? 0,"Web");
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
		public async Task<OpResult> UnlockWebOrder(LockModel m)
		{
			modB040Config.lb040Config();
			var or = new OpResult();
			ModLock.unLock(m.Lock_table, m.Lock_LockedPK ?? 0);
			if (or.Success == false)
			{
				or.Fail("De ontgrendeling is mislukt.");
                Serilog.Log.Warning(or.Message); 
			}
			return or;
		}
        [AllowAnonymous]
        [HttpPost]
        [Route("UnlockWebOrdersFromWebAccountId")]
        public async Task UnlockWebOrdersFromWebAccountId(WebOrderParametersModel wp)
		{
            var b040Db = DataAccessB040.GetInstance();
			await Task.Run(() => b040Db.UnlockFromWebAccountId(wp.WebAccountId));
        }
		/// <summary>
		/// Quick and dirty implementation for prelaunching purposes.   Customer does not access use the automatisch bestellen process.
		/// </summary>
		/// <param name="wp"></param>
		/// <returns></returns>
		[AllowAnonymous]
		[HttpPost]
		[Route("GenerateWebOrderFromStandards")]
        public async Task<int> GenerateWebOrderFromStandards(WebOrderParametersModel wp)
        {
            modB040Config.lb040Config();
			modLog.nLog("Api - Create Web Order From Standard requested.");
            string webAccountId = wp.WebAccountId;
            DateTime date = wp.Date;
            string standardCode = wp.StandardCode ?? "1";
            string dayOfWeekInDutch = modDutch.cDagInDeWeek(date);
            var _b040 = DataAccessB040.GetInstance();
            var customer = await _b040.GetWebCustomerByWebAccountId(webAccountId);
			if (customer == null) {
                modLog.nLog("  ==> No such customer.");
                return 0; }
            modLog.nLog($"  ==> Getting order for {customer.KL_ID} on {date.ToString("dd/MMM/yyyy)")}.","API");
            var orderId = await _b040.GetOrderIdByCustomerAndDate(customer.KL_ID, date);
			if (orderId != 0) {
                modLog.nLog("  ==> Order akready exists.");
                return 0; }
            var t = new TaskCompletionSource<int>();
            var sthId = await _b040.GetStandardHIdByCustomerDayOfWeekCode(customer.KL_ID, dayOfWeekInDutch, standardCode);
            if (sthId == 0){
                modLog.nLog("  ==> No standards.");
                return 0; }
            var o = new bzBestel();
            string document = "";
            bool isParticulier = false;
            await Task.Run(() => o.createBestelFromStandaard(sthId, date, ref document, ref isParticulier));
            modLog.nLog($"  ==> Created {document}.");
            return await _b040.GetOrderIdByDocument(document);
        }
        [AllowAnonymous]
        [HttpGet]
        [Route("GetWebAccountApproved")]
        public async Task<Boolean> GetWebAccountApprovedASync(string webaccountId)
        {
			bool rv = false;
            var _b040 = DataAccessB040.GetInstance();
			try
			{
                rv = await _b040.GetWebAccountApprovedAsync(webaccountId);
            }
			catch (Exception ex)
			{
				Log.Warning("GetAccountsApproved Endpoing Failure");
				Log.Warning(ex.Message);
			}
			return rv;
        }
    }
}