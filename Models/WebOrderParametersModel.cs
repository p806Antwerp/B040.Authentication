using System;

namespace B040.Authentication.Models

{
	public class WebOrderParametersModel
	{
		public string WebAccountId { get; set; }
		public DateTime Date { get; set; }
		public string StandardCode { get; set; } = string.Empty;
	}
}
