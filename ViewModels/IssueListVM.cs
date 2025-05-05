using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using JiraClient.Common;
using JiraClient.JiraAPI;
using JiraClient.Utilities;

namespace JiraClient.ViewModels
{
    // TODO: should change to ViewModelBase if want to dynamically update the list
    public class HierarchicalIssueList
    {
        public string Name { get; set; }
        public ObservableCollection<JiraIssue> Children { get; set; } = new ObservableCollection<JiraIssue>();
    }

    public class IssueListVM : ViewModelBase
    {
        public ObservableCollection<HierarchicalIssueList> HierarchicalIssueList { get; private set; } = new ObservableCollection<HierarchicalIssueList>();

        public IssueListVM()
        {
        }

        public void Setup(Dictionary<string, JiraIssue> jiraIssuesByID, Dictionary<string, List<string>> jiraIssuesByJQL)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                HierarchicalIssueList.Clear();

                foreach (KeyValuePair<string, List<string>> issuesByJQL in jiraIssuesByJQL)
                {
                    ObservableCollection<JiraIssue> childIssues = new ObservableCollection<JiraIssue>();

                    foreach (string id in issuesByJQL.Value)
                    {
                        if (jiraIssuesByID.TryGetValue(id, out JiraIssue issue))
                        {
                            bool isSubtaskOfAnotherIssue = jiraIssuesByID.Values.Any(parent =>
                                parent.Fields.SubTasks != null &&
                                parent.Fields.SubTasks.Any(sub => sub.ID == id));

                            if (isSubtaskOfAnotherIssue)
                            {
#if EXTRA_LOG_MODE
                                Logger.Log(MessageType.Info, $"[Setup] '{issue.Key}' is subtask of another issue → excluded from filter group '{issuesByJQL.Key}'");
#endif
                                continue;
                            }

                            childIssues.Add(issue);
                        }
                        else
                        {
                            Debug.Assert(false, $"[Setup] 누락된 이슈 ID: {id} (JQL 그룹: '{issuesByJQL.Key}')");
                        }
                    }

                    HierarchicalIssueList.Add(new HierarchicalIssueList
                    {
                        Name = issuesByJQL.Key,
                        Children = childIssues
                    });
                }
            });

            Logger.Log(MessageType.Info, $"IssueListVM: {HierarchicalIssueList.Count} JQLs with {HierarchicalIssueList.Sum(v => v.Children.Count)} issues");
        }

        public void OnRefreshCommand(Dictionary<string, JiraIssue> jiraIssuesByID, Dictionary<string, List<string>> jiraIssuesByJQL)
        {
            Logger.Log(MessageType.Info, "Refreshing IssueList ViewModel");

            Setup(jiraIssuesByID, jiraIssuesByJQL);
        }

        public void OnNewIssueCreated(JiraIssue createdJiraIssue, Dictionary<string, JiraIssue> jiraIssuesByID, Dictionary<string, List<string>> jiraIssuesByJQL)
        {
            string createdID = createdJiraIssue.ID;
            string createdKey = createdJiraIssue.Key;

            Application.Current.Dispatcher.Invoke(() =>
            {
                // 모든 부모 이슈의 SubTask 목록에서 이슈 ID가 포함되어 있는지 검사
                bool isSubtaskOfAnotherIssue = jiraIssuesByID.Values.Any(parent =>
                    parent.Fields.SubTasks != null &&
                    parent.Fields.SubTasks.Any(sub => sub.ID == createdID));

                if (isSubtaskOfAnotherIssue)
                {
#if EXTRA_LOG_MODE
                    Logger.Log(MessageType.Info, $"[OnNewIssueCreated] '{createdKey}' is a subtask of another issue → excluded from all filter groups");
#endif
                    return;
                }

                foreach (KeyValuePair<string, List<string>> entry in jiraIssuesByJQL)
                {
                    string jqlName = entry.Key;
                    List<string> issueIDs = entry.Value;

                    if (issueIDs.Contains(createdID))
                    {
                        HierarchicalIssueList group = HierarchicalIssueList.FirstOrDefault(g => g.Name == jqlName);
                        if (group != null)
                        {
                            // 중복 추가 방지
                            bool alreadyExists = group.Children.Any(issue => issue.ID == createdID);
                            if (!alreadyExists)
                            {
                                group.Children.Add(createdJiraIssue);
                                Logger.Log(MessageType.Info, $"[OnNewIssueCreated] '{createdKey}' added to group '{jqlName}'");
                            }
                        }
                        else
                        {
                            // 해당 그룹이 없으면 새로 추가
                            ObservableCollection<JiraIssue> newChildren = new ObservableCollection<JiraIssue> { createdJiraIssue };
                            HierarchicalIssueList.Add(new HierarchicalIssueList
                            {
                                Name = jqlName,
                                Children = newChildren
                            });
                            Logger.Log(MessageType.Info, $"[OnNewIssueCreated] '{createdKey}' added to new group '{jqlName}'");
                        }
                    }
                }
            });
        }

        public void OnIssueDeleted(JiraIssue deletedJiraIssue, Dictionary<string, JiraIssue> jiraIssuesByID, Dictionary<string, List<string>> jiraIssuesByJQL)
        {
            string deletedID = deletedJiraIssue.ID;
            string deletedKey = deletedJiraIssue.Key;
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (HierarchicalIssueList group in HierarchicalIssueList)
                {
                    int removedCount = 0;
                    for (int i = group.Children.Count - 1; i >= 0; --i) 
                    {
                        if (group.Children[i].ID == deletedID)
                        {
                            group.Children.RemoveAt(i);
                            removedCount++;
                        }
                    }

                    if (removedCount > 0)
                    {
                        Logger.Log(MessageType.Info, $"[OnIssueDeleted] '{deletedKey}' removed from group '{group.Name}' ({removedCount}개 제거)");
                    }
                }
            });
        }

        public void OnFilterDeleted(FilterVM filterToDelete)
        {
            string filterName = filterToDelete.FilterName;

            Application.Current.Dispatcher.Invoke(() =>
            {
                var groupToRemove = HierarchicalIssueList.FirstOrDefault(g => g.Name == filterName);
                if (groupToRemove != null)
                {
                    HierarchicalIssueList.Remove(groupToRemove);
                    Logger.Log(MessageType.Info, $"[OnFilterDeleted] Group '{filterName}' removed from issue list");
                }
                else
                {
                    Logger.Log(MessageType.Warning, $"[OnFilterDeleted] Group '{filterName}' not found");
                }
            });
        }

        public void OnNewFilterCreated(FilterVM newFilter, List<string> filteredJiraIssueIDs, Dictionary<string, JiraIssue> jiraIssuesByID)
        {
            List<JiraIssue> jiraIssues = new List<JiraIssue>();

            foreach (string id in filteredJiraIssueIDs)
            {
                if (jiraIssuesByID.TryGetValue(id, out JiraIssue issue))
                {
                    bool isSubtaskOfAnother = jiraIssuesByID.Values.Any(parent =>
                        parent.Fields.SubTasks != null &&
                        parent.Fields.SubTasks.Any(sub => sub.ID == id));

                    if (!isSubtaskOfAnother)
                    {
                        jiraIssues.Add(issue);
                    }
                }
                else
                {
                    Logger.Log(MessageType.Warning, $"[OnNewFilterCreated] ID '{id}' not found in jiraIssuesByID");
                }
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                var existingGroup = HierarchicalIssueList.FirstOrDefault(g => g.Name == newFilter.FilterName);
                if (existingGroup != null)
                {
                    existingGroup.Children.Clear();
                    foreach (var issue in jiraIssues)
                    {
                        existingGroup.Children.Add(issue);
                    }

                    Logger.Log(MessageType.Info, $"[OnNewFilterCreated] Updated group '{newFilter.FilterName}' with {jiraIssues.Count} issues");
                }
                else
                {
                    HierarchicalIssueList.Add(new HierarchicalIssueList
                    {
                        Name = newFilter.FilterName,
                        Children = new ObservableCollection<JiraIssue>(jiraIssues)
                    });

                    Logger.Log(MessageType.Info, $"[OnNewFilterCreated] Created new group '{newFilter.FilterName}' with {jiraIssues.Count} issues");
                }
            });
        }

        public void OnFilterUpdated(string previousFilterName, FilterVM filterToUpdate, List<string> filteredJiraIssueIDs, Dictionary<string, JiraIssue> jiraIssuesByID)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Step 1: 기존 그룹의 위치를 찾음
                int oldIndex = HierarchicalIssueList.ToList().FindIndex(g => g.Name == previousFilterName);
                if (oldIndex >= 0)
                {
                    HierarchicalIssueList.RemoveAt(oldIndex);
                    Logger.Log(MessageType.Info, $"[OnFilterUpdated] Removed old group '{previousFilterName}' at index {oldIndex}");
                }
                else
                {
                    oldIndex = HierarchicalIssueList.Count; // 없는 경우는 맨 뒤에 추가
                }

                // Step 2: 순서 보존 + SubTask 제외
                ObservableCollection<JiraIssue> filteredIssues = new ObservableCollection<JiraIssue>(
                    filteredJiraIssueIDs
                        .Where(id => jiraIssuesByID.TryGetValue(id, out JiraIssue issue) &&
                                     !jiraIssuesByID.Values.Any(parent =>
                                         parent.Fields.SubTasks != null &&
                                         parent.Fields.SubTasks.Any(sub => sub.ID == id)))
                        .Select(id => jiraIssuesByID[id])
                );

                // Step 3: 기존 위치에 삽입
                HierarchicalIssueList.Insert(oldIndex, new HierarchicalIssueList
                {
                    Name = filterToUpdate.FilterName,
                    Children = filteredIssues
                });

                Logger.Log(MessageType.Info, $"[OnFilterUpdated] Inserted updated group '{filterToUpdate.FilterName}' at index {oldIndex} with {filteredIssues.Count} issues");
            });
        }

        public void OnFilterOrderChanged(int oldIndex, int newIndex)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (oldIndex < 0 || oldIndex >= HierarchicalIssueList.Count ||
                    newIndex < 0 || newIndex >= HierarchicalIssueList.Count ||
                    oldIndex == newIndex)
                {
                    Logger.Log(MessageType.Warning, $"[OnFilterOrderChanged] Invalid indices: oldIndex={oldIndex}, newIndex={newIndex}");
                    return;
                }

                HierarchicalIssueList.Move(oldIndex, newIndex);

                Logger.Log(MessageType.Info, $"[OnFilterOrderChanged] Filter group moved from index {oldIndex} to {newIndex}");
            });
        }
    }
}

