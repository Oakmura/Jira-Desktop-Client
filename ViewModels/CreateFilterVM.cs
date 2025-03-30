using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using JiraClient.Common;
using JiraClient.Utilities;

namespace JiraClient.ViewModels
{
    public class FilterVM : ViewModelBase
    {
        private string mFilterName;
        public string FilterName
        {
            get => mFilterName;
            set
            {
                if (mFilterName != value)
                {
                    mFilterName = value;
                    OnPropertyChanged();
                }
            }
        }
        private string mFilterJQL;
        public string FilterJQL
        {
            get => mFilterJQL;
            set
            {
                if (mFilterJQL != value)
                {
                    mFilterJQL = value;
                    OnPropertyChanged();
                }
            }
        }
        public FilterVM(string filterName, string filterJQL)
        {
            FilterName = filterName;
            FilterJQL = filterJQL;
        }
    }

    public class CreateFilterVM : ViewModelBase
    {
        private readonly string USER_JQL_PATH = Settings.USER_JQL_PATH;

        private FilterVM mSelectedFilter;
        public FilterVM SelectedFilter
        {
            get => mSelectedFilter;
            set
            {
                if (mSelectedFilter != value)
                {
                    mSelectedFilter = value;
                    OnPropertyChanged();

                    if (value != null)
                    {
                        NewFilterName = value.FilterName;
                        NewFilterJQL = value.FilterJQL;
                    }
                    else
                    {
                        NewFilterName = string.Empty;
                        NewFilterJQL = string.Empty;
                    }
                }
            }
        }

        private string mNewFilterName;
        public string NewFilterName
        {
            get => mNewFilterName;
            set
            {
                if (mNewFilterName != value)
                {
                    mNewFilterName = value;
                    OnPropertyChanged(nameof(NewFilterName));
                }
            }
        }

        private string mNewFilterJQL;
        public string NewFilterJQL
        {
            get => mNewFilterJQL;
            set
            {
                if (mNewFilterJQL != value)
                {
                    mNewFilterJQL = value;
                    OnPropertyChanged(nameof(NewFilterJQL));
                }
            }
        }

        public ObservableCollection<FilterVM> JqlFilters { get; private set; } = new ObservableCollection<FilterVM>();

        private HashSet<string> mFilterNameSet = new HashSet<string>();
        private HashSet<string> mFilterJqlSet = new HashSet<string>();

        public RelayCommand AddFilterCommand { get; }
        public RelayCommand UpdateFilterCommand { get; }
        public RelayCommand DeleteFilterCommand { get; }

        public CreateFilterVM()
        {
            NewFilterName = string.Empty;
            NewFilterJQL = string.Empty;

            AddFilterCommand = new RelayCommand(onAddFilterCommand);
            UpdateFilterCommand = new RelayCommand(onUpdateFilterCommand);
            DeleteFilterCommand = new RelayCommand(onDeleteFilterCommand);

            loadAllJiraIssues();
        }

        public void OnRefreshCommand()
        {
            _ = Logger.Log(MessageType.Info, "Refreshing CreateFilter ViewModel");
        }
        
        private void onAddFilterCommand()
        {
            if (mFilterNameSet.Contains(NewFilterName) || mFilterJqlSet.Contains(NewFilterJQL))
            {
                _ = Logger.Log(MessageType.Error, $"Filter name or JQL already exists");
                return;
            }

            if (NewFilterName.Trim().Length == 0 || NewFilterJQL.Trim().Length == 0)
            {
                _ = Logger.Log(MessageType.Error, $"Cannot create filter with empty name or JQL");
                return;
            }

            FilterVM newFilter = new FilterVM(NewFilterName, NewFilterJQL);

            mFilterNameSet.Add(newFilter.FilterName);
            mFilterJqlSet.Add(newFilter.FilterJQL);
            JqlFilters.Add(newFilter);

            try
            {
                if (!Path.Exists(USER_JQL_PATH))
                {
                    _ = Logger.Log(MessageType.Error, $"User JQL Path: {USER_JQL_PATH} does not exist");
                    return;
                }

                using (StreamWriter writer = File.AppendText(USER_JQL_PATH))
                {
                    writer.WriteLine($"{NewFilterName},{NewFilterJQL}");
                }
            }
            catch (Exception e)
            {
                _ = Logger.Log(MessageType.Error, e.Message);
                return;
            }
        }

