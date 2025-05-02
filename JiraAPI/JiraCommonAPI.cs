using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using JiraClient.Utilities;

namespace JiraClient.JiraAPI
{
    public static class JiraCommonAPI
    {
        public static HttpClient CreateHttpClient()
        {
            HttpClient client = new HttpClient();
            byte[] byteArray = Encoding.ASCII.GetBytes($"{Settings.UserName}:{Settings.ApiToken}");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            return client;
        }
    }
}
