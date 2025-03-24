using System.Collections.ObjectModel;
using JiraClient.Common;
using JiraClient.Models;
using JiraClient.Utilities;

namespace JiraClient.ViewModels
{
    public class Issue : ViewModelBase
    {
        public string ID { get; set; }
        public string Key { get; set; }
        
    }

    public class EpicIssue : Issue
    {
        public ObservableCollection<TaskIssue> Tasks { get; set; } = new ObservableCollection<TaskIssue>();
    }

    public class TaskIssue : Issue
    {
        public ObservableCollection<SubTaskIssue> SubTasks { get; set; } = new ObservableCollection<SubTaskIssue>();
    }

    public class SubTaskIssue : Issue
    {
        private JiraIssueResponse mIssue;
        public JiraIssueResponse Issue
        {
            get => mIssue;
            set
            {
                if (mIssue != value)
                {
                    mIssue = value;
                    OnPropertyChanged();
                }
            }
        }

        public SubTaskIssue(JiraIssueResponse issue)
        {
            Issue = issue;
        }
    }

    public class IssueListVM : ViewModelBase
    {
        public ObservableCollection<EpicIssue> EpicIssues { get; set; }

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
