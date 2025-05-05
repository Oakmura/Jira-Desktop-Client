using System.Windows;
using System.Windows.Controls;
using JiraClient.JiraAPI;
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

        private void SelectedIssueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            JiraIssue selectedIssue = e.NewValue as JiraIssue;
            if (selectedIssue == null)
            {
                return;
            }

            Logger.Log(MessageType.Info, $"Selected issue changed: {selectedIssue.Key}");

            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
            MainWindowVM mainWIndowVM = mainWindow.DataContext as MainWindowVM;

            mainWIndowVM.OnSelectedIssueChanged(selectedIssue);
        }

        private void SelectedIssueDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.OriginalSource is DependencyObject source)
            {
                TreeViewItem issueTreeViewItem = TreeHelper.FindParent<TreeViewItem>(source);
                if (issueTreeViewItem != null && issueTreeViewItem.DataContext is JiraIssue selectedIssue)
                {
                    Logger.Log(MessageType.Info, $"Selected issue double-clicked: {selectedIssue.Key}");

                    var mainWindow = Application.Current.MainWindow as MainWindow;
                    var mainWindowVM = mainWindow.DataContext as MainWindowVM;

                    mainWindowVM.SwitchToIssueDetailView(selectedIssue);
                }
            }
        }

        private async void DeleteIssueButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem &&
                menuItem.Parent is ContextMenu contextMenu &&
                contextMenu.PlacementTarget is FrameworkElement fe &&
                fe.DataContext is JiraIssue selectedIssue)
            {
                if (selectedIssue.Fields.SubTasks.Count != 0)
                {
                    Logger.Log(MessageType.Error, "Child Issue가 있는 이슈는 제거할 수 없습니다. 먼저 Child Issue 들을 모두 제거해주세요.");
                    return;
                }

                bool bSuccess = await JiraDeleteAPI.DeleteIssueAsync(selectedIssue.Key);

                var mainWindow = Application.Current.MainWindow as MainWindow;
                var mainWindowVM = mainWindow.DataContext as MainWindowVM;

                mainWindowVM.OnIssueDeleted(selectedIssue);
            }
        }
    }
}
