using System.Net.Http;
using System.Text;
using JiraClient.Utilities;
using Newtonsoft.Json;

namespace JiraClient.JiraAPI
{
    public static class JiraCreateAPI
    {
        public static async Task<(bool, JiraIssue)> CreateJiraIssueAsync(JiraIssue jiraIssue)
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
