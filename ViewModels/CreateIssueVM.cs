using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using JiraClient.Common;
using JiraClient.JiraAPI;
using JiraClient.Utilities;

namespace JiraClient.ViewModels
{
    public class CreateIssueVM : ViewModelBase
    {
        private Dictionary<string, List<IssueType>> mJiraIssueTypeByProject;

        public ObservableCollection<Project> Projects { get; } = new ObservableCollection<Project>();
        private Project mSelectedProject;
        public Project SelectedProject
        {
            get => mSelectedProject;
            set
            {
                if (mSelectedProject != value)
                {
                    mSelectedProject = value;
                    OnPropertyChanged();

                    updateIssueTypes();
                    _ = updateAssignableUsers();
                }
            }
        }

        public ObservableCollection<IssueType> IssueTypes { get; } = new ObservableCollection<IssueType>();
        private IssueType mSelectedIssueType;
        public IssueType SelectedIssueType
        {
            get => mSelectedIssueType;
            set
            {
                if (mSelectedIssueType != value)
                {
                    mSelectedIssueType = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<string> Labels { get; } = new ObservableCollection<string>();
        private string mSelectedLabel;
        public string SelectedLabel
        {
            get => mSelectedLabel;
            set
            {
                if (mSelectedLabel != value)
                {
                    mSelectedLabel = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<User> Assignees { get; } = new ObservableCollection<User>();
        private User mSelectedAssignee;
        public User SelectedAssignee
        {
            get => mSelectedAssignee;
            set
            {
                if (mSelectedAssignee != value)
                {
                    mSelectedAssignee = value;
                    OnPropertyChanged();
                }
            }
        }

        private string mSummary;
        public string Summary
        {
            get => mSummary;
            set
            {
                if (mSummary != value)
                {
                    mSummary = value;
                    OnPropertyChanged();
                }
            }
        }

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

        private string mOriginalEstimate;
        public string OriginalEstimate
        {
            get => mOriginalEstimate;
            set
            {
                if (mOriginalEstimate != value)
                {
                    mOriginalEstimate = value;
                    OnPropertyChanged();
                }
            }
        }

        private string mRemainingEstimate;
        public string RemainingEstimate
        {
            get => mRemainingEstimate;
            set
            {
                if (mRemainingEstimate != value)
                {
                    mRemainingEstimate = value;
                    OnPropertyChanged();
                }
            }
        }

        private DateTime mStartDate;
        public DateTime StartDate
        {
            get => mStartDate;
            set
            {
                if (mStartDate != value)
                {
                    mStartDate = value;
                    OnPropertyChanged();
                }
            }
        }

        private DateTime? mDueDate;
        public DateTime? DueDate
        {
            get => mDueDate;
            set
            {
                if (mDueDate != value)
                {
                    mDueDate = value;
                    OnPropertyChanged();
                }
            }
        }

        private string mParentKey;
        public string ParentKey
        {
            get => mParentKey;
            set
            {
                if (mParentKey != value)
                {
                    mParentKey = value;
                    OnPropertyChanged();
                }
            }
        }

        private string mWorklogTimeSpent;
        public string WorklogTimeSpent
        {
            get => mWorklogTimeSpent;
            set
            {
                if (mWorklogTimeSpent != value)
                {
                    mWorklogTimeSpent = value;
                    OnPropertyChanged();
                }
            }
        }

        private string mWorklogStarted;
        public string WorklogStarted
        {
            get => mWorklogStarted;
            set
            {
                if (mWorklogStarted != value)
                {
                    mWorklogStarted = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand SubmitIssueCommand { get; }

        public CreateIssueVM()
        {
            SubmitIssueCommand = new RelayCommand(async () => await OnSubmitIssueAsync());

            _ = LoadDataAsync();
        }

        public void OnRefreshCommand()
        {
            Logger.Log(MessageType.Info, "Refreshing CreateIssue ViewModel");
        }

        public async void ReadJiraIssueTypes(List<string> uniqueProjectKeys)
        {
            mJiraIssueTypeByProject = await JiraReadAPI.ReadIssueTypesAsync(uniqueProjectKeys);

            updateIssueTypes();
        }

        private async Task LoadDataAsync()
        {
            var projects = await JiraReadAPI.ReadProjectsAsync();
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Projects.Clear();
                foreach (var p in projects)
                {
                    Projects.Add(p);
                }
            });

            var labels = await JiraReadAPI.ReadLabelsAsync();
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Labels.Clear();
                foreach (var l in labels)
                {
                    Labels.Add(l);
                }
            });

            await updateAssignableUsers();

            Logger.Log(MessageType.Info, "CreateIssueVM 초기화 완료");
        }

        private async Task OnSubmitIssueAsync()
        {
            if (SelectedProject == null)
            {
                Logger.Log(MessageType.Warning, "이슈 생성 실패: Project가 선택되지 않았습니다.");
                return;
            }

            if (SelectedIssueType == null)
            {
                Logger.Log(MessageType.Warning, "이슈 생성 실패: Issue Type이 선택되지 않았습니다.");
                return;
            }

            if (string.IsNullOrWhiteSpace(Summary))
            {
                Logger.Log(MessageType.Warning, "이슈 생성 실패: Summary가 입력되지 않았습니다.");
                return;
            }

            try
            {
                CreateJiraIssueRequest issue = new CreateJiraIssueRequest
                {
                    Fields = new CreateJiraIssueFields
                    {
                        Project = new Project { Key = SelectedProject.Key },
                        IssueType = new IssueType { ID = SelectedIssueType.ID },
                        Summary = Summary,
                        Description = Description,
                        Labels = !string.IsNullOrWhiteSpace(SelectedLabel) ? new List<string> { SelectedLabel } : null,
                        Assignee = SelectedAssignee != null ? new User { AccountID = SelectedAssignee.AccountID } : null,
                        DueDate = DueDate?.ToString("yyyy-MM-dd"),
                        Timetracking = (!string.IsNullOrWhiteSpace(OriginalEstimate) || !string.IsNullOrWhiteSpace(RemainingEstimate))
                            ? new JiraTimeTracking
                            {
                                OriginalEstimate = OriginalEstimate,
                                RemainingEstimate = RemainingEstimate
                            }
                            : null,
                        Parent = !string.IsNullOrWhiteSpace(ParentKey) ? new JiraIssue { Key = ParentKey } : null
                    },

                    Update = (!string.IsNullOrWhiteSpace(WorklogTimeSpent) && !string.IsNullOrWhiteSpace(WorklogStarted))
                        ? new CreateJiraIssueUpdate
                        {
                            Worklog = new List<JiraWorklogAdd>
                            {
                                new JiraWorklogAdd
                                {
                                    Add = new JiraWorklogEntry
                                    {
                                        TimeSpent = WorklogTimeSpent,
                                        Started = WorklogStarted
                                    }
                                }
                            }
                        }
                        : null
                };

                (bool bSuccess, JiraIssue createdIssue) = await JiraCreateAPI.CreateJiraIssueAsync(issue);
                if (bSuccess)
                {
                    Logger.Log(MessageType.Info, $"이슈 생성 성공: {createdIssue.Key}");
                    resetFields();
                }
                else
                {
                    Logger.Log(MessageType.Error, "이슈 생성 실패");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(MessageType.Error, $"Submit Issue 오류: {ex.Message}");
            }
        }


        private void updateIssueTypes()
        {
            if (mJiraIssueTypeByProject == null)
            {
                Logger.Log(MessageType.Warning, "JiraIssueTypeByProject is null");
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                IssueTypes.Clear();
                if (SelectedProject != null && mJiraIssueTypeByProject.ContainsKey(SelectedProject.Key))
                {
                    foreach (var t in mJiraIssueTypeByProject[SelectedProject.Key])
                    {
                        IssueTypes.Add(t);
                    }
                }
            });
        }

        private async Task updateAssignableUsers()
        {
            if (SelectedProject == null || string.IsNullOrEmpty(SelectedProject.Key))
            {
                return;
            }

            List<User> users = await JiraReadAPI.ReadAssignableUsersAsync(SelectedProject.Key);
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Assignees.Clear();
                foreach (User user in users)
                {
                    Assignees.Add(user);
                }
            });
        }

        private void resetFields()
        {
            SelectedIssueType = null;
            Summary = string.Empty;
            Description = string.Empty;
            SelectedLabel = null;
            SelectedAssignee = null;
            OriginalEstimate = string.Empty;
            RemainingEstimate = string.Empty;
            StartDate = DateTime.Now;
            DueDate = null;
            ParentKey = string.Empty;
            WorklogTimeSpent = string.Empty;
            WorklogStarted = string.Empty;
        }
    }
}
