using Newtonsoft.Json;

namespace JiraClient.ViewModels
{
    public class JiraIssueResponse
    {
        [JsonProperty("startAt")]
        public int StartAt { get; set; }

        [JsonProperty("maxResults")]
        public int MaxResults { get; set; }

        [JsonProperty("total")]
        public int Total { get; set; }

        [JsonProperty("issues")]
        public List<JiraIssue> Issues { get; set; }
    }

    public class JiraIssue
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("fields")]
        public JiraIssueFields Fields { get; set; }
    }

    public class JiraIssueFields
    {
        [JsonProperty("summary")]
        public string Summary { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("priority")]
        public Priority Priority { get; set; }

        [JsonProperty("status")]
        public Status Status { get; set; }

        [JsonProperty("assignee")]
        public User Assignee { get; set; }

        [JsonProperty("reporter")]
        public User Reporter { get; set; }

        [JsonProperty("creator")]
        public User Creator { get; set; }

        [JsonProperty("labels")]
        public List<string> Labels { get; set; }

        [JsonProperty("issuetype")]
        public IssueType IssueType { get; set; }

        [JsonProperty("project")]
        public Project Project { get; set; }

        [JsonProperty("created")]
        public string Created { get; set; }
        public string FormattedCreated => DateTime.Parse(Created).ToString("yyyy-MM-dd HH:mm");

        [JsonProperty("updated")]
        public string Updated { get; set; }

        [JsonProperty("resolution")]
        public Resolution Resolution { get; set; }

        [JsonProperty("customfield_10010")]
        public CustomField10010 CustomField10010 { get; set; }

        [JsonProperty("issuelinks")]
        public List<IssueLink> IssueLinks { get; set; }
    }

    public class Priority
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("iconUrl")]
        public string IconUrl { get; set; }
    }

    public class Status
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("iconUrl")]
        public string IconUrl { get; set; }

        [JsonProperty("statusCategory")]
        public StatusCategory StatusCategory { get; set; }
    }

    public class StatusCategory
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("colorName")]
        public string ColorName { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class User
    {
        [JsonProperty("accountId")]
        public string AccountId { get; set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        [JsonProperty("timeZone")]
        public string TimeZone { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }
    }

    public class IssueType
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("iconUrl")]
        public string IconUrl { get; set; }
    }

    public class Project
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("projectTypeKey")]
        public string ProjectTypeKey { get; set; }
    }

    public class Resolution
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class CustomField10010
    {
        [JsonProperty("currentStatus")]
        public CustomFieldStatus CurrentStatus { get; set; }
    }

    public class CustomFieldStatus
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("statusCategory")]
        public string StatusCategory { get; set; }

        [JsonProperty("statusDate")]
        public CustomFieldStatusDate StatusDate { get; set; }
    }

    public class CustomFieldStatusDate
    {
        [JsonProperty("iso8601")]
        public string Iso8601 { get; set; }

        [JsonProperty("jira")]
        public string Jira { get; set; }

        [JsonProperty("friendly")]
        public string Friendly { get; set; }
    }

    public class IssueLink
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("type")]
        public IssueLinkType Type { get; set; }

        [JsonProperty("outwardIssue")]
        public OutwardIssue OutwardIssue { get; set; }
    }

    public class IssueLinkType
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("inward")]
        public string Inward { get; set; }

        [JsonProperty("outward")]
        public string Outward { get; set; }
    }

    public class OutwardIssue
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("fields")]
        public OutwardIssueFields Fields { get; set; }
    }

    public class OutwardIssueFields
    {
        [JsonProperty("summary")]
        public string Summary { get; set; }

        [JsonProperty("status")]
        public Status Status { get; set; }

        [JsonProperty("priority")]
        public Priority Priority { get; set; }

        [JsonProperty("issuetype")]
        public IssueType IssueType { get; set; }
    }
}
