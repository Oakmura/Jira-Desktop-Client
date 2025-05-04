using JiraClient.Common;
using JiraClient.Utilities;
using JiraClient.Views;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;
using JiraClient.JiraAPI;

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

        private UserControl mIssueListView;
        public UserControl IssueListView
        {
            get => mIssueListView;
            set
            {
                if (mIssueListView != value)
                {
                    mIssueListView = value;
                    OnPropertyChanged();
                }
            }
        }
        private IssueListVM mIssueListVM;

        private ViewIssueView mViewIssueView;
        private ViewIssueVM mViewIssueVM;

        private CreateIssueView mCreateIssueView;
        private CreateIssueVM mCreateIssueVM;

        private CreateFilterView mCreateFilterView;
        private CreateFilterVM mCreateFilterVM;

        Dictionary<int, JiraIssue> mJiraIssuesByID;
        Dictionary<string, List<int>> mJiraIssuesByJQL;

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
                Logger.Log(MessageType.Error, "MainWindow Not Initialized");

                return;
            }

            DateTime now = DateTime.Now;
            double secondsSinceLastRefresh = (now - mLastRefreshTime).TotalSeconds;

            if (secondsSinceLastRefresh < REFRESH_INTERVAL_SECONDS)
            {
                Logger.Log(MessageType.Info, $"Refresh skipped. Only {secondsSinceLastRefresh:F1}/{REFRESH_INTERVAL_SECONDS:F1} seconds since last refresh.");
                return;
            }

            Logger.Log(MessageType.Info, "Refreshing MainWindow ViewModel");

            mIssueListVM.OnRefreshCommand(mJiraIssuesByID, mJiraIssuesByJQL);
            mCreateIssueVM.OnRefreshCommand();
            mCreateFilterVM.OnRefreshCommand();

            mLastRefreshTime = DateTime.Now;
        }

        public void OnSelectedIssueChanged(JiraIssue jiraIssue)
        {
            mViewIssueVM.OnSelectedIssueChanged(jiraIssue);     
        }

        public void SwitchToIssueDetailView(JiraIssue jiraIssue)
        {
            mViewIssueVM.OnSelectedIssueChanged(jiraIssue);
            CurrentView = mViewIssueView;
        }

        private void OnSwitchViewCommand(string action)
        {
            Logger.Log(MessageType.Info, $"Switching to {action} View");

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
            mJiraIssuesByID = await JiraReadAPI.ReadAllJiraIssues();
            mJiraIssuesByJQL = await JiraReadAPI.ReadAllJiraIssuesByJQL();

            Stopwatch sw = Stopwatch.StartNew();
            foreach (KeyValuePair<int, JiraIssue> issueKeyToIssue in mJiraIssuesByID)
            {
                // if jira issue has parent, add itself as subtask to parent
                if (issueKeyToIssue.Value.Fields.Parent != null)
                {
                    JiraIssue parent = mJiraIssuesByID[issueKeyToIssue.Value.Fields.Parent.ID];
                    issueKeyToIssue.Value.Fields.Parent = parent;
   
                    if (parent.Fields.SubTasks == null)
                    {
                        parent.Fields.SubTasks = new List<JiraIssue>(128);
                    }

                    // 이미 subtask에 추가된 경우 overwrite
                    bool bFound = false;
                    for (int i = 0; i < parent.Fields.SubTasks.Count; ++i)
                    {
                        if (parent.Fields.SubTasks[i].ID == issueKeyToIssue.Value.ID)
                        {
                            parent.Fields.SubTasks[i] = issueKeyToIssue.Value;
                            bFound = true;
                            break;
                        }
                    }

                    if (!bFound)
                    {
                        parent.Fields.SubTasks.Add(issueKeyToIssue.Value);
                    }
                }

                // if jira issue has subtasks, add them to parent
                if (issueKeyToIssue.Value.Fields.SubTasks != null)
                {
                    foreach (JiraIssue subtask in issueKeyToIssue.Value.Fields.SubTasks)
                    {
                        subtask.Fields.Parent = issueKeyToIssue.Value;
                    }
                }
            }
            sw.Stop();
            Logger.Log(MessageType.Info, $"Parsing took {sw.Elapsed} seconds");

            mIssueListVM.Setup(mJiraIssuesByID, mJiraIssuesByJQL);

            mbInitialized = true;
        }
    }
}
