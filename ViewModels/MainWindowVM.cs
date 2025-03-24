using JiraClient.Common;
using JiraClient.Utilities;
using JiraClient.Views;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;
using JiraClient.Models;

namespace JiraClient.ViewModels
{
    public class MainWindowVM : ViewModelBase
    {
        public ICommand RefreshCommand { get; }
        public ICommand SwitchViewCommand { get; }

        private UserControl mCurrentView;
        public UserControl CurrentView
        {
            get => mCurrentView;
            set
            {
                if (mCurrentView != value)
                {
                    mCurrentView = value;
                    OnPropertyChanged();
                }
            }
        }

        private IssueListView mIssueListView;
        private IssueListVM mIssueListVM;
        private ViewIssueView mViewIssueView;
        private ViewIssueVM mViewIssueVM;
        private CreateIssueView mCreateIssueView;
        private CreateIssueVM mCreateIssueVM;
        private CreateFilterView mCreateFilterView;
        private CreateFilterVM mCreateFilterVM;

        public List<JiraIssueResponse> JiraIssueResponses { get; private set; } = new List<JiraIssueResponse>();

        bool mbInitialized = false;
        private DateTime mLastRefreshTime = DateTime.MinValue;
        private static readonly double REFRESH_INTERVAL_SECONDS = 10;

        public MainWindowVM()
        {
            _ = initialize();

            RefreshCommand = new RelayCommand(OnRefreshCommand);
            SwitchViewCommand = new RelayCommand<string>(OnSwitchViewCommand);

            mIssueListView = new IssueListView();
            mIssueListVM = new IssueListVM();
            mIssueListView.DataContext = mIssueListVM;

            mViewIssueView = new ViewIssueView();
            mViewIssueVM = new ViewIssueVM();
            mViewIssueView.DataContext = mViewIssueVM;

            mCreateFilterView = new CreateFilterView();
            mCreateFilterVM = new CreateFilterVM();
            mCreateFilterView.DataContext = mCreateFilterVM;

            mCreateIssueView = new CreateIssueView();
            mCreateIssueVM = new CreateIssueVM();
            mCreateIssueView.DataContext = mCreateIssueVM;

            CurrentView = mViewIssueView;
        }

        public void OnRefreshCommand()
        {
            if (!mbInitialized)
            {
                _ = Logger.Log(MessageType.Error, "MainWindow Not Initialized");

                return;
            }

            DateTime now = DateTime.Now;
            double secondsSinceLastRefresh = (now - mLastRefreshTime).TotalSeconds;

            if (secondsSinceLastRefresh < REFRESH_INTERVAL_SECONDS)
            {
                _ = Logger.Log(MessageType.Info, $"Refresh skipped. Only {secondsSinceLastRefresh:F1} seconds since last refresh.");
                return;
            }

            _ = Logger.Log(MessageType.Info, "Refreshing MainWindow ViewModel");

            mIssueListVM.OnRefreshCommand();
            mViewIssueVM.OnRefreshCommand();
            mCreateIssueVM.OnRefreshCommand();
            mCreateFilterVM.OnRefreshCommand();

            mLastRefreshTime = DateTime.Now;
        }

        private void OnSwitchViewCommand(string action)
        {
            _ = Logger.Log(MessageType.Info, $"Switching to {action} View");

            switch (action)
            {
                case "View Issue":
                    CurrentView = mViewIssueView;
                    break;
                case "Create Issue":
                    CurrentView = mCreateIssueView;
                    break;
                case "Create Filter":
                    CurrentView = mCreateFilterView;
                    break;
                default:
                    Debug.Assert(false);
                    break;
            }
        }

        private async Task initialize()
        {
            JiraIssueResponses = await JiraAPI.fetchAllJiraIssues();

            mbInitialized = true;
        }
    }
}
