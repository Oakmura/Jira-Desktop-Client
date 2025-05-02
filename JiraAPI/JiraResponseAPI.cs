using Newtonsoft.Json;

namespace JiraClient.JiraAPI
{
    public class JiraResponseAPI
    {
        [JsonProperty("startAt")]
        public int StartAt { get; set; } // index of the first item returned in the page

        [JsonProperty("maxResults")]
        public int MaxResults { get; set; } // max number of issues a page can return

        [JsonProperty("total")]
        public int Total { get; set; } // total number of issues

        [JsonProperty("issues")]
        public List<JiraIssue> Issues { get; set; }

        public string ResponseTitle { get; set; }
    }

    public class JiraIssue
    {
        [JsonProperty("id")]
        public int ID { get; set; } // "id": "13688"

        [JsonProperty("key")]
        public string Key { get; set; } // "key": "VFS-36"

        [JsonProperty("fields")]
        public JiraIssueFields Fields { get; set; }
    }

    public class JiraIssueFields
    {
        [JsonProperty("parent")]
        public JiraIssue Parent { get; set; }

        [JsonProperty("subtasks")]
        public List<JiraIssue> SubTasks { get; set; }

        [JsonProperty("summary")]
        public string Summary { get; set; } // "ChatGPT 온라인 강의 및 전파교육"

        [JsonProperty("description")]
        public string Description { get; set; } // "ChatGPT 교육 수강\n* 전자 결제 제출 자료 (`교육훈련결과보고서`)\n** 전파 교육 사진\n** 참석자 서명 스캔본""

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

        [JsonProperty("resolution")]
        public Resolution Resolution { get; set; }

        [JsonProperty("issuelinks")]
        public List<IssueLink> IssueLinks { get; set; }

        [JsonProperty("created")]
        public string Created { get; set; }
        public string FormattedCreated => Created == null ? null : DateTime.Parse(Created).ToString("yyyy-MM-dd HH:mm");

        [JsonProperty("updated")]
        public string Updated { get; set; }
        public string FormattedUpdated => Updated == null ? null : DateTime.Parse(Updated).ToString("yyyy-MM-dd HH:mm");

        [JsonProperty("duedate")]
        public string DueDate { get; set; }
        public string FormattedDueDate => DueDate == null ? null : DateTime.Parse(DueDate).ToString("yyyy-MM-dd HH:mm");
    }

    public class Priority
    {
        [JsonProperty("id")]
        public string ID { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; } // "Medium"

        [JsonProperty("iconUrl")]
        public string IconURL { get; set; }
    }

    public class Status
    {
        [JsonProperty("id")]
        public string ID { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; } // "Open"

        [JsonProperty("statusCategory")]
        public StatusCategory StatusCategory { get; set; }
    }

    public class StatusCategory
    {
        [JsonProperty("id")]
        public string ID { get; set; }

        [JsonProperty("key")]
        public string Key { get; set; } // "done"

        [JsonProperty("name")]
        public string Name { get; set; } // "Done"
    }

    public class User // Add avatarUrls if needed
    {
        [JsonProperty("accountId")]
        public string AccountID { get; set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; set; } // "노재우"
    }

    public class IssueType // Add iconUrl if needed
    {
        [JsonProperty("id")]
        public string ID { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; } // Task, Task Request"

        [JsonProperty("iconUrl")]
        public string IconURL { get; set; }
        public byte[] IconImage { get; set; }
    }

    public class Project // Add avatarUrls if needed
    {
        [JsonProperty("id")]
        public string ID { get; set; }

        [JsonProperty("key")]
        public string Key { get; set; } // "VFS, SVEZ"

        [JsonProperty("name")]
        public string Name { get; set; } // "BIONOTE Support, 노재우-개인용"
    }

    public class Resolution
    {
        [JsonProperty("id")]
        public string ID { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; } // "Done"
    }

    public class IssueLink
    {
        [JsonProperty("id")]
        public string ID { get; set; }

        [JsonProperty("type")]
        public IssueLinkType Type { get; set; }

        [JsonProperty("inwardIssue")]
        public JiraIssue InwardIssue { get; set; }
        public string InwardDescription => InwardIssue != null ? $"{Type.Inward} {InwardIssue.Key}" : "";

        [JsonProperty("outwardIssue")]
        public JiraIssue OutwardIssue { get; set; }
        public string OutwardDescription => OutwardIssue != null ? $"{Type.Inward} {OutwardIssue.Key}" : "";
    }


    public class IssueLinkType
    {
        [JsonProperty("id")]
        public string ID { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; } // "Cloners"

        [JsonProperty("inward")]
        public string Inward { get; set; } // "is cloned by"

        [JsonProperty("outward")]
        public string Outward { get; set; } // "clones"
    }
}
