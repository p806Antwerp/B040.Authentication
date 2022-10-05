using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
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
        public async Task<AuthenticatedUser> Authenticate(string username,string password)
        {
            var data = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("grant_type","password"),
                new KeyValuePair<string,string>("username",username),
                new KeyValuePair<string,string>("password",password),
            });
            using (HttpResponseMessage response = await _APIClient.PostAsync("/Token",data))
            {
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsAsync<AuthenticatedUser>();
                    return result;
                }
                else
                {
                    throw new Exception(response.ReasonPhrase);
                }
            }
        }
    }
    public class AuthenticatedUser
    {
        public string Access_Token{ get; set; }
        public string UserName { get; set; }

    }

}