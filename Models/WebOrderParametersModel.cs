using System;

namespace B040.Authentication.Models

{
	public class WebOrderParametersModel
	{
		public string Email { get; set; }
		public DateTime Date { get; set; }
		public string StandardCode { get; set; } = string.Empty;
	}
}
