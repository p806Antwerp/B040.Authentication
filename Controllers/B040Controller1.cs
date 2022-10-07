using B040.Authentication.Models;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace B040.Authentication.Controllers
{
    [Authorize]
    [RoutePrefix("api/B040")]
    public class B040Controller : ApiController
    {
        [AllowAnonymous]
        [HttpGet]
        [Route("Admin/GetAllUsers")]
        public List<ApplicationUser> GetAllUsers()
        {
            using (var ctx = new ApplicationDbContext())
            {
                var users = ctx.Users.ToList();
                return users;
            }
        }
        [AllowAnonymous]
        [HttpPost]
        [Route("Admin/CreateRoles")]
        public void CreateRoles()
        {
            using (var ctx = new ApplicationDbContext())
            {
                process("Admin");
                process("Client");
                void process(string roleName)
                {
                    if (ctx.Roles.Any(x => x.Name == roleName)) { return; }
                    var r = new IdentityRole() { Name = roleName };
                    ctx.Roles.Add(r);
                    ctx.SaveChanges();
                }
            }
        }
    }

}