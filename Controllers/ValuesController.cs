using B040.Authentication.Models;
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
        [AllowAnonymous]
        [HttpGet]
        [Route("Admin/GetAllUsers")]
        public List<ApplicationUser> GetAllUsers()
        {
            // List<ApplicationUser> users = new List<ApplicationUser>();
            var ctx = new ApplicationDbContext();
 
                //var userStore = new UserStore<ApplicationUser>(ctx);
                //var userManager = new UserManager<ApplicationUser>(userStore);
            var users = ctx.Users.ToList();
        
            return users;
        }
        // GET api/values
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
    }
}
