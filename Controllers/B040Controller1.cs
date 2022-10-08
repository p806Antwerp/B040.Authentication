using B040.Authentication.Models;
using Microsoft.Ajax.Utilities;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.UI;

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
            var ctx = new ApplicationDbContext();
            var users = ctx.Users.ToList();
            return users;
        }
        [AllowAnonymous]
        [HttpPost]
        [Route("Admin/CreateRoles")]
        public void CreateRoles()
        {
            var ctx = new ApplicationDbContext();
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
        [AllowAnonymous]
        [HttpGet]
        [Route("Admin/GetAllRoles")]
        public List<IdentityRole> GetAllRoles()
        {
            var ctx = new ApplicationDbContext();
            var roles = ctx.Roles.ToList();
            return roles;
        }
        [AllowAnonymous]
        [HttpPost]
        [Route("Admin/CreateAdmin")]
        public void CreateAdmin(string userName, string password)
        {
            var ctx = new ApplicationDbContext();
            var userId = ctx.Users.FirstOrDefault(x => x.UserName == userName)?.Id;
            if (userId == null)
            {
                var newU = new ApplicationUser()
                {
                    UserName = userName,
                    PasswordHash = password
                };
                ctx.Users.Add(newU);
                ctx.SaveChanges();
                userId = newU.Id;
            }
            var u = ctx.Users.FirstOrDefault(x => x.Id == userId);
            addRole("Admin");
            addRole("Client");
            void addRole(string roleName)
            {
                var roleId = ctx.Roles.FirstOrDefault(x => x.Name == roleName).Id;
                var ur = new IdentityUserRole()
                {
                    UserId = userId,
                    RoleId = roleId
                };
                if (u.Roles.Any(x => x.RoleId == roleId) == false)
                {
                    u.Roles.Add(ur);
                    ctx.SaveChanges();
                }
            }
        }
    }

}