using B040.Authentication.Models;
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
            var ctx = new ApplicationDbContext();
            var users = ctx.Users.ToList();
            return users;
        } 
 
    }
}