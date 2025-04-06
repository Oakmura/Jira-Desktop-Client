using JiraClient.Common;
using JiraClient.Models;
using JiraClient.Utilities;

namespace JiraClient.ViewModels
{
    public class ViewIssueVM : ViewModelBase
    {
        private JiraIssue mIssue;
        public JiraIssue Issue
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

        private string mIssuePath;
        public string IssuePath
        {
            get => mIssuePath;
            set
            {
                if (mIssuePath != value)
                {
                    mIssuePath = value;
                    OnPropertyChanged();
                }
            }
        }

        public ViewIssueVM()
        {
        }

        public void OnRefreshCommand()
        {
            Logger.Log(MessageType.Info, "Refreshing ViewIssue ViewModel");
        }

        public void OnSelectedIssueChanged(JiraIssue jiraIssue)
        {
            Issue = jiraIssue;
            IssuePath = BuildIssuePath(jiraIssue);
        }

        private string BuildIssuePath(JiraIssue issue)
        {
            var path = new List<string>();
            var current = issue;

            while (current != null)
            {
                if (current.Fields != null)
                {
                    path.Insert(0, current.Key);
                }
                else
                {
                    path.Insert(0, current.Key);
                }

                current = current.Fields?.Parent;
            }

            return string.Join(" / ", path);
        }
    }
}
