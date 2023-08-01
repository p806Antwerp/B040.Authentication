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
		[Route("GetArtikelFromId")]
		public async Task<ArtikelModel> GetArtikelsFromId(string idString)
		{
			int id = int.Parse(idString);
			var _b040 = DataAccessB040.GetInstance();
			return Task.Run(() => _b040.GetArtikelFromId(id)).Result;
		}
		[AllowAnonymous]
		[HttpPost]
		[Route("GetWebOrder")]
		public async Task<WebOrderDto> GetWebOrder(WebOrderParametersModel wp)
		{
			string webAccountId = wp.WebAccountId;
			DateTime date = wp.Date;
			string standardCode= wp.StandardCode;
			var dtoTask = new TaskCompletionSource<WebOrderDto>();
			var dto = new WebOrderDto();
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
			var orderId = await _b040.GetOrderIdByCustomerAndDate(customer.KL_ID, date);
			if (orderId == 0)
			{
				dto.Success = false;
				dto.Message = $"Uw bestelling voor {date.ToString("dd/MMM/yyyy")} ({dto.DayOfWeekInDutch}) kan nog niet worden aangepast.";
				dtoTask.SetResult(dto);
				return await dtoTask.Task;
			}
			var q = await _b040.GetOrderById(orderId);
			int count = q.Count();
			if (count < 2)
			{
				dto.Success = false;
				dto.Message = "Deze bestelling kan niet via Web worden aangepast (minder dan 2 lijnen).";
				dtoTask.SetResult(dto);
				return await dtoTask.Task;
			}
			bool locked = await Task<bool>.Run(() => b040.ModLock.lLock(0, "BestH", orderId));
			if (locked==false)
			{
				dto.Success = false;
				dto.Message = "Deze bestelling is vergrendeld.";
				dtoTask.SetResult(dto);
				return await dtoTask.Task;
			}
			dto.Repository = q.CastingList<WebOrderDtoDetail, BestDModelX>();
			dto.BestH_Id = orderId;
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
		public async Task<OpResult> UpdateWebOrder(WebOrderDto dto)
		{
			BestHModel bH;
			var b040Db = DataAccessB040.GetInstance();
			modB040Config.lb040Config();
			var opResult = new OpResult();
			int typFId = 1;
			try
			{
				// bH = await Task<BestHModel>.Run(() => b040Db.GetBestHFromId(dto.BestH_Id, typFId));
				bH = b040Db.GetBestHFromId(dto.BestH_Id, typFId);
				double totalTeBetalen = await ComputeTeBetalen();
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
				var cruds = new Cruds(b040Db.GetConnection());
				using (var t = DataAccessB040.BeginTransaction())
				{
					try
					{
						Serilog.Log.Warning(bH.ToString());
						cruds.UpdateBestH(bH, t);
						foreach (var l in dto.Repository)
						{
							BestDModel bD = l.Casting<BestDModel>();
							if (bD.BestD_ID == 0) 
							{ // B040 6296.3 add header id to added orderline
								bD.BestD_BestH = dto.BestH_Id;
								cruds.InsertBestD(bD, t);
							}
							else { cruds.UpdateBestD(bD, t); }
						}
						t.Commit();
					}
					catch (Exception ex)
					{
						t.Rollback();
						string msg = $"UpdateWebOrder Rolled Back. ({dto.CustomerName})";
						Serilog.Log.Warning(msg);
						opResult.Fail(msg);
						opResult.Fail(ex.Message);
					}
				}
				if (opResult.Success)
				{
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
			}
			return opResult;
		}
		[AllowAnonymous]
		[HttpPost]
		[Route("LockWebOrder")]
		public async Task<OpResult> LockWebOrder(LockModel m)
		{
			modB040Config.lb040Config();
			var or = new OpResult();
			or.Success = ModLock.lLock(0, m.Table, m.Id,"Web");
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
			ModLock.unLock(m.Table, m.Id);
			if (or.Success == false)
			{
				or.Fail("Could not unlock.");
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
    }
}