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
        private HttpClient _APIClient;
        public APIHelper()
        {
            InitializeClient();
        }
        private void InitializeClient()
        {
            _APIClient = new HttpClient();
            string api = ConfigurationManager.AppSettings["api"];
            Console.WriteLine(api);
            try
            {
                _APIClient.BaseAddress = new Uri(api);
            }
            catch (Exception)
            {
                Console.Write("Setting BaseAddress threw an error.");
                throw;
            }
            _APIClient.DefaultRequestHeaders.Accept.Clear();
            _APIClient
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
            using (HttpResponseMessage response = await _APIClient.PostAsync("/Token", data))
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
            using (HttpResponseMessage response = await _APIClient.GetAsync("/api/B040/Admin/GetAllUsers"))
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
            using (HttpResponseMessage response = await _APIClient.GetAsync("/api/B040/Admin/GetAllRoles"))
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
           
            using (HttpResponseMessage response = await _APIClient.PostAsJsonAsync("/api/B040/Admin/CreateRoles","[{}]"))
            {
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsAsync(null);
                    return ;
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
public class AspNetUserModel
{
    public string Id { get; set; }
    public string Email { get; set; }
    public bool EmailConfirmed{ get; set; }
    public string PaswordHash{ get; set; }
    public string SecurityStamp{ get; set; }
    public string PhoneNumber{ get; set; }
    public bool PhoneNumberConfirmed{ get; set; }
    public DateTime LockoutEndDateUtc { get; set; }
    public int AccessFailedCount { get; set; }
    public string UserName { get; set; }
}
