using JiraClient.Models;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.IO;

namespace JiraClient.Utilities
{
    public static class JiraAPI
    {
        public static async Task<List<JiraIssueResponse>> fetchAllJiraIssues()
        {
            string[] JQLs;
            List<JiraIssueResponse> jiraIssueResponses = new List<JiraIssueResponse>(128);

            try
            {
                if (!Path.Exists(Settings.USER_JQL_PATH))
                {
                    _ = Logger.Log(MessageType.Error, $"User JQL Path: {Settings.USER_JQL_PATH} does not exist");
                    return jiraIssueResponses;
                }

                JQLs = File.ReadAllLines(Settings.USER_JQL_PATH);
            }
            catch (Exception e)
            {
                _ = Logger.Log(MessageType.Error, e.Message);
                return jiraIssueResponses;
            }

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(Settings.JiraBaseURL);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                string authInfo = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{Settings.UserName}:{Settings.ApiToken}"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authInfo);

                Stopwatch sw = Stopwatch.StartNew();

                List<string> filters = new List<string>(64);
                List<Task<HttpResponseMessage>> tasks = new List<Task<HttpResponseMessage>>();
                for (int i = 0; i < JQLs.Length; ++i)
                {
                    if (JQLs[i].Trim().Length == 0)
                    {
                        continue;
                    }

                    string filterName = JQLs[i].Split(',')[0];
                    string filterJQL = JQLs[i].Split(',')[1];

                    string apiURL = $"/rest/api/2/search?jql={filterJQL}&maxResults=100";
                    tasks.Add(client.GetAsync(apiURL));
                    filters.Add(filterName);
                }

                HttpResponseMessage[] responses = await Task.WhenAll(tasks);
                for (int i = 0; i < responses.Length; ++i)
                {
                    HttpResponseMessage response = responses[i];
                    if (response.IsSuccessStatusCode)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        JObject jsonResult = JObject.Parse(result);
                        JiraIssueResponse JiraResponse = JsonConvert.DeserializeObject<JiraIssueResponse>(result);
                        JiraResponse.ResponseTitle = $"{filters[i]} ({JiraResponse.Issues.Count} Issues)";

                        jiraIssueResponses.Add(JiraResponse);
                    }
                    else
                    {
                        _ = Logger.Log(MessageType.Error, $"{response.StatusCode} - {response.ReasonPhrase}");
                    }
                }

                sw.Stop();
                _ = Logger.Log(MessageType.Info, $"Issue fetch took {sw.Elapsed} seconds");

                return jiraIssueResponses;
            }
        }

        public static async Task<List<string>> fetchAllJiraIssueTypes()
        {
            List<string> issueTypes = new List<string>(64);

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(Settings.JiraBaseURL);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                string authInfo = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{Settings.UserName}:{Settings.ApiToken}"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authInfo);

                Stopwatch sw = Stopwatch.StartNew();

                // TODO: 아직 구현되지 않음

                sw.Stop();
                _ = Logger.Log(MessageType.Info, $"Issue fetch took {sw.Elapsed} seconds");

                return issueTypes;
            }
        }
    }
}
