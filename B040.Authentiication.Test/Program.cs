using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B040.Authentication.Controllers;
using B040.Services.Models;
using System.Web.Http;

namespace B040.Authentication.Test
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var m = new CallModel();
            m.Call_ConnectionTimeStamp = DateTime.Now;
            m.Call_RawData = "Test";
            m.Call_Telephone = "Dan";
            var b040Controller = new B040Controller();
            var or = b040Controller.InsertCall(m);
            Console.WriteLine("Done");
        }
    }
}
