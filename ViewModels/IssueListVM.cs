using System.Collections.ObjectModel;
using JiraClient.Common;
using JiraClient.Models;
using JiraClient.Utilities;

namespace JiraClient.ViewModels
{
    // TODO: should change to ViewModelBase if one to dynamically update the list
    public class HierarchicalIssueList
    {
        public string Name { get; set; }
        public List<JiraIssue> Children { get; set; } = new List<JiraIssue>();
    }

    public class IssueListVM : ViewModelBase
    {
        public ObservableCollection<HierarchicalIssueList> HierarchicalIssueList { get; private set; } = new ObservableCollection<HierarchicalIssueList>();

        public IssueListVM()
        {
        }

        public void Setup(Dictionary<int, JiraIssue> jiraIssuesByID, Dictionary<string, List<int>> jiraIssuesByJQL)
        {
            HierarchicalIssueList.Clear();
            foreach (var issuesByJQL in jiraIssuesByJQL)
            {
                HierarchicalIssueList.Add(new HierarchicalIssueList
                {
                    Name = issuesByJQL.Key,
                    Children = issuesByJQL.Value.Select(id => jiraIssuesByID[id]).ToList()
                });
            }
        }

        public void OnRefreshCommand(Dictionary<int, JiraIssue> jiraIssuesByID, Dictionary<string, List<int>> jiraIssuesByJQL)
        {
            Logger.Log(MessageType.Info, "Refreshing IssueList ViewModel");

            Setup(jiraIssuesByID, jiraIssuesByJQL);
        }
    }
}
