using B040.Authentication.Models;
using Microsoft.Ajax.Utilities;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.UI;

namespace B040.Authentication.Controllers
{

    [Authorize]
    [RoutePrefix("api/Authentication")]
    public class AuthenticationController : ApiController
    {
        [AllowAnonymous]
        [HttpPost]
        [Route("Admin/CreateAdmin")]
        public bool CreateAdmin(ApplicationUser applicationUser)
        {
            var ctx = new ApplicationDbContext();
            var u = ctx.Users.FirstOrDefault(x => x.UserName == applicationUser.UserName);
            addRole("Admin");
            addRole("Client");
            return true;
            void addRole(string roleName)
            {
                var roleId = ctx.Roles.FirstOrDefault(x => x.Name == roleName).Id;
                var ur = new IdentityUserRole()
                {
                    UserId = u.Id,
                    RoleId = roleId
                };
                if (u.Roles.Any(x => x.RoleId == roleId) == false)
                {
                    u.Roles.Add(ur);
                    ctx.SaveChanges();
                }
            }
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
        [HttpPost]
        [Route("Admin/ExistsUser")]
        public ExistsModel ExistsUser(UserNamePasswordPairModel applicationUser)
        {
            var ctx = new ApplicationDbContext();
            ApplicationUser u = ctx.Users.FirstOrDefault(x => x.UserName == applicationUser.UserName);
            var result = new ExistsModel() { Exists = u != null };
            return result;
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
        [HttpGet]
        [Route("Admin/GetAllUsers")]
        public List<UserWithRolesModel> GetAllUsers()
        {
            var ctx = new ApplicationDbContext();
            var roles = ctx.Roles.ToList();
            List<UserWithRolesModel> results = ctx
                .Users.ToList()
                .Select(x => new UserWithRolesModel
                {
                    Id = x.Id,
                    UserName = x.UserName,
                    Roles = x.Roles.Join(roles,u => u.RoleId,r => r.Id,(u,r)=> r)
                        .Select(y => new RoleModel(){ Id =y.Id,Name = y.Name })
                        .ToList()
                })
                .ToList();
            return results;
        }
    }

}