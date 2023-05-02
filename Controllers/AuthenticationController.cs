﻿using B040.Authentication.Models;
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
using System.Security.Principal;

namespace B040.Authentication.Controllers
{

    [Authorize]
    [RoutePrefix("api/Authentication")]
    public class AuthenticationController : ApiController
    {
        private ApplicationDbContext _context;
        public AuthenticationController()
        {
            _context = new ApplicationDbContext();
            Monitor.Console("Authentication Controller Connected.");
        }
        [AllowAnonymous]
        [HttpPost]
        [Route("Admin/CreateAdmin")]
        public bool CreateAdmin(ApplicationUser applicationUser)
        {
            var u = _context.Users.FirstOrDefault(x => x.UserName == applicationUser.UserName);
            addRole("Admin");
            addRole("Client");
            return true;
            void addRole(string roleName)
            {
                var roleId = _context.Roles.FirstOrDefault(x => x.Name == roleName).Id;
                var ur = new IdentityUserRole()
                {
                    UserId = u.Id,
                    RoleId = roleId
                };
                if (u.Roles.Any(x => x.RoleId == roleId) == false)
                {
                    u.Roles.Add(ur);
                    _context.SaveChanges();
                }
            }
        }
        [AllowAnonymous]
        [HttpPost]
        [Route("Admin/CreateClients")]
        public async Task<bool> CreateClients()
        {
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
                var u = _context.Users.FirstOrDefault(x => x.UserName == c.Kl_Email);
                addRole("Client");
                void addRole(string roleName)
                {
                    var roleId = _context.Roles.FirstOrDefault(x => x.Name == roleName).Id;
                    var ur = new IdentityUserRole()
                    {
                        UserId = u.Id,
                        RoleId = roleId
                    };
                    if (u.Roles.Any(x => x.RoleId == roleId) == false)
                    {
                        u.Roles.Add(ur);
                        _context.SaveChanges();
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
            process("Admin");
            process("Client");
            void process(string roleName)
            {
                if (_context.Roles.Any(x => x.Name == roleName)) { return; }
                var r = new IdentityRole() { Name = roleName };
                _context.Roles.Add(r);
                _context.SaveChanges();
            }
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("Admin/ExistsUser")]
        public ExistsModel ExistsUser(UserNamePasswordPairModel model)
        {
            ApplicationUser u = _context.Users.FirstOrDefault(x => x.UserName == model.UserName);
            var result = new ExistsModel() { Exists = u != null };
            return result;
        }
        [AllowAnonymous]
        [HttpGet]
        [Route("Admin/GetAllRoles")]
        public List<IdentityRole> GetAllRoles()
        {
            var roles = _context.Roles.ToList();
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
            ApplicationUser u;
            Monitor.Console($"Conn. string: {_context.Database.Connection.ConnectionString}");
            Monitor.Console($"Current User: {WindowsIdentity.GetCurrent().Name}");
            try
            {
                u = _context.Users.FirstOrDefault(x => x.UserName == model.UserName);
            }
            catch (Exception ex)
            {
                Monitor.Console($"GetRoles exception: {ex.Message}");
 
                throw ex;
            }
            if (u == null) { return rv; }
            // Verify the passowrd
            var hasher = new PasswordHasher();
            var result = hasher.VerifyHashedPassword(u.PasswordHash, model.Password);
            if (result != PasswordVerificationResult.Success) { return rv; }
            // return the role name(s)
            foreach (var r in u.Roles)
            {
                rv.Add(_context.Roles.FirstOrDefault(x => x.Id == r.RoleId).Name);
            }
            return rv;
        }
        [AllowAnonymous]
        [HttpGet]
        [Route("Admin/GetAllUsers")]
        public List<UserWithRolesModel> GetAllUsers()
        {
            var roles = _context.Roles.ToList();
            List<UserWithRolesModel> results = _context
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