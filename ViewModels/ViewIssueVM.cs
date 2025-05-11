using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using JiraClient.Common;
using JiraClient.JiraAPI;
using JiraClient.Utilities;

namespace JiraClient.ViewModels
{
    public class ViewIssueVM : ViewModelBase
    {
        public ICommand UpdateIssueCommand { get; }
        public ICommand ResetIssueCommand { get; }

        public JiraIssue OriginalIssue => mOriginalIssue;

        private JiraIssue mOriginalIssue;

        // Editable fields
        public string IssueKey => mOriginalIssue?.Key;

        private string mIssuePath;
        public string IssuePath
        {
            get => mIssuePath;
            set { mIssuePath = value; OnPropertyChanged(); }
        }

        private Project mProject;
        public Project Project
        {
            get => mProject;
            set { mProject = value; OnPropertyChanged(); }
        }

        private string mSummary;
        public string Summary
        {
            get => mSummary;
            set { mSummary = value; OnPropertyChanged(); }
        }

        private string mDescription;
        public string Description
        {
            get => mDescription;
            set { mDescription = value; OnPropertyChanged(); }
        }

        private DateTime? mDueDate;
        public DateTime? DueDate
        {
            get => mDueDate;
            set { mDueDate = value; OnPropertyChanged(); }
        }

        private IssueType mSelectedIssueType;
        public IssueType SelectedIssueType
        {
            get => mSelectedIssueType;
            set { mSelectedIssueType = value; OnPropertyChanged(); }
        }

        private User mSelectedAssignee;
        public User SelectedAssignee
        {
            get => mSelectedAssignee;
            set { mSelectedAssignee = value; OnPropertyChanged(); }
        }

        public ObservableCollection<IssueType> IssueTypes { get; } = new();
        public ObservableCollection<User> Assignees { get; } = new();
        public ObservableCollection<Attachment> Attachments { get; } = new();
        public ObservableCollection<Comment> Comments { get; } = new();

        private Dictionary<string, List<IssueType>> mJiraIssueTypeByProject;
        private Dictionary<string, List<User>> mAssignableUsersByProject;

        public ViewIssueVM()
        {
            UpdateIssueCommand = new RelayCommand(async () => await OnUpdateIssueCommand());
            ResetIssueCommand = new RelayCommand(OnResetIssueCommand);
        }

        public async void OnSelectedIssueChanged(JiraIssue jiraIssue)
        {
            mOriginalIssue = jiraIssue;
            OnPropertyChanged(nameof(OriginalIssue));
            IssuePath = buildIssuePath(jiraIssue);

            Project = jiraIssue.Fields.Project;
            Summary = jiraIssue.Fields.Summary;
            Description = jiraIssue.Fields.Description;
            DueDate = string.IsNullOrWhiteSpace(jiraIssue.Fields.DueDate) ? null : DateTime.Parse(jiraIssue.Fields.DueDate);

            Attachments.Clear();
            foreach (var att in jiraIssue.Fields.Attachment ?? Enumerable.Empty<Attachment>())
            {
                Attachments.Add(att);
            }

            Comments.Clear();
            foreach (var cmt in jiraIssue.Fields.Comment?.Comments ?? Enumerable.Empty<Comment>())
            {
                Comments.Add(cmt);
            }

            await updateIssueTypes();
            await loadAssignees(jiraIssue.Fields.Project);

            SelectedAssignee = Assignees.FirstOrDefault(x => x.AccountID == jiraIssue.Fields.Assignee?.AccountID);
        }

        public async void ReadJiraIssueTypesByProject(List<string> uniqueProjectKeys)
        {
            mJiraIssueTypeByProject = await JiraReadAPI.ReadIssueTypesByProjectAsync(uniqueProjectKeys);

            _ = updateIssueTypes();
        }

        public void SetAssignableUsersByProject(Dictionary<string, List<User>> assignableUsersByProject)
        {
            mAssignableUsersByProject = assignableUsersByProject;
        }

