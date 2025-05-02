using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http;
using System.IO;
using JiraClient.Utilities;

namespace JiraClient.JiraAPI
{
    public static class JiraReadAPI
    {
        public static async Task<Dictionary<int, JiraIssue>> ReadAllJiraIssues()
        {
            Dictionary<int, JiraIssue> jiraIssues = new Dictionary<int, JiraIssue>(4096);
            int maxResults = 100;
            int total = 0;

            using (HttpClient client = JiraCommonAPI.CreateHttpClient())
            {
                client.BaseAddress = new Uri(Settings.JiraBaseURL);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

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
                Logger.Log(MessageType.Info, $"총 이슈 수 파악 took {swFetchOne.Elapsed} seconds");

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
                        JiraResponseAPI JiraResponse = JsonConvert.DeserializeObject<JiraResponseAPI>(result);

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
        public static async Task<Dictionary<string, List<int>>> ReadAllJiraIssuesByJQL()
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
            using (HttpClient client = JiraCommonAPI.CreateHttpClient())
            {
                client.BaseAddress = new Uri(Settings.JiraBaseURL);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

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
                        JiraResponseAPI jiraResponse = JsonConvert.DeserializeObject<JiraResponseAPI>(result);
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

        public static async Task<List<Project>> ReadProjectsAsync()
        {
            using (HttpClient client = JiraCommonAPI.CreateHttpClient())
            {
                var response = await client.GetAsync($"{Settings.JiraBaseURL}/rest/api/latest/project");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var projects = JsonConvert.DeserializeObject<List<Project>>(json);

                Logger.Log(MessageType.Info, $"프로젝트 {projects.Count}개 가져옴");
                return projects;
            }
        }

        public static async Task<List<IssueType>> ReadIssueTypesAsync()
        {
            using (HttpClient client = JiraCommonAPI.CreateHttpClient())
            {
                var response = await client.GetAsync($"{Settings.JiraBaseURL}/rest/api/latest/issuetype");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var issueTypes = JsonConvert.DeserializeObject<List<IssueType>>(json);

                Logger.Log(MessageType.Info, $"이슈 타입 {issueTypes.Count}개 가져옴");
                return issueTypes;
            }
        }

        public static async Task<List<string>> ReadLabelsAsync()
        {
            using (HttpClient client = JiraCommonAPI.CreateHttpClient())
            {
                HttpResponseMessage response = await client.GetAsync($"{Settings.JiraBaseURL}/rest/api/latest/label");
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();
                JObject jobject = JObject.Parse(json);

                List<string> labels = jobject["values"].ToObject<List<string>>();

                Logger.Log(MessageType.Info, $"라벨 {labels.Count}개 가져옴");
                return labels;
            }
        }

        public static async Task<List<Priority>> ReadPrioritiesAsync()
        {
            using (HttpClient client = JiraCommonAPI.CreateHttpClient())
            {
                var response = await client.GetAsync($"{Settings.JiraBaseURL}/rest/api/latest/priority");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var priorities = JsonConvert.DeserializeObject<List<Priority>>(json);

                Logger.Log(MessageType.Info, $"우선순위 {priorities.Count}개 가져옴");
                return priorities;
            }
        }

        public static async Task<List<User>> ReadAssignableUsersAsync()
        {
            using (HttpClient client = JiraCommonAPI.CreateHttpClient())
            {
                // DEFAULT project key로 C10을 사용
                string projectKey = Settings.DEFAULT_PROJECT_KEY ?? "C10";

                var response = await client.GetAsync($"{Settings.JiraBaseURL}/rest/api/latest/user/assignable/search?project={projectKey}");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var users = JsonConvert.DeserializeObject<List<User>>(json);

                Logger.Log(MessageType.Info, $"Assignee {users.Count}명 가져옴");
                return users;
            }
        }
    }
}
