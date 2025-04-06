using JiraClient.Models;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.IO;
using System.Windows.Media.Imaging;
using System.Security.Policy;
using System.Drawing;

namespace JiraClient.Utilities
{
    public static class JiraAPI
    {
        public static async Task<Dictionary<int, JiraIssue>> fetchAllJiraIssues()
        {
            Dictionary<int, JiraIssue> jiraIssues = new Dictionary<int, JiraIssue>(4096);
            int maxResults = 100;
            int total = 0;

            using (HttpClient client = createHttpClient())
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
            using (HttpClient client = createHttpClient())
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

        public static async Task<(bool, JiraIssue)> CreateJiraIssueAsync(JiraIssue jiraIssue)
        {
            string createIssueJson = JsonConvert.SerializeObject(jiraIssue);

            using (HttpClient client = createHttpClient())
            {
                StringContent content = new StringContent(createIssueJson, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync($"{Settings.JiraBaseURL}/rest/api/latest/issue", content);

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    JiraIssue createdIssue = JsonConvert.DeserializeObject<JiraIssue>(responseContent);

                    Logger.Log(MessageType.Info, $"Created issue: {createdIssue.Key}");

                    return (true, createdIssue);
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();
                    Logger.Log(MessageType.Error, $"이슈 생성 실패: {response.StatusCode}\n{error}");

                    return (false, null);
                }
            }
        }

        public static async Task<List<Project>> FetchProjectsAsync()
        {
            using (HttpClient client = createHttpClient())
            {
                var response = await client.GetAsync($"{Settings.JiraBaseURL}/rest/api/latest/project");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var projects = JsonConvert.DeserializeObject<List<Project>>(json);

                Logger.Log(MessageType.Info, $"프로젝트 {projects.Count}개 가져옴");
                return projects;
            }
        }

        public static async Task<List<IssueType>> FetchIssueTypesAsync()
        {
            using (HttpClient client = createHttpClient())
            {
                var response = await client.GetAsync($"{Settings.JiraBaseURL}/rest/api/latest/issuetype");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var issueTypes = JsonConvert.DeserializeObject<List<IssueType>>(json);

                Logger.Log(MessageType.Info, $"이슈 타입 {issueTypes.Count}개 가져옴");
                return issueTypes;
            }
        }

        /* TODO: use this to fetch also the icons (instead of FetchIssueTypesAsync)
        public static async Task<List<IssueType>> FetchIssueTypesAsync()
        {
            using (HttpClient client = createHttpClient())
            {
                // 1. 이슈 타입 정보 가져오기
                var response = await client.GetAsync($"{Settings.JiraBaseURL}/rest/api/latest/issuetype");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var issueTypes = JsonConvert.DeserializeObject<List<IssueType>>(json);

                // 2. 각각의 이슈 타입 아이콘 가져오기
                using (HttpClient imageClient = new HttpClient())
                {
                    foreach (var issueType in issueTypes)
                    {
                        if (!string.IsNullOrEmpty(issueType.IconURL))
                        {
                            try
                            {
                                byte[] icon = await response.Content.ReadAsByteArrayAsync();
                                issueType.IconImage = icon;
                            }
                            catch (Exception ex)
                            {
                                Logger.Log(MessageType.Warning, $"아이콘 다운로드 실패: {issueType.Name}, {ex.Message}");
                            }
                        }
                    }
                }

                Logger.Log(MessageType.Info, $"이슈 타입 {issueTypes.Count}개 가져옴");
                return issueTypes;
            }
        }
        */

        public static async Task TestFetchIssueTypeIconsAsync()
        {
            string imageUrl = "https://sdb-jira.atlassian.net/rest/api/2/universal_avatar/view/type/issuetype/avatar/10309?size=medium";

            using (HttpClient imageClient = new HttpClient())
            {
                try
                {
                    var response = await imageClient.GetAsync(imageUrl);
                    response.EnsureSuccessStatusCode();

                    byte[] data = await response.Content.ReadAsByteArrayAsync();
                    Logger.Log(MessageType.Info, $"이미지 다운로드 성공: {imageUrl}, 크기: {data.Length} bytes");
                }
                catch (Exception ex)
                {
                    Logger.Log(MessageType.Error, $"이미지 다운로드 실패: {ex.Message}");
                }
            }
        }

        public static async Task<List<string>> FetchLabelsAsync()
        {
            using (HttpClient client = createHttpClient())
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

        public static async Task<List<Priority>> FetchPrioritiesAsync()
        {
            using (HttpClient client = createHttpClient())
            {
                var response = await client.GetAsync($"{Settings.JiraBaseURL}/rest/api/latest/priority");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var priorities = JsonConvert.DeserializeObject<List<Priority>>(json);

                Logger.Log(MessageType.Info, $"우선순위 {priorities.Count}개 가져옴");
                return priorities;
            }
        }

        public static async Task<List<User>> FetchAssignableUsersAsync()
        {
            using (HttpClient client = createHttpClient())
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

        private static HttpClient createHttpClient()
        {
            HttpClient client = new HttpClient();
            byte[] byteArray = Encoding.ASCII.GetBytes($"{Settings.UserName}:{Settings.ApiToken}");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            return client;
        }
    }
}
