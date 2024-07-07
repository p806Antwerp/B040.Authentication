using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B040.Authentication.Models

{
	public class WebOrderDtoDetailShared
	{
		public int? BestD_ID { get; set; }
		public int? BestD_BestH { get; set; }
		public int? BestD_Artikel { get; set; }
		public string BestD_Omschrijving { get; set; }
		public bool BestD_Snijden { get; set; }
		public string BestD_Tour { get; set; }
		public double? BestD_Hoev { get; set; }
		public double? BestD_Hoev1 { get; set; }
		public double? BestD_EenhPrijs { get; set; }
		public double? BestD_Waarde { get; set; }
		public string Art_Nr { get; set; }
		public bool Art_Snijden { get; set; }
		public int Eenh_Exponent { get; set; }
		public bool Art_Notify { get; set; }
		public bool BestD_Notified { get; set; }
		public bool CuttingEnabled { get; set; }
		public string BestD_Opschrift { get; set; }
	}
}
