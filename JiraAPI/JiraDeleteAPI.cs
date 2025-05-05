using JiraClient.Utilities;
using System.Net.Http;

namespace JiraClient.JiraAPI
{
    public static class JiraDeleteAPI
    {
        public static async Task<bool> DeleteIssueAsync(string issueKey)
        {
            using (HttpClient client = JiraCommonAPI.CreateHttpClient())
            {
                HttpResponseMessage response = await client.DeleteAsync($"{Settings.JiraBaseURL}/rest/api/2/issue/{issueKey}");

                if (response.IsSuccessStatusCode)
                {
                    Logger.Log(MessageType.Info, $"Issue {issueKey} 삭제 성공");
                    return true;
                }
                else
                {
                    string content = await response.Content.ReadAsStringAsync();
                    Logger.Log(MessageType.Error, $"이슈 삭제 실패: {response.StatusCode} - {content}");
                    return false;
                }
            }
        }
    }
}
