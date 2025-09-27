using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

            if (!GlobalConfiguration.Configuration.Routes
                    .Any(rt => (rt.RouteTemplate ?? string.Empty)
                        .StartsWith("swagger", StringComparison.OrdinalIgnoreCase)))
            {
                SwaggerConfig.Register();
            }
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            //string logFilePath = GetLogFilePath();

            Serilog.Log.Logger = new LoggerConfiguration()
                .WriteTo.Console() // Log messages to the console
                .Enrich.WithProperty("MachineName",Environment.MachineName)    
                .WriteTo.File(
                    GetLogsPath(),
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:ddd HH:mm:ss} [{MachineName} API] {Message:lj}{NewLine:1}{Exception:1}",
                    shared:true)
                .CreateLogger();
            string assemblyLocation = Assembly.GetExecutingAssembly().Location;
            // Create a FileInfo object for the assembly file
            FileInfo fileInfo = new FileInfo(assemblyLocation);

            // Get the creation time or last write time
            DateTime creationTime = fileInfo.CreationTime;
            DateTime lastWriteTime = fileInfo.LastWriteTime;
            Log.Warning(fileInfo.FullName);
            Log.Warning(fileInfo.LastAccessTime.ToString("dd/MMM/yy-hh:mm:ss"));

        }
        static bool IsDevelopment()
        {
            return (Environment.MachineName.ToUpper() == "PEPIN");
        }
        public static string GetBackendDriveUNC()
        {
            string rv = @"\\RS820\Bread";
            if (IsDevelopment())
            {
                rv = @"\\jaspers\c";
            }
            return rv;
        }
        public static string GetLogsPath()
        {
            return $@"{GetBackendDriveUNC()}\B040\logs\logs-.txt";
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
        protected void Application_End()
        {
            // This method is called when the application is shutting down
            Serilog.Log.Information("Application is stopping...");

            // Perform any necessary cleanup or logging here
            // OnApplicationStopping();

            // Flush the log to ensure all messages are written
            Serilog.Log.CloseAndFlush();
        }
    }

}

