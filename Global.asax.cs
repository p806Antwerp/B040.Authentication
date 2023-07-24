using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Serilog;

namespace B040.Authentication
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            string logFilePath = GetLogFilePath();

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console() // Log messages to the console
                .WriteTo.File(
                    logFilePath,
                    rollingInterval: RollingInterval.Infinite,
                    outputTemplate: "{Timestamp:MMM/dd hh:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
        }

        private static string GetLogFilePath()
        {
            string formattedDate = DateTime.Now.ToString("ddd dd/MMM/yy hh:mm:ss");
            formattedDate = formattedDate.Replace('/', '-');
            formattedDate = formattedDate.Replace(':', '-');
            string latestLogFile = Directory.GetFiles(@"C:\Docs\", "logB040Api-" + formattedDate + "-*.txt")
                                .OrderByDescending(x => x)
                                .FirstOrDefault();
            int sequenceNumber = 1; // Initialize with 1 for the first session
            if (latestLogFile != null)
            {
                string fileName = Path.GetFileNameWithoutExtension(latestLogFile);
                int.TryParse(fileName.Split('-').Last(), out sequenceNumber);
                sequenceNumber++; // Increment the sequence number for the next session
            }
            string zeroFilledSeq = sequenceNumber.ToString("D3"); // Zero-filled 3-digit sequence number
            string logFilePath = Path.Combine(@"C:\Docs\", $"logB040Api-{formattedDate}-{zeroFilledSeq}.txt");
            return logFilePath;
        }
    }
}