        private async Task updateIssueTypes()
        {
            if (mJiraIssueTypeByProject == null || mProject == null)
            {
                Logger.Log(MessageType.Error, "issue type is not initialized");
                return;
            }

            List<IssueType> allIssueTypes = mJiraIssueTypeByProject[mProject.Key];
            string currentTypeName = mOriginalIssue?.Fields.IssueType.Name.ToLowerInvariant();
            Debug.Assert(!string.IsNullOrWhiteSpace(currentTypeName), "Current issue type must be defined");

            // 1. 현재 타입 우선 추가 + 바로 선택
            IssueType currentType = allIssueTypes.FirstOrDefault(t => t.Name.Equals(currentTypeName, StringComparison.OrdinalIgnoreCase));
            if (currentType != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    IssueTypes.Clear();                          // 완전히 비우고
                    IssueTypes.Add(currentType);                 // 현재 타입만 먼저 추가
                    SelectedIssueType = currentType;             // 즉시 선택되도록 설정
                });
            }

            // 2. 나머지 타입 비동기 추가
            await Task.Run(() =>
            {
                List<IssueType> toAdd = new();

                if (currentTypeName != "epic" && currentTypeName != "sub-task")
                {
                    toAdd = allIssueTypes
                        .Where(t =>
                            !t.Name.Equals("epic", StringComparison.OrdinalIgnoreCase) &&
                            !t.Name.Equals("sub-task", StringComparison.OrdinalIgnoreCase) &&
                            !t.Name.Equals(currentTypeName, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                if (toAdd.Count > 0)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (IssueType type in toAdd)
                        {
                            // ID 대소문자 구분 없이 비교
                            if (!IssueTypes.Any(t => t.ID == type.ID))
                            {
                                IssueTypes.Add(type);
                            }
                        }
                    });
                }
            });
        }

        private Task loadAssignees(Project project)
        {
            return Task.Run(() =>
            {
                if (mOriginalIssue?.Fields.Assignee == null)
                {
                    return;
                }

                User currentAssignee = mOriginalIssue.Fields.Assignee;
                List<User> allUsers;

                if (!mAssignableUsersByProject.TryGetValue(project.Key, out allUsers) || allUsers == null || allUsers.Count == 0)
                {
                    // fallback: currentAssignee만 보여주기
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Assignees.Clear();
                        Assignees.Add(currentAssignee);
                        SelectedAssignee = currentAssignee;
                    });
                    return;
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Assignees.Clear();

                    // currentAssignee 먼저 추가 (중복 방지)
                    if (!allUsers.Any(u => u.AccountID == currentAssignee.AccountID))
                    {
                        Assignees.Add(currentAssignee);
                    }

                    foreach (User user in allUsers)
                    {
                        if (!Assignees.Any(u => u.AccountID == user.AccountID))
                        {
                            Assignees.Add(user);
                        }
                    }

                    SelectedAssignee = Assignees.FirstOrDefault(x => x.AccountID == currentAssignee.AccountID);
                });
            });
        }

        private void OnResetIssueCommand()
        {
            if (mOriginalIssue != null)
            {
                Summary = mOriginalIssue.Fields.Summary;
                Description = mOriginalIssue.Fields.Description;
                DueDate = string.IsNullOrWhiteSpace(mOriginalIssue.Fields.DueDate) ? null : DateTime.Parse(mOriginalIssue.Fields.DueDate);
                SelectedIssueType = IssueTypes.FirstOrDefault(x => x.ID == mOriginalIssue.Fields.IssueType.ID);
                SelectedAssignee = Assignees.FirstOrDefault(x => x.AccountID == mOriginalIssue.Fields.Assignee?.AccountID);
            }
        }

        private async Task OnUpdateIssueCommand()
        {
            if (mOriginalIssue == null)
            {
                return;
            }

            JiraUpdateAPI.UpdateJiraIssueRequest update = new JiraUpdateAPI.UpdateJiraIssueRequest
            {
                Fields = new JiraUpdateAPI.UpdateJiraIssueFields
                {
                    Summary = Summary,
                    Description = Description,
                    DueDate = DueDate?.ToString("yyyy-MM-dd"),
                    IssueType = SelectedIssueType != null ? new IssueType { ID = SelectedIssueType.ID } : null,
                    Assignee = SelectedAssignee != null ? new User { AccountID = SelectedAssignee.AccountID } : null
                }
            };

            bool result = await JiraUpdateAPI.UpdateIssueAsync(mOriginalIssue.Key, update);
            Logger.Log(result ? MessageType.Info : MessageType.Error, result ? "이슈 업데이트 성공" : "이슈 업데이트 실패");
        }

        private string buildIssuePath(JiraIssue issue)
        {
            List<string> path = new();
            JiraIssue current = issue;
            while (current != null)
            {
                path.Insert(0, current.Key);
                current = current.Fields?.Parent;
            }
            return string.Join(" / ", path);
        }
    }
}
