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
using Mg.Services;
using System.Data.SqlTypes;
using Swashbuckle.Swagger;
using Newtonsoft.Json;
using System.Text;
using Serilog;
using b040;

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
            // string api = ConfigurationManager.AppSettings["api"];
            string api = modB040Config.Generic("API-ADDRESS");
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
            using (HttpResponseMessage response = await _ApiClient.PostAsJsonAsync("/api/Authentication/Admin/CreateRoles", o))
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
        public async Task CreateAdminAsync(string userName, string password)
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
        public async Task<OpResult> CreateUserAsync(string userName, string password)
        {
            var or = new OpResult();
            var data = new RegisterBindingModel()
            {
                Email = userName,
                Password = password,
                ConfirmPassword = password
            };
            string returnedId;
            try
            {
                using (HttpResponseMessage response = await _ApiClient.PostAsJsonAsync("api/Account/Register", data))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        dynamic responseData =  JsonConvert.DeserializeObject(responseContent);
                        or.Message = responseData.Id;
                    }
                    if (response.IsSuccessStatusCode == false)
                    {
                        or.Message=$"Registering failed, [{userName}], {response.ReasonPhrase}";
                        or.Success = false;
                        Log.Warning(or.Message);
                        return or;
                    }
                }
            }
            catch (Exception ex)
            {
                or.Message = $"CreaetUser failed, {userName}, {ex.Message}";
                or.Success = false;
                Log.Warning(or.Message);
                return or;
            }
   
            return or;
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
                new UserNamePasswordPairModel()
                {
                    UserName = loginEmail,
                    Password = loginPassword
                };
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
                    "/api/B040/GetNextDeliveryDate", DateTime.Today.AddDays(1)))
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
        public OpResult CreateClient(string name, string pwd)
        {
            var or = new OpResult();
            //var httpClient = new HttpClient();
            //string url = @"http://localhost:44386/";
            string endPoint = $@"{_ApiClient.BaseAddress}api/Authentication/Admin/CreateClient";
            var httpClient = new HttpClient();
            var response  = new HttpResponseMessage();
            //var requestContent = new FormUrlEncodedContent(new[]
            //{
            //    new KeyValuePair<string,string>("username",name),
            //    new KeyValuePair<string,string>("password",pwd),
            //});
            var cm = new CreateClientModel() { Name = name, Password = pwd };
            string json = JsonConvert.SerializeObject(cm);
            var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
            try
            {
                response =  httpClient.PostAsync(endPoint,requestContent).Result;
            }
            catch (Exception ex)
            {
                Log.Warning($"{endPoint}");
                or.Message = $"Could not Create Client {name}, {ex.Message}";
                or.Success = false;
                Log.Warning(or.Message);
                return or;
            }
            if (response.IsSuccessStatusCode == false) {
                Log.Warning($"{endPoint}");
                or.Message = $"Could not createClient {name}, Response: {response.ReasonPhrase}";
                or.Success = false;
                Log.Warning(or.Message);
                return or;
            }
            dynamic responseData = JsonConvert.DeserializeObject<OpResult>(response.Content.ReadAsStringAsync().Result);
            or.Message = responseData.Message;
            return or;
        }
        class CreateClientModel
        {
            public string Name { get; set; }
            public string Password { get; set; }
        }
    }
}
