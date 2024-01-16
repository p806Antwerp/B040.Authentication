using MySql.Data.EntityFramework;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace B040.Authentication
{
    public class MyDbConfiguration : DbConfiguration
    {
        public MyDbConfiguration()
        {
            if (Environment.GetEnvironmentVariable(
                 "ACTIVE_AUTH"
                 , EnvironmentVariableTarget.Machine)
                == "MARIADB")
            {
                SetProviderServices(
                    "MySql.Data.MySqlClient",
                    new MySqlProviderServices()
                    );
                SetDefaultConnectionFactory(
                    new MySqlConnectionFactory());
            }
        }
    }

}