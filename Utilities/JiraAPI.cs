using JiraClient.Models;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.IO;
using System.Collections.Generic;

namespace JiraClient.Utilities
{
    public static class JiraAPI
    {
        public static async Task<Dictionary<int, JiraIssue>> fetchAllJiraIssues()
        {
            Dictionary<int, JiraIssue> jiraIssues = new Dictionary<int, JiraIssue>(4096);
            int maxResults = 100;
            int total = 0;

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(Settings.JiraBaseURL);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                string authInfo = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{Settings.UserName}:{Settings.ApiToken}"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authInfo);

                // 이슈 갯수 파악 (1개만 가져옴)
                Stopwatch swFetchOne = Stopwatch.StartNew();
                Stopwatch swFetchAll = Stopwatch.StartNew();

                string firstUrl = $"/rest/api/latest/search?startAt=0&maxResults=1";
                HttpResponseMessage firstResponse = await client.GetAsync(firstUrl);
                firstResponse.EnsureSuccessStatusCode();

                string firstContent = await firstResponse.Content.ReadAsStringAsync();
                JObject firstJson = JObject.Parse(firstContent);
                total = firstJson["total"]?.Value<int>() ?? 0;

                swFetchOne.Stop();

                Logger.Log(MessageType.Info, $"총 이슈 수: {total}");
                Logger.Log(MessageType.Info, $"1 Issue fetch took {swFetchOne.Elapsed} seconds");

                // 전체 이슈 가져오기
                List<Task<HttpResponseMessage>> tasks = new List<Task<HttpResponseMessage>>();
                for (int startAt = 0; startAt < total; startAt += maxResults)
                {
                    string url = $"/rest/api/latest/search?startAt={startAt}&maxResults={maxResults}";
                    tasks.Add(client.GetAsync(url));
                }

                HttpResponseMessage[] responses = await Task.WhenAll(tasks);
                foreach (HttpResponseMessage response in responses)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        JObject jsonResult = JObject.Parse(result);
                        JiraIssueResponse JiraResponse = JsonConvert.DeserializeObject<JiraIssueResponse>(result);

                        foreach (JiraIssue jiraIssue in JiraResponse.Issues)
                        {
                            jiraIssues[jiraIssue.ID] = jiraIssue;
                        }
                    }
                    else
                    {
                        Logger.Log(MessageType.Error, $"{response.StatusCode} - {response.ReasonPhrase}");
                    }
                }

                swFetchAll.Stop();
                Logger.Log(MessageType.Info, $"{total} Issue fetch took {swFetchAll.Elapsed} seconds");

                return jiraIssues;
            }
        }

        // <JQL, [issue keys]>
        public static async Task<Dictionary<string, List<int>>> fetchAllJiraIssuesJQL()
        {
            string[] JQLs;
            try
            {
                if (!Path.Exists(Settings.USER_JQL_PATH))
                {
                    Logger.Log(MessageType.Error, $"User JQL Path: {Settings.USER_JQL_PATH} does not exist");
                    return null;
                }

                JQLs = File.ReadAllLines(Settings.USER_JQL_PATH);
            }
            catch (Exception e)
            {
                Logger.Log(MessageType.Error, e.Message);
                return null;
            }

            Dictionary<string, List<int>> issueKeysByJQL = new Dictionary<string, List<int>>(128);
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(Settings.JiraBaseURL);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                string authInfo = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{Settings.UserName}:{Settings.ApiToken}"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authInfo);

                Stopwatch sw = Stopwatch.StartNew();

                List<string> filters = new List<string>(64);
                List<Task<HttpResponseMessage>> tasks = new List<Task<HttpResponseMessage>>(128);
                for (int i = 0; i < JQLs.Length; ++i)
                {
                    if (JQLs[i].Trim().Length == 0)
                    {
                        continue;
                    }

                    string filterName = JQLs[i].Split(',')[0];
                    string filterJQL = JQLs[i].Split(',')[1];

                    string apiURL = $"/rest/api/2/search?jql={filterJQL}&maxResults=100&fields=id";
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
                        JiraIssueResponse jiraResponse = JsonConvert.DeserializeObject<JiraIssueResponse>(result);
                        string filterName = filters[i];

                        foreach (JiraIssue jiraIssue in jiraResponse.Issues)
                        {
                            if (!issueKeysByJQL.ContainsKey(filterName))
                            {
                                issueKeysByJQL[filterName] = new List<int>(1024);
                            }                            
                            
                            issueKeysByJQL[filterName].Add(jiraIssue.ID);
                        }
                    }
                    else
                    {
                        Logger.Log(MessageType.Error, $"{response.StatusCode} - {response.ReasonPhrase}");
                    }
                }

                sw.Stop();
                Logger.Log(MessageType.Info, $"JQL Issue fetch took {sw.Elapsed} seconds");

                return issueKeysByJQL;
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
                Logger.Log(MessageType.Info, $"Issue fetch took {sw.Elapsed} seconds");

                return issueTypes;
            }
        }
    }
}
