using B040.Authentication.Controllers;
using B040.Authentication.Models;
using B040.Services.Models;
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
    public class ApiHelper
    {
        private HttpClient _ApiClient;
        public ApiHelper()
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
        public async Task<List<UserWithRolesModel>> GetAllUsersAsync()
        {
            using (HttpResponseMessage response = await _ApiClient.GetAsync("/api/Authentication/Admin/GetAllUsers"))
            {
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsAsync<List<UserWithRolesModel>>();
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
            using (HttpResponseMessage response = await _ApiClient.GetAsync("/api/Authentication/Admin/GetAllRoles"))
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
            using (HttpResponseMessage response = await _ApiClient.PostAsJsonAsync("/api/Authentication/Admin/CreateRoles",o))
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
            using (HttpResponseMessage response = await _ApiClient.PostAsJsonAsync("/api/Authentication/Admin/CreateAdmin", o))
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
        public async Task<bool> ExistsUserAsync(string userName)
        {
            var data = new UserNamePasswordPairModel()
            {
                UserName = userName
            };
            using (HttpResponseMessage response = await _ApiClient.PostAsJsonAsync("/api/Authentication/Admin/ExistsUser", data))
            {
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsAsync<ExistsModel>();
                    return result.Exists;
                }
                else
                {
                    throw new Exception(response.ReasonPhrase);
                }
            }
        }
        public async Task CreateUserAsync(string userName,string password)
        {
            var data = new RegisterBindingModel()
            {
                Email = userName,
                Password = password,
                ConfirmPassword = password
            };
            using (HttpResponseMessage response = await _ApiClient.PostAsJsonAsync("api/Account/Register", data))
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
        public async Task CreateClientsAsync()
        {
            string o = null;
            using (HttpResponseMessage response = await _ApiClient.PostAsJsonAsync("/api/Account/Admin/CreateClients", o))
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
        public async Task GetRolesAsync(string loginEmail, string loginPassword)
        {
            UserNamePasswordPairModel up = 
                new UserNamePasswordPairModel() { 
                    UserName = loginEmail, 
                    Password = loginPassword };
            using (HttpResponseMessage response = 
                await _ApiClient.PostAsJsonAsync<UserNamePasswordPairModel>(
                    "/api/Account/Admin/GetRoles",
                    up))
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

        public Task CreateAdminAsync(string userName)
        {
            throw new NotImplementedException();
        }
		public async Task GetWebOrder(WebOrderParametersModel wp)
		{
			using (HttpResponseMessage response = 
                await _ApiClient.PostAsJsonAsync<WebOrderParametersModel>(
                    "/api/B040/GetWebOrder", wp))
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
		public async Task GetNextDeliveryDate()
		{
			using (HttpResponseMessage response =
				await _ApiClient.PostAsJsonAsync<DateTime>(
					"/api/B040/GetNextDeliveryDate",DateTime.Today.AddDays(1)))
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
