using System.Windows;
using System.Windows.Controls;
using JiraClient.Models;
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

        private void DeleteIssueButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem &&
                menuItem.Parent is ContextMenu contextMenu &&
                contextMenu.PlacementTarget is FrameworkElement fe &&
                fe.DataContext is JiraIssue selectedIssue)
            {
                Logger.Log(MessageType.Warning, $"Delete issue: {selectedIssue.Key} (Not implemented yet)");
            }
        }
    }
}