        private void onUpdateFilterCommand()
        {
            string previousFilterName = SelectedFilter.FilterName;
            string previousFilterJQL = SelectedFilter.FilterJQL;
            if (!mFilterNameSet.Contains(previousFilterName) || !mFilterJqlSet.Contains(previousFilterJQL))
            {
                return;
            }

            mFilterNameSet.Remove(previousFilterName);
            mFilterNameSet.Add(NewFilterName);
            mFilterJqlSet.Remove(previousFilterJQL);
            mFilterJqlSet.Add(NewFilterJQL);

            for (int i = 0; i < JqlFilters.Count; ++i)
            {
                if (JqlFilters[i].FilterName.Equals(previousFilterName) && JqlFilters[i].FilterJQL.Equals(previousFilterJQL))
                {
                    JqlFilters[i].FilterName = NewFilterName;
                    JqlFilters[i].FilterJQL = NewFilterJQL;
                    break;
                }
            }

            try
            {
                if (!Path.Exists(USER_JQL_PATH))
                {
                    _ = Logger.Log(MessageType.Error, $"User JQL Path: {USER_JQL_PATH} does not exist");
                    return;
                }

                string[] JQLs = File.ReadAllLines(USER_JQL_PATH);
                for (int i = 0; i < JQLs.Length; ++i)
                {
                    if (JQLs[i].Trim().Length == 0)
                    {
                        continue;
                    }

                    string filterName = JQLs[i].Split(',')[0];
                    string filterJQL = JQLs[i].Split(',')[1];
                    if (filterName.Equals(previousFilterName) && filterJQL.Equals(previousFilterJQL))
                    {
                        JQLs[i] = $"{NewFilterName},{NewFilterJQL}";
                        break;
                    }
                }

                File.WriteAllLines(USER_JQL_PATH, JQLs);
            }
            catch (Exception e)
            {
                _ = Logger.Log(MessageType.Error, e.Message);
                return;
            }
        }

        private void onDeleteFilterCommand()
        {
            FilterVM filterToDelete = SelectedFilter;
            if (filterToDelete == null)
            {
                return;
            }

            mFilterNameSet.Remove(filterToDelete.FilterName);
            mFilterJqlSet.Remove(filterToDelete.FilterJQL);
            JqlFilters.Remove(filterToDelete);

            try
            {
                if (!Path.Exists(USER_JQL_PATH))
                {
                    _ = Logger.Log(MessageType.Error, $"User JQL Path: {USER_JQL_PATH} does not exist");
                    return;
                }

                string[] JQLs = File.ReadAllLines(USER_JQL_PATH);
                string targetLine = $"{filterToDelete.FilterName},{filterToDelete.FilterJQL}";
                var updatedLines = JQLs.Where(line => !line.Trim().Equals(targetLine)).ToArray();

                File.WriteAllLines(USER_JQL_PATH, updatedLines);
            }
            catch (Exception e)
            {
                _ = Logger.Log(MessageType.Error, e.Message);
                return;
            }
        }

        private void loadAllJiraIssues()
        {
            string[] JQLs;

            try
            {
                if (!Path.Exists(USER_JQL_PATH))
                {
                    _ = Logger.Log(MessageType.Error, $"User JQL Path: {USER_JQL_PATH} does not exist");
                    return;
                }

                JQLs = File.ReadAllLines(USER_JQL_PATH);
                foreach (string JQL in JQLs)
                {
                    if (JQL.Trim().Length == 0)
                    {
                        continue;
                    }

                    string filterName = JQL.Split(',')[0];
                    string filterJQL = JQL.Split(',')[1];
                    FilterVM filterVM = new FilterVM(filterName, filterJQL);

                    JqlFilters.Add(filterVM);
                    mFilterNameSet.Add(filterName);
                    mFilterJqlSet.Add(filterJQL);
                }
            }
            catch (Exception e)
            {
                _ = Logger.Log(MessageType.Error, e.Message);
                return;
            }
        }
    }
}
