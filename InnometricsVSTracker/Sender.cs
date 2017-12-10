using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace InnometricsVSTracker
{
    class Sender
    {
        private string _url;

        public Sender(string url)
        {
            _url = url;
        }

        public Sender()
        {
            var config = new ConfigFile();
            config.Read();
            _url = config.Url;
        }

        public string GetToken(string username, string password)
        {
            var client = new HttpClient();
            var values = new Dictionary<string, string>
            {
                { "username", username },
                { "password", password }
            };
            var content = new FormUrlEncodedContent(values);
            var response = client.PostAsync(_url + "/api-token-auth/", content).Result;

            if (!response.IsSuccessStatusCode) return null;
            var resp = response.Content.ReadAsStringAsync().Result;
            return (string)JObject.Parse(resp)["token"];
        }

        public bool SendActivities(List<Activity> activitiesList)
        {
            var client = new HttpClient();
            var token = new ConfigFile().Token;

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", token);
            var activities = new StringContent(SerializeActivities(activitiesList), Encoding.UTF8, "application/json");
            var response = client.PostAsync(_url + "/activities/", activities).Result;

            return response.IsSuccessStatusCode;
        }

        private string SerializeActivities(List<Activity> activitiesList)
        {
            var temp = new Dictionary<String, List<Activity>> { { "activities", activitiesList } };
            return JsonConvert.SerializeObject(temp);
        }
    }
}