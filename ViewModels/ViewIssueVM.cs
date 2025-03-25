using JiraClient.Common;
using JiraClient.Models;
using JiraClient.Utilities;

namespace JiraClient.ViewModels
{
    public class ViewIssueVM : ViewModelBase
    {
        private string mDescription;
        public string Description
        {
            get => mDescription;
            set
            {
                if (mDescription != value)
                {
                    mDescription = value;
                    OnPropertyChanged();
                }
            }
        }
        public ViewIssueVM() 
        { 
        }

        public void OnRefreshCommand()
        {
            _ = Logger.Log(MessageType.Info, "Refreshing ViewIssue ViewModel");
        }

        public void OnSelectedIssueChanged(JiraIssue jiraIssue)
        {
            Description = jiraIssue.Fields.Description;
        }
    }
}
