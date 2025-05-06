using JiraClient.Utilities;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace JiraClient.JiraAPI
{
    public static class JiraUpdateAPI
    {
        public class UpdateJiraIssueRequest
        {
            [JsonProperty("fields", NullValueHandling = NullValueHandling.Ignore)]
            public UpdateJiraIssueFields Fields { get; set; }

            [JsonProperty("update", NullValueHandling = NullValueHandling.Ignore)]
            public UpdateJiraIssueUpdate Update { get; set; }
        }

        public class UpdateJiraIssueFields
        {
            [JsonProperty("issuetype", NullValueHandling = NullValueHandling.Ignore)]
            public IssueType IssueType { get; set; } // id 또는 name 사용

            [JsonProperty("summary", NullValueHandling = NullValueHandling.Ignore)]
            public string Summary { get; set; }

            [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
            public string Description { get; set; }

            [JsonProperty("labels", NullValueHandling = NullValueHandling.Ignore)]
            public List<string> Labels { get; set; }

            [JsonProperty("timetracking", NullValueHandling = NullValueHandling.Ignore)]
            public JiraTimeTracking Timetracking { get; set; }

            [JsonProperty("duedate", NullValueHandling = NullValueHandling.Ignore)]
            public string DueDate { get; set; } // yyyy-MM-dd

            [JsonProperty("assignee", NullValueHandling = NullValueHandling.Ignore)]
            public User Assignee { get; set; }
        }

        public class UpdateJiraIssueUpdate
        {
            [JsonProperty("attachment", NullValueHandling = NullValueHandling.Ignore)]
            public List<JiraAttachmentAdd> Attachment { get; set; }

            [JsonProperty("comment", NullValueHandling = NullValueHandling.Ignore)]
            public List<JiraCommentAdd> Comment { get; set; }
        }

        public class JiraAttachmentAdd
        {
            [JsonProperty("add")]
            public JiraAttachment Add { get; set; }
        }

        public class JiraAttachment
        {
            [JsonProperty("filename")]
            public string FileName { get; set; }

            [JsonProperty("content")]
            public string ContentUrl { get; set; }
        }

        public class JiraCommentAdd
        {
            [JsonProperty("add")]
            public JiraComment Add { get; set; }
        }

        public class JiraComment
        {
            [JsonProperty("body")]
            public string Body { get; set; }
        }

        public static async Task<bool> UpdateIssueAsync(string issueKey, UpdateJiraIssueRequest request)
        {
            string updateIssueJson = JsonConvert.SerializeObject(request);

            using (HttpClient client = JiraCommonAPI.CreateHttpClient())
            {
                StringContent content = new StringContent(updateIssueJson, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PutAsync($"{Settings.JiraBaseURL}/rest/api/latest/issue/{issueKey}", content);

                if (response.IsSuccessStatusCode)
                {
                    Logger.Log(MessageType.Info, $"Updated issue: {issueKey}");
                    return true;
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();
                    Logger.Log(MessageType.Error, $"이슈 업데이트 실패: {response.StatusCode}\n{error}");
                    return false;
                }
            }
        }
    }
}
