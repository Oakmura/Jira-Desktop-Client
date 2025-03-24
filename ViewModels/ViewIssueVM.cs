using JiraClient.Common;
using JiraClient.Utilities;

namespace JiraClient.ViewModels
{
    public class ViewIssueVM : ViewModelBase
    {
        public string Text { get; set; } = "View Issue VM Test";
        public ViewIssueVM() 
        { 
        }

        public void OnRefreshCommand()
        {
            _ = Logger.Log(MessageType.Info, "Refreshing ViewIssue ViewModel");
        }
    }
}
