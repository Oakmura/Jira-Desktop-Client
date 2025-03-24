using System.Collections.ObjectModel;
using JiraClient.Common;
using JiraClient.Utilities;

namespace JiraClient.ViewModels
{
    public class CreateIssueVM : ViewModelBase
    {
        private string mTitle;
        public string Title
        {
            get => mTitle;
            set
            {
                if (mTitle != value)
                {
                    mTitle = value;
                    OnPropertyChanged();
                }
            }
        }

        ObservableCollection<string> Projects { get; set; } = new ObservableCollection<string>();

        public CreateIssueVM()
        {
            Title = "Create Issue";
        }

        public void OnRefreshCommand()
        {
            _ = Logger.Log(MessageType.Info, "Refreshing CreateIssue ViewModel");
        }
    }
}
