using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;

namespace B040.Authentication.Models
{

    public class AuthenticatedUserModel
    {
        public string Access_Token { get; set; }
        public string UserName { get; set; }
    }
    public class ExistsModel
    {
        public bool Exists{ get; set; }
    }
    public class RoleModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
    public class UserNamePasswordPairModel
    {
        public string UserName { get; set; }
        public string Password { get; set; }
    }
    public class UserWithRolesModel
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public List<RoleModel> Roles { get; set; }

    }
}
