using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(B040.Authentication.Startup))]

namespace B040.Authentication
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
            Serilog.Log.Warning("End of Startup.Configuration");
        }
    }
}
