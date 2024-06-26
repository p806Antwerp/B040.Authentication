﻿using B040.Authentication.Models;
using Microsoft.AspNet.Identity;
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
    public class ValuesController : ApiController
    {
        private ApplicationDbContext _context;
        public ValuesController(ApplicationDbContext context) 
        {
            _context = context;
        }
        [AllowAnonymous]
        [HttpGet]
        [Route("Admin/GetAllUsers")]
        public List<ApplicationUser> GetAllUsers()
        {
            // List<ApplicationUser> users = new List<ApplicationUser>();
            //var userStore = new UserStore<ApplicationUser>(ctx);
            //var userManager = new UserManager<ApplicationUser>(userStore);
            var users = _context.Users.ToList();
            return users;
        }
        // GET api/values
        //[AllowAnonymous]
        //[HttpGet]
        //[Route("Admin/GetAllRoles")]
        //public List<IdentityRole> GetAllRoles()
        //{
        //    using (var ctx = new ApplicationDbContext())
        //    {
        //        var roles = ctx.Roles.ToList();
        //        return roles;
        //    }
        //}
        public IEnumerable<string> Get()
        {
            string userId = RequestContext.Principal.Identity.GetUserId();
            return new string[] { "value1", "value2",userId };
        }

        // GET api/values/5
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        public void Delete(int id)
        {
        }
    //    [AllowAnonymous]
    //    [HttpPost]
    //    [Route("Admin/CreateRoles")]
    //    public void CreateRolesOnce()
    //    {
    //        using (var ctx = new ApplicationDbContext())
    //        {
    //            process("Admin");
    //            process("Client");
    //            void process(string roleName)
    //            {
    //                if (ctx.Roles.Any(x => x.Name == roleName)) { return; }
    //                var r = new IdentityRole() { Name = roleName };
    //                ctx.Roles.Add(r);
    //                ctx.SaveChanges();
    //            }
    //        }
    //    }
    }
}

