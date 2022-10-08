using B040.Authentication.Models;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.VisualBasic.ApplicationServices;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace B040.Authentication
{
    public class APIHelper
    {
        private HttpClient _ApiClient;
        public APIHelper()
        {
            InitializeClient();
        }
        private void InitializeClient()
        {
            _ApiClient = new HttpClient();
            string api = ConfigurationManager.AppSettings["api"];
            Console.WriteLine(api);
            try
            {
                _ApiClient.BaseAddress = new Uri(api);
            }
            catch (Exception)
            {
                Console.Write("Setting BaseAddress threw an error.");
                throw;
            }
            _ApiClient.DefaultRequestHeaders.Accept.Clear();
            _ApiClient
                .DefaultRequestHeaders
                .Accept
                .Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/Json"));
        }
        public async Task<AuthenticatedUserModel> Authenticate(string username, string password)
        {
            var data = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("grant_type","password"),
                new KeyValuePair<string,string>("username",username),
                new KeyValuePair<string,string>("password",password),
            });
            using (HttpResponseMessage response = await _ApiClient.PostAsync("/Token", data))
            {
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsAsync<AuthenticatedUserModel>();
                    return result;
                }
                else
                {
                    throw new Exception(response.ReasonPhrase);
                }
            }
        }
        public async Task<List<ApplicationUser>> GetUsersAsync()
        {
            using (HttpResponseMessage response = await _ApiClient.GetAsync("/api/B040/Admin/GetAllUsers"))
            {
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsAsync<List<ApplicationUser>>();
                    return result;
                }
                else
                {
                    throw new Exception(response.ReasonPhrase);
                }
            }
        }
        public async Task<List<IdentityRole>> GetRolesAsync()
        {
            using (HttpResponseMessage response = await _ApiClient.GetAsync("/api/B040/Admin/GetAllRoles"))
            {
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsAsync<List<IdentityRole>>();
                    return result;
                }
                else
                {
                    throw new Exception(response.ReasonPhrase);
                }
            }
        }
        public async Task CreateRolesAsync()
        {
           string o = null;
            using (HttpResponseMessage response = await _ApiClient.PostAsJsonAsync("/api/B040/Admin/CreateRoles",o))
            {
                if (response.IsSuccessStatusCode)
                {
                    return ;
                }
                else
                {
                    throw new Exception(response.ReasonPhrase);
                }
            }
        }
        public async Task CreateAdminAsync(string userName,string password)
        {
            var o = new UserNamePasswordPairModel()
            {
                UserName = userName,
                Password = password,
            };
            using (HttpResponseMessage response = await _ApiClient.PostAsJsonAsync("/api/B040/Admin/CreateAdmin", o))
            {
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
                else
                {
                    throw new Exception(response.ReasonPhrase);
                }
            }
        }
    }
}
public class AuthenticatedUserModel
{
    public string Access_Token { get; set; }
    public string UserName { get; set; }
}
public class UserNamePasswordPairModel
{
    public string UserName { get; set; }
    public string Password { get; set; }
}
