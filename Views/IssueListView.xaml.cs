using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using JiraClient.Utilities;
using JiraClient.ViewModels;

namespace JiraClient.Views
{
    /// <summary>
    /// Interaction logic for IssueListView.xaml
    /// </summary>
    public partial class IssueListView : UserControl
    {
        public IssueListView()
        {
            InitializeComponent();
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            JiraIssueResponse jiraResponse = e.NewValue as JiraIssueResponse;
            Debug.Assert(jiraResponse != null);

            _ = Logger.Log(MessageType.Info, $"StartAt: {jiraResponse.StartAt}, MaxResults: {jiraResponse.MaxResults}, Total: {jiraResponse.Total}");
        }
    }
}
