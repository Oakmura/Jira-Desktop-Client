using JiraClient.Common;
using JiraClient.Utilities;
using JiraClient.Views;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;
using JiraClient.JiraAPI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace JiraClient.ViewModels
{
    public class MainWindowVM : ViewModelBase
    {
        public ICommand RefreshCommand { get; }
        public ICommand SwitchViewCommand { get; }
        public ICommand OpenIssueFinderCommand { get; }

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

        private IssueListVM mIssueListVM;
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

        private ViewIssueVM mViewIssueVM;
        private ViewIssueView mViewIssueView;
        private CreateIssueVM mCreateIssueVM;
        private CreateIssueView mCreateIssueView;
        private CreateFilterVM mCreateFilterVM;
        private CreateFilterView mCreateFilterView;

        // TODO: should keep this to private
        public Dictionary<string, JiraIssue> mJiraIssuesByID { get; set; }
        private Dictionary<string, List<string>> mJiraIssuesByJQL;
        private Dictionary<string, List<User>> mAssignableUsersByProject;

        public List<string> UniqueProjectKeys { get; private set; }

        bool mbInitialized = false;
        private DateTime mLastRefreshTime = DateTime.MinValue;
        private static readonly double REFRESH_INTERVAL_IN_SECONDS = 10;

        public MainWindowVM()
        {
            _ = initialize();

            RefreshCommand = new RelayCommand(OnRefreshCommand);
            SwitchViewCommand = new RelayCommand<string>(OnSwitchViewCommand);
            OpenIssueFinderCommand = new RelayCommand(OnOpenIssueFinderCommand);

            mIssueListVM = new IssueListVM();
            mIssueListView = new IssueListView();
            mIssueListView.DataContext = mIssueListVM;

            mViewIssueVM = new ViewIssueVM();
            mViewIssueView = new ViewIssueView();
            mViewIssueView.DataContext = mViewIssueVM;

            mCreateFilterVM = new CreateFilterVM();
            mCreateFilterView = new CreateFilterView();
            mCreateFilterView.DataContext = mCreateFilterVM;

            mCreateIssueVM = new CreateIssueVM();
            mCreateIssueView = new CreateIssueView();
            mCreateIssueView.DataContext = mCreateIssueVM;

            CurrentView = mCreateIssueView;
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

            if (secondsSinceLastRefresh < REFRESH_INTERVAL_IN_SECONDS)
            {
                Logger.Log(MessageType.Info, $"Refresh skipped. Only {secondsSinceLastRefresh:F1}/{REFRESH_INTERVAL_IN_SECONDS:F1} seconds since last refresh.");
                return;
            }

            Logger.Log(MessageType.Info, "Refreshing MainWindow ViewModel");

            _ = initialize();

            mLastRefreshTime = DateTime.Now;
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

        private void OnOpenIssueFinderCommand()
        {
            if (!mbInitialized)
            {
                Logger.Log(MessageType.Warning, "MainWindow Not Initialized");
                return;
            }

            IssueFinderView finderWindow = new IssueFinderView(mJiraIssuesByID);
            finderWindow.Owner = Application.Current.MainWindow;
            if (finderWindow.ShowDialog() == true)
            {
                JiraIssue selected = finderWindow.SelectedIssue;
                if (selected != null)
                {
                    SwitchToIssueDetailView(selected);
                }
            }
        }

        public void OnSelectedIssueChanged(JiraIssue jiraIssue)
        {
            mViewIssueVM.OnSelectedIssueChanged(jiraIssue);
        }

        public async void OnNewIssueCreated(string jiraIssueKey)
        {
            JiraIssue jiraIssue = await JiraReadAPI.ReadSingleJiraIssueOrNull(jiraIssueKey);
            Debug.Assert(jiraIssue != null, "JiraIssue is null");

            mJiraIssuesByID[jiraIssue.ID] = jiraIssue;
            _ = RefreshCreate(jiraIssue);
        }

        public async void OnIssueUpdated(string jiraIssueKey)
        {
            JiraIssue jiraIssue = await JiraReadAPI.ReadSingleJiraIssueOrNull(jiraIssueKey);
            Debug.Assert(jiraIssue != null, "JiraIssue is null");

            JiraIssue originalJiraIssue = mJiraIssuesByID[jiraIssue.ID];
            originalJiraIssue.Fields.IssueType = jiraIssue.Fields.IssueType;
            originalJiraIssue.Fields.Summary = jiraIssue.Fields.Summary;
            originalJiraIssue.Fields.Description = jiraIssue.Fields.Description;
            originalJiraIssue.Fields.Assignee = jiraIssue.Fields.Assignee;
            originalJiraIssue.Fields.DueDate = jiraIssue.Fields.DueDate;

            _ = RefreshUpdate(originalJiraIssue);
        }

        public void OnIssueDeleted(JiraIssue jiraIssue)
        {
            mJiraIssuesByID.Remove(jiraIssue.ID);
            _ = refreshDelete(jiraIssue);
        }

        public async void OnNewFilterCreated(FilterVM newFilter)
        {
            List<string> filteredJiraIssues = await JiraReadAPI.ReadAllJiraIssuesByJQL(newFilter.FilterJQL);
            mJiraIssuesByJQL[newFilter.FilterName] = filteredJiraIssues;
            mIssueListVM.OnNewFilterCreated(newFilter, filteredJiraIssues, mJiraIssuesByID);
        }

        public void OnFilterDeleted(FilterVM filterToDelete)
        {
            if (mJiraIssuesByJQL.ContainsKey(filterToDelete.FilterName))
            {
                mJiraIssuesByJQL.Remove(filterToDelete.FilterName);
                mIssueListVM.OnFilterDeleted(filterToDelete);
            }
        }

        public async void OnFilterUpdated(string previousFilterName, FilterVM filterToUpdate)
        {
            if (mJiraIssuesByJQL.ContainsKey(previousFilterName))
            {
                mJiraIssuesByJQL.Remove(previousFilterName);
                List<string> filteredJiraIssues = await JiraReadAPI.ReadAllJiraIssuesByJQL(filterToUpdate.FilterJQL);
                mJiraIssuesByJQL[filterToUpdate.FilterName] = filteredJiraIssues;
                mIssueListVM.OnFilterUpdated(previousFilterName, filterToUpdate, filteredJiraIssues, mJiraIssuesByID);
            }
        }

        public void OnFilterOrderChanged(int oldIndex, int newIndex)
        {
            mIssueListVM.OnFilterOrderChanged(oldIndex, newIndex);
            mCreateFilterVM.SaveCurrentFilterOrderToDisk();
        }

        public void SwitchToIssueDetailView(JiraIssue jiraIssue)
        {
            mViewIssueVM.OnSelectedIssueChanged(jiraIssue);
            CurrentView = mViewIssueView;
        }

        private async Task initialize()
        {
            mJiraIssuesByID = await JiraReadAPI.ReadAllJiraIssues();
            mJiraIssuesByJQL = await JiraReadAPI.ReadAllJiraIssuesByJQL();
            UniqueProjectKeys = mJiraIssuesByID.Select(pair => pair.Value.Fields.Project.Key).Distinct().ToList();

            mCreateIssueVM.ReadJiraIssueTypesByProject(UniqueProjectKeys);
            mViewIssueVM.ReadJiraIssueTypesByProject(UniqueProjectKeys);

            Stopwatch assigneeLoadWatch = Stopwatch.StartNew();
            mAssignableUsersByProject = await JiraReadAPI.ReadAssignableUsersAsync(UniqueProjectKeys);
            mViewIssueVM.SetAssignableUsersByProject(mAssignableUsersByProject);
            assigneeLoadWatch.Stop();
            Logger.Log(MessageType.Info, $"Assignable user loading took {assigneeLoadWatch.Elapsed} seconds");

            Stopwatch sw = Stopwatch.StartNew();
            foreach (KeyValuePair<string, JiraIssue> issueKeyToIssue in mJiraIssuesByID)
            {
                // if jira issue has parent, add itself as subtask to parent
                if (issueKeyToIssue.Value.Fields.Parent != null)
                {
                    JiraIssue parent = mJiraIssuesByID[issueKeyToIssue.Value.Fields.Parent.ID];
                    issueKeyToIssue.Value.Fields.Parent = parent;
   
                    if (parent.Fields.SubTasks == null)
                    {
                        parent.Fields.SubTasks = new ObservableCollection<JiraIssue>();
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

        private async Task RefreshCreate(JiraIssue jiraIssue)
        {
            mJiraIssuesByJQL = await JiraReadAPI.ReadAllJiraIssuesByJQL();

            Stopwatch sw = Stopwatch.StartNew();

            // if jira issue has parent, add itself as subtask to parent
            if (jiraIssue.Fields.Parent != null)
            {
                JiraIssue parent = mJiraIssuesByID[jiraIssue.Fields.Parent.ID];
                jiraIssue.Fields.Parent = parent;

                if (parent.Fields.SubTasks == null)
                {
                    parent.Fields.SubTasks = new ObservableCollection<JiraIssue>();
                }

                // 이미 subtask에 추가된 경우 overwrite
                bool bFound = false;
                for (int i = 0; i < parent.Fields.SubTasks.Count; ++i)
                {
                    if (parent.Fields.SubTasks[i].ID == jiraIssue.ID)
                    {
                        parent.Fields.SubTasks[i] = jiraIssue;
                        bFound = true;
                        break;
                    }
                }

                if (!bFound)
                {
                    parent.Fields.SubTasks.Add(jiraIssue);
                }
            }

            // if jira issue has subtasks, add them to parent
            if (jiraIssue.Fields.SubTasks != null)
            {
                foreach (JiraIssue subtask in jiraIssue.Fields.SubTasks)
                {
                    subtask.Fields.Parent = jiraIssue;
                }
            }
            sw.Stop();
            Logger.Log(MessageType.Info, $"Parsing took {sw.Elapsed} seconds");

            mIssueListVM.OnNewIssueCreated(jiraIssue, mJiraIssuesByID, mJiraIssuesByJQL);
        }

        private async Task refreshDelete(JiraIssue jiraIssue)
        {
            mJiraIssuesByJQL = await JiraReadAPI.ReadAllJiraIssuesByJQL();

            Stopwatch sw = Stopwatch.StartNew();
            if (jiraIssue.Fields.Parent != null)
            {
                JiraIssue parent = mJiraIssuesByID[jiraIssue.Fields.Parent.ID];
                parent.Fields.SubTasks.Remove(jiraIssue);
            }
            sw.Stop();
            Logger.Log(MessageType.Info, $"Refresh Delete took {sw.Elapsed} seconds");

            mIssueListVM.OnIssueDeleted(jiraIssue, mJiraIssuesByID, mJiraIssuesByJQL);
        }

        private async Task RefreshUpdate(JiraIssue jiraIssue)
        {
            mJiraIssuesByJQL = await JiraReadAPI.ReadAllJiraIssuesByJQL();

            mIssueListVM.OnNewIssueUpdated(jiraIssue, mJiraIssuesByID, mJiraIssuesByJQL);
        }
    }
}
