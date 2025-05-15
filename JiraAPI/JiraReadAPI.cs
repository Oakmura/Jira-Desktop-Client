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
        // 단일 이슈에 대해서는 attachment와 comment 필드를 모두 받아올 수 있음
        public static async Task<JiraIssue> ReadSingleJiraIssueOrNull(string jiraIssueKey)
        {
            using (HttpClient client = JiraCommonAPI.CreateHttpClient())
            {
                HttpResponseMessage response = await client.GetAsync($"{Settings.JiraBaseURL}/rest/api/2/issue/{jiraIssueKey}");

                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    JiraIssue issue = JsonConvert.DeserializeObject<JiraIssue>(content);
                    Logger.Log(MessageType.Info, $"이슈 조회 성공: {jiraIssueKey}");
                    return issue;
                }
                else
                {
                    string content = await response.Content.ReadAsStringAsync();
                    Logger.Log(MessageType.Error, $"이슈 조회 실패: {response.StatusCode} - {content}");
                    return null;
                }
            }
        }

        public static async Task<Dictionary<string, JiraIssue>> ReadAllJiraIssues()
        {
            Dictionary<string, JiraIssue> jiraIssues = new Dictionary<string, JiraIssue>(4096);
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

                string firstURL = $"/rest/api/latest/search?startAt=0&maxResults=1";
                HttpResponseMessage firstResponse = await client.GetAsync(firstURL);
                firstResponse.EnsureSuccessStatusCode();

                string firstContent = await firstResponse.Content.ReadAsStringAsync();
                JObject firstJson = JObject.Parse(firstContent);
                total = firstJson["total"]?.Value<int>() ?? 0;

                swFetchOne.Stop();
                Logger.Log(MessageType.Info, $"총 이슈 수: {total}");
                Logger.Log(MessageType.Info, $"총 이슈 수 파악 took {swFetchOne.Elapsed} seconds");

                // 전체 이슈 가져오기
                string fields = "id,key,summary,description,comment,attachment,assignee,reporter,creator,parent,subtasks,status,labels,issuetype,project,resolution,issuelinks,created,updated,duedate, environment";
                List<Task<HttpResponseMessage>> tasks = new List<Task<HttpResponseMessage>>();
                for (int startAt = 0; startAt < total; startAt += maxResults)
                {
                    string url = $"/rest/api/2/search?startAt={startAt}&maxResults={maxResults}&fields={fields}";
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

        public static async Task<List<string>> ReadAllJiraIssuesByJQL(string JQL)
        {
            List<string> issueIDs = new List<string>();

            using (HttpClient client = JiraCommonAPI.CreateHttpClient())
            {
                string apiURL = $"{Settings.JiraBaseURL}/rest/api/2/search?jql={Uri.EscapeDataString(JQL)}&maxResults=100&fields=id";

                HttpResponseMessage response = await client.GetAsync(apiURL);
                if (response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    JiraResponseAPI jiraResponse = JsonConvert.DeserializeObject<JiraResponseAPI>(result);

                    foreach (JiraIssue issue in jiraResponse.Issues)
                    {
                        issueIDs.Add(issue.ID);
                    }
                }
                else
                {
                    Logger.Log(MessageType.Error, $"[ReadAllJiraIssuesByJQL] {response.StatusCode} - {response.ReasonPhrase}");
                }
            }

            return issueIDs;
        }


        // <JQL, [issue keys]>
        public static async Task<Dictionary<string, List<string>>> ReadAllJiraIssuesByJQL()
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

            Dictionary<string, List<string>> issueKeysByJQL = new Dictionary<string, List<string>>(128);
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
                                issueKeysByJQL[filterName] = new List<string>(1024);
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

        public static async Task<List<Attachment>> RefreshAttachmentsAsync(string issueKey)
        {
            using (HttpClient client = JiraCommonAPI.CreateHttpClient())
            {
                string url = $"{Settings.JiraBaseURL}/rest/api/latest/issue/{issueKey}?fields=attachment";
                HttpResponseMessage response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    JiraIssue updatedIssue = JsonConvert.DeserializeObject<JiraIssue>(json);

                    Logger.Log(MessageType.Info, $"Attachment list refreshed: {issueKey}");
                    return updatedIssue.Fields.Attachment;
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();
                    Logger.Log(MessageType.Error, $"Attachment refresh failed: {response.StatusCode}\n{error}");
                    return null;
                }
            }
        }

        // TODO: change to appropriate place
        public class CreateMetaResponse
        {
            [JsonProperty("projects")]
            public List<ProjectWithIssueTypes> Projects { get; set; }
        }

        public class ProjectWithIssueTypes
        {
            [JsonProperty("key")]
            public string Key { get; set; }

            [JsonProperty("issuetypes")]
            public List<IssueType> IssueTypes { get; set; }
        }

        public static async Task<Dictionary<string, List<IssueType>>> ReadIssueTypesByProjectAsync(List<string> projectKeys)
        {
            Dictionary<string, List<IssueType>> issueTypeMap = new Dictionary<string, List<IssueType>>();

            if (projectKeys == null || projectKeys.Count == 0)
            {
                Logger.Log(MessageType.Warning, "Project key 리스트가 비어 있음");
                return issueTypeMap;
            }

            using (HttpClient client = JiraCommonAPI.CreateHttpClient())
            {
                string joinedKeys = string.Join(",", projectKeys);
                string url = $"{Settings.JiraBaseURL}/rest/api/2/issue/createmeta?projectKeys={joinedKeys}";

                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();
                CreateMetaResponse createMeta = JsonConvert.DeserializeObject<CreateMetaResponse>(json);

                if (createMeta?.Projects != null)
                {
                    foreach (ProjectWithIssueTypes project in createMeta.Projects)
                    {
                        if (!string.IsNullOrEmpty(project.Key) && project.IssueTypes != null)
                        {
                            issueTypeMap[project.Key] = project.IssueTypes;
                            Logger.Log(MessageType.Info, $"이슈 타입 {project.IssueTypes.Count}개 가져옴 (Project: {project.Key})");
                        }
                    }
                }
            }

            return issueTypeMap;
        }
        public static async Task<Dictionary<string, List<User>>> ReadAssignableUsersAsync(List<string> projectKeys)
        {
            Debug.Assert(projectKeys != null && projectKeys.Count != 0, "ReadAssignableUsersAsync(projectKeys) : project Keys is null or empty");

            using (HttpClient client = JiraCommonAPI.CreateHttpClient())
            {
                List<Task<(string, List<User>)>> tasks = new List<Task<(string, List<User>)>>();

                foreach (string projectKey in projectKeys)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        string project = string.IsNullOrWhiteSpace(projectKey) ? (Settings.DEFAULT_PROJECT_KEY ?? "C10") : projectKey;
                        HttpResponseMessage response = await client.GetAsync($"{Settings.JiraBaseURL}/rest/api/latest/user/assignable/search?project=" + project);
                        response.EnsureSuccessStatusCode();

                        string json = await response.Content.ReadAsStringAsync();
                        List<User> userList = JsonConvert.DeserializeObject<List<User>>(json);

                        Logger.Log(MessageType.Info, "Assignee " + userList.Count + "명 가져옴 (Project: " + project + ")");
                        return (project, userList);
                    }));
                }

                (string, List<User>)[] results = await Task.WhenAll(tasks);

                Dictionary<string, List<User>> resultDict = new Dictionary<string, List<User>>();
                foreach ((string project, List<User> userList) in results)
                {
                    resultDict[project] = userList;
                }

                return resultDict;
            }
        }

        public static async Task<List<User>> ReadAssignableUsersAsync(string projectKey)
        {
            using (HttpClient client = JiraCommonAPI.CreateHttpClient())
            {
                // DEFAULT project key로 C10을 사용
                string finalProjectKey = string.IsNullOrWhiteSpace(projectKey) ? (Settings.DEFAULT_PROJECT_KEY ?? "C10") : projectKey;
                HttpResponseMessage response = await client.GetAsync($"{Settings.JiraBaseURL}/rest/api/latest/user/assignable/search?project={finalProjectKey}");
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();
                List<User> users = JsonConvert.DeserializeObject<List<User>>(json);

                Logger.Log(MessageType.Info, $"Assignee {users.Count}명 가져옴 (Project: {finalProjectKey})");
                return users;
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
    }
}
