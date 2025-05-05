using System.Net.Http;
using System.Net.Mail;
using System.Text;
using JiraClient.Utilities;
using Newtonsoft.Json;

namespace JiraClient.JiraAPI
{
    public class CreateJiraIssueRequest
    {
        [JsonProperty("fields")]
        public CreateJiraIssueFields Fields { get; set; }

        [JsonProperty("update", NullValueHandling = NullValueHandling.Ignore)]
        public CreateJiraIssueUpdate Update { get; set; }
    }

    public class CreateJiraIssueFields
    {
        [JsonProperty("project")]
        public Project Project { get; set; } // 사용 시 key 또는 id 설정

        [JsonProperty("issuetype")]
        public IssueType IssueType { get; set; } // id 사용

        [JsonProperty("summary")]
        public string Summary { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("labels", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Labels { get; set; }

        [JsonProperty("assignee", NullValueHandling = NullValueHandling.Ignore)]
        public User Assignee { get; set; }

        [JsonProperty("customfield_10015", NullValueHandling = NullValueHandling.Ignore)]
        public string StartDate { get; set; }  // Format: "yyyy-MM-dd"

        [JsonProperty("duedate", NullValueHandling = NullValueHandling.Ignore)]
        public string DueDate { get; set; }

        [JsonProperty("parent", NullValueHandling = NullValueHandling.Ignore)]
        public JiraIssue Parent { get; set; } // parent는 key만 포함하면 됨

        [JsonProperty("timetracking", NullValueHandling = NullValueHandling.Ignore)]
        public JiraTimeTracking Timetracking { get; set; }
    }

    public class CreateJiraIssueUpdate
    {
        [JsonProperty("worklog")]
        public List<JiraWorklogAdd> Worklog { get; set; }
    }

    public class JiraWorklogAdd
    {
        [JsonProperty("add")]
        public JiraWorklogEntry Add { get; set; }
    }

    public class JiraWorklogEntry
    {
        [JsonProperty("started")]
        public string Started { get; set; }

        [JsonProperty("timeSpent")]
        public string TimeSpent { get; set; }
    }

    public class JiraTimeTracking
    {
        [JsonProperty("originalEstimate")]
        public string OriginalEstimate { get; set; }

        [JsonProperty("remainingEstimate")]
        public string RemainingEstimate { get; set; }
    }

    public static class JiraCreateAPI
    {
        public static async Task<(bool, JiraIssue)> CreateJiraIssueAsync(CreateJiraIssueRequest jiraIssue)
        {
            string createIssueJson = JsonConvert.SerializeObject(jiraIssue);

            using (HttpClient client = JiraCommonAPI.CreateHttpClient())
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
    }
}
