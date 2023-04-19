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
using Mg.Services;
using B040.Services;
using B040.Services.Models;
using b040;
using System.Web.Security;
using System.Security.Policy;
using System.Runtime.InteropServices.ComTypes;
using System.Web.Helpers;

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
        [Route("Admin/CreateClients")]
        public async Task<bool> CreateClients()
        {
            var ctx = new ApplicationDbContext();
            var b040 = DataAccessB040.GetInstance();
            AccountController accountController = new AccountController();
            List<ClientWithEmailModel> clientsWithEmail = b040.GetClientsWithEmail();
            foreach (ClientWithEmailModel c in clientsWithEmail)
            {
                var au = new UserNamePasswordPairModel()
                {
                    UserName = c.Kl_Email
                };
                var exists = ExistsUser(au);
                if (exists.Exists == false)
                {
                    string zerofilledAccount = ("00000" + c.Kl_Nummer.Trim()).Right(5);
                    string pwd = $"Pwd{zerofilledAccount}.";
                    RegisterBindingModel m = new RegisterBindingModel()
                    {
                        Email = c.Kl_Email,
                        Password = pwd,
                        ConfirmPassword = pwd
                    };
                    await accountController.Register(m);
                }
                var u = ctx.Users.FirstOrDefault(x => x.UserName == c.Kl_Email);
                addRole("Client");
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
            return true;
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
        public ExistsModel ExistsUser(UserNamePasswordPairModel model)
        {
            var ctx = new ApplicationDbContext();
            ApplicationUser u = ctx.Users.FirstOrDefault(x => x.UserName == model.UserName);
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
        [HttpPost]
        [Route("Admin/GetRoles")]
        public List<String> GetRoles(UserNamePasswordPairModel model)
        {
            var rv = new List<String>();
            if (model.UserName == null) { return rv; }
            if (model.Password == null) { return rv; }
            // Get the DBContext from the OWin Context
            var ctx = new ApplicationDbContext();
            // Retrieve the user
            ApplicationUser u = ctx.Users.FirstOrDefault(x => x.UserName == model.UserName);
            if (u == null) { return rv; }
            // Verify the passowrd
            var hasher = new PasswordHasher();
            var result = hasher.VerifyHashedPassword(u.PasswordHash, model.Password);
            if (result != PasswordVerificationResult.Success) { return rv; }
            // return the role name(s)
            foreach (var r in u.Roles)
            {
                rv.Add(ctx.Roles.FirstOrDefault(x => x.Id == r.RoleId).Name);
            }
            return rv;
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
                    Roles = x.Roles.Join(roles, u => u.RoleId, r => r.Id, (u, r) => r)
                        .Select(y => new RoleModel() { Id = y.Id, Name = y.Name })
                        .ToList()
                })
                .ToList();
            return results;
        }
    }
}