using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using System.Configuration;
using System.IO;
using Mg.Services;

namespace B040.Authentication.Models
{
    // You can add profile data for the user by adding more properties to your ApplicationUser class, please visit https://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    public class ApplicationUser : IdentityUser
    {
        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager, string authenticationType)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, authenticationType);
            // Add custom user claims here
            return userIdentity;
        }
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        static string _connectionString = "*";
        static string GetConnectionString()
        {
            if (_connectionString != "*") { return _connectionString; }
            string connectionStringKey = "";
            string filePath = @"c:\_Config\B040.Ini";
            using (StreamReader reader = new StreamReader(filePath))
            {
                string line;
                string authToken = "AUTH";
                while ((line = reader.ReadLine()) != null)
                {
                    // Split the line into a key and a value
                    string[] parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        string key = parts[0].Trim();
                        string value = parts[1].Trim();

                        // Check if the key is "AUTH"
                        if (key == authToken)
                        {
                            connectionStringKey = value;
                        }
                    }
                }
            }
            _connectionString = ConfigurationManager
                  .ConnectionStrings[connectionStringKey]
                   .ConnectionString;
            Monitor.Console($"Connection (Auth): {_connectionString}");
            return _connectionString;
        }
        public ApplicationDbContext()
            : base(GetConnectionString(), throwIfV1Schema: false)
        {
        }
        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }
    }
}