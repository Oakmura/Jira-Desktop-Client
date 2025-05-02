using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using JiraClient.Common;
using JiraClient.JiraAPI;
using JiraClient.Utilities;

namespace JiraClient.ViewModels
{
    public class CreateIssueVM : ViewModelBase
    {
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

        public ObservableCollection<Priority> Priorities { get; } = new ObservableCollection<Priority>();
        private Priority mSelectedPriority;
        public Priority SelectedPriority
        {
            get => mSelectedPriority;
            set
            {
                if (mSelectedPriority != value)
                {
                    mSelectedPriority = value;
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

        public ICommand SubmitIssueCommand { get; }

        private List<string> SelectedLabels { get; } = new List<string>();

        public CreateIssueVM()
        {
            SubmitIssueCommand = new RelayCommand(async () => await OnSubmitIssueAsync());

            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            var projects = await JiraReadAPI.ReadProjectsAsync();
            Projects.Clear();
            foreach (var p in projects)
                Projects.Add(p);

            var issueTypes = await JiraReadAPI.ReadIssueTypesAsync();
            IssueTypes.Clear();
            foreach (var t in issueTypes)
                IssueTypes.Add(t);

            var labels = await JiraReadAPI.ReadLabelsAsync();
            Labels.Clear();
            foreach (var l in labels)
                Labels.Add(l);

            var priorities = await JiraReadAPI.ReadPrioritiesAsync();
            Priorities.Clear();
            foreach (var p in priorities)
                Priorities.Add(p);

            var users = await JiraReadAPI.ReadAssignableUsersAsync();
            Assignees.Clear();
            foreach (var u in users)
                Assignees.Add(u);

            Logger.Log(MessageType.Info, "CreateIssueVM 초기화 완료");
        }

        private async Task OnSubmitIssueAsync()
        {
            try
            {
                JiraIssue issue = new JiraIssue
                {
                    Fields = new JiraIssueFields
                    {
                        Project = SelectedProject,
                        IssueType = SelectedIssueType,
                        Summary = Summary,
                        Description = Description,
                        Labels = SelectedLabels.Count > 0 ? new List<string>(SelectedLabels) : (SelectedLabel != null ? new List<string> { SelectedLabel } : new List<string>()),
                        Priority = SelectedPriority,
                        Assignee = SelectedAssignee,
                        DueDate = DueDate?.ToString("yyyy-MM-dd") // Jira API는 "yyyy-MM-dd" 형식 요구
                    }
                };

                var (success, createdIssue) = await JiraCreateAPI.CreateJiraIssueAsync(issue);
                if (success)
                {
                    Logger.Log(MessageType.Info, $"이슈 생성 성공: {createdIssue.Key}");
                }
                else
                {
                    Logger.Log(MessageType.Error, $"이슈 생성 실패");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(MessageType.Error, $"Submit Issue 오류: {ex.Message}");
            }
        }

        public void OnRefreshCommand()
        {
            Logger.Log(MessageType.Info, "Refreshing CreateIssue ViewModel");
        }
    }
}
