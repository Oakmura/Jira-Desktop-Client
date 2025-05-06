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
        private Dictionary<string, List<IssueType>> mJiraIssueTypeByProject;

        public ViewIssueVM()
        {
            UpdateIssueCommand = new RelayCommand(async () => await OnUpdateIssueCommand());
            ResetIssueCommand = new RelayCommand(OnResetIssueCommand);
        }

        public async void OnSelectedIssueChanged(JiraIssue jiraIssue)
        {
            mOriginalIssue = jiraIssue;
            IssuePath = buildIssuePath(jiraIssue);

            Project = jiraIssue.Fields.Project;
            Summary = jiraIssue.Fields.Summary;
            Description = jiraIssue.Fields.Description;

            await updateIssueTypes();
            await loadAssignees(jiraIssue.Fields.Project);

            SelectedIssueType = IssueTypes.FirstOrDefault(x => x.ID == jiraIssue.Fields.IssueType.ID);
            SelectedAssignee = Assignees.FirstOrDefault(x => x.AccountID == jiraIssue.Fields.Assignee?.AccountID);
        }

        public async void ReadJiraIssueTypesByProject(List<string> uniqueProjectKeys)
        {
            mJiraIssueTypeByProject = await JiraReadAPI.ReadIssueTypesByProjectAsync(uniqueProjectKeys);

            _ = updateIssueTypes();
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

            Application.Current.Dispatcher.Invoke(() =>
            {
                IssueTypes.Clear();

                // epic 또는 sub-task는 해당 타입만 선택 가능
                if (currentTypeName == "epic" || currentTypeName == "sub-task")
                {
                    IssueType onlyType = allIssueTypes.FirstOrDefault(t => t.Name.Equals(currentTypeName, StringComparison.OrdinalIgnoreCase));
                    if (onlyType != null)
                    {
                        IssueTypes.Add(onlyType);
                    }
                }
                else
                {
                    foreach (IssueType type in allIssueTypes)
                    {
                        string typeName = type.Name.ToLowerInvariant();
                        if (typeName != "epic" && typeName != "sub-task")
                        {
                            IssueTypes.Add(type);
                        }
                    }
                }
            });
        }

        private async Task loadAssignees(Project project)
        {
            List<User> users = await JiraReadAPI.ReadAssignableUsersAsync(project.Key);
            Application.Current.Dispatcher.Invoke(() =>
            {
                Assignees.Clear();
                foreach (User user in users)
                {
                    Assignees.Add(user);
                }
            });
        }

        private void OnResetIssueCommand()
        {
            if (mOriginalIssue != null)
            {
                Summary = mOriginalIssue.Fields.Summary;
                Description = mOriginalIssue.Fields.Description;
                SelectedIssueType = IssueTypes.FirstOrDefault(x => x.ID == mOriginalIssue.Fields.IssueType.ID);
                SelectedAssignee = Assignees.FirstOrDefault(x => x.AccountID == mOriginalIssue.Fields.Assignee?.AccountID);
            }
        }

        private async Task OnUpdateIssueCommand()
        {
            if (mOriginalIssue == null)
                return;

            JiraUpdateAPI.UpdateJiraIssueRequest update = new JiraUpdateAPI.UpdateJiraIssueRequest
            {
                Fields = new JiraUpdateAPI.UpdateJiraIssueFields
                {
                    Summary = Summary,
                    Description = Description,
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
