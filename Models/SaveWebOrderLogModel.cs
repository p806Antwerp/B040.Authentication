using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace B040.Services.Cruds.CrudModels
{
    public class SaveWebOrderLogModel
    {
        public int Sw_Id { get; set; }
        public int Sw_Client { get; set; }
        public string Sw_Station { get; set; }
        public DateTime Sw_Date { get; set; }
        public string Sw_Time { get; set; } // Format: hh:mm
    }
}
