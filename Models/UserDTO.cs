using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace B040.Authentication.Models
{
    public class UserDTO
    {
        public string WebAccountId { get; set; }
        public string WebAccountName { get; set; }
        public bool IsRegistered { get; set; }
        public List<string> Roles { get; set; }
    }
}