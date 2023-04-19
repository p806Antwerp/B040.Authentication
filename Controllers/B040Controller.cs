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
namespace B040.Authentication.Controllers
{
	[Authorize]
	[RoutePrefix("api/B040")]
	public class B040Controller : ApiController
	{
		[AllowAnonymous]
		[HttpPost]
		[Route("GetWebOrder")]
		public async Task<WebOrderDto> GetWebOrder(WebOrderParametersModel wp)
		{
			string email = wp.Email;
			DateTime date = wp.Date;
			string standardCode= wp.StandardCode;
			var dtoTask = new TaskCompletionSource<WebOrderDto>();
			var dto = new WebOrderDto();
			var _b040 = DataAccessB040.GetInstance();
			var customer = await _b040.GetWebCustomerByEmail(email);
			if (customer == null)
			{
				dto.Success = false;
				dto.Message = $"Email [{email}] komt niet overeen met een Web klant.";
				dtoTask.SetResult(dto);
				return await dtoTask.Task;
			}
			dto.CustomerName = customer.KL_Naam;
			dto.DayOfWeekInDutch = modDutch.cDagInDeWeek(date);
			var orderId = await _b040.GetOrderIdByCustomerAndDate(customer.KL_ID, date);
			if (customer == null)
			{
				dto.Success = false;
				dto.Message = $"Uw bestelling voor {date.ToString("dd/MMM/yyyy")} ({dto.DayOfWeekInDutch}) kan nog niet worden aangepast.";
				dtoTask.SetResult(dto);
				return await dtoTask.Task;
			}
			//if (orderId == 0)
			//{
			//	orderId = await GenerateOrder();
			//	if (dto.Success == false)
			//	{
			//		dtoTask.SetResult(dto);
			//		return await dtoTask.Task;
			//	}
			//}
			var q = await _b040.GetOrderById(orderId);
			int count = q.Count();
			if (count < 2)
			{
				dto.Success = false;
				dto.Message = "Deze bestelling kan niet via Web worden aangepast (minder dan 2 lijnen).";
				dtoTask.SetResult(dto);
				return await dtoTask.Task;
			}
			dto.Repository = q.CastingList<WebOrderDtoDetail, BestDModelX>();
			dto.BestH_Id = orderId;
			dtoTask.SetResult(dto);
			return await dtoTask.Task;
			//async Task<int> GenerateOrder()
			//{
			//	var t = new TaskCompletionSource<int>();
			//	var sthId = await _b040.GetStandardHIdByCustomerDayOfWeekCode(customer.KL_ID, dto.DayOfWeekInDutch, standardCode);
			//	if (sthId == 0)
			//	{
			//		dto.Success = false;
			//		dto.Message = $"Geen standaard beschikbaar voor {dto.CustomerName} op {dto.DayOfWeekInDutch}.  Standaard: {standardCode}";
			//		return 0;
			//	}
			//	var o = new bzBestel();
			//	string document = "";
			//	bool isParticulier = false;
			//	await Task.Run(() => o.createBestelFromStandaard(sthId, date, ref document, ref isParticulier));
			//	return await _b040.GetOrderIdByDocument(document);
			//}
		}
		[AllowAnonymous]
		[HttpPost]
		[Route("GetNextDeliveryDate")]
		public async Task<DateTime> GetNextDeliveryDate()
		{
			var result = Task.Run(() => bzBestel.dGetLeveringForBestellingDatum()).Result;
			return result;
		}
		// GET api/<controller>
		public IEnumerable<string> Get()
		{
			return new string[] { "value1", "value2" };
		}

		// GET api/<controller>/5
		public string Get(int id)
		{
			return "value";
		}

		// POST api/<controller>
		public void Post([FromBody] string value)
		{
		}

		// PUT api/<controller>/5
		public void Put(int id, [FromBody] string value)
		{
		}

		// DELETE api/<controller>/5
		public void Delete(int id)
		{
		}
	}
}