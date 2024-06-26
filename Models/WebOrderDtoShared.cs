﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B040.Authentication.Models
{
	public class WebOrderDtoShared
	{
		public string CustomerName { get; set; }
		public string DayOfWeekInDutch { get; set; }
		public int BestH_Id { get; set; }
		public bool Success { get; set; } = true;
		public string Message { get; set; } = "";
		public string Info { get; set; } = "";
		public bool InProduction { get; set; } = false;
		public List<WebOrderDtoDetailShared> Repository { get; set; } = new List<WebOrderDtoDetailShared>();
	}
}
