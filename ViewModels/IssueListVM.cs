using System.Collections.ObjectModel;
using JiraClient.Common;
using JiraClient.Models;
using JiraClient.Utilities;

namespace JiraClient.ViewModels
{
    public class IssueListVM : ViewModelBase
    {
        public ObservableCollection<JiraIssueResponse> JiraIssueResponses { get; set; } = new ObservableCollection<JiraIssueResponse>();

        public IssueListVM()
        {
        }

        public void OnRefreshCommand()
        {
            _ = Logger.Log(MessageType.Info, "Refreshing IssueList ViewModel");
        }
    }
}
