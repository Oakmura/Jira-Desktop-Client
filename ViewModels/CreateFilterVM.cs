using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using JiraClient.Common;
using JiraClient.Utilities;

namespace JiraClient.ViewModels
{
    public class CreateFilterVM : ViewModelBase
    {
        private readonly string USER_JQL_PATH = "./../../../UserData/JQLs.txt";

        private string mTitle;
        public string Title
        {
            get => mTitle;
            set
            {
                if (mTitle != value)
                {
                    mTitle = value;
                    OnPropertyChanged();
                }
            }
        }

        private string mSelectedFilter;
        public string SelectedFilter
        {
            get => mSelectedFilter;
            set
            {
                if (mSelectedFilter != value)
                {
                    mSelectedFilter = value;
                    OnPropertyChanged();

                    NewFilterText = value;
                }
            }
        }

        private string mNewFilterText;
        public string NewFilterText
        {
            get => mNewFilterText;
            set
            {
                if (mNewFilterText != value)
                {
                    mNewFilterText = value;
                    OnPropertyChanged(nameof(NewFilterText));
                }
            }
        }

        public ObservableCollection<String> JqlFilters { get; private set; } = new ObservableCollection<string>();

        private HashSet<string> mFilterSet = new HashSet<string>();

        public RelayCommand AddFilterCommand { get; }
        public RelayCommand UpdateFilterCommand { get; }
        public RelayCommand DeleteFilterCommand { get; }

        public CreateFilterVM()
        {
            Title = "Filters and Custom JQL";
            NewFilterText = "";

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
            if (mFilterSet.Contains(NewFilterText))
            {
                return;
            }

            if (NewFilterText.Trim().Length == 0)
            {
                return;
            }

            mFilterSet.Add(NewFilterText);
            JqlFilters.Add(NewFilterText);

            try
            {
                if (!Path.Exists(USER_JQL_PATH))
                {
                    _ = Logger.Log(MessageType.Error, $"User JQL Path: {USER_JQL_PATH} does not exist");
                    return;
                }

                File.AppendText(NewFilterText);
            }
            catch (Exception e)
            {
                _ = Logger.Log(MessageType.Error, e.Message);
                return;
            }
        }

        private void onUpdateFilterCommand()
        {
            string previousFilterText = SelectedFilter;

            if (!mFilterSet.Contains(previousFilterText))
            {
                return;
            }

            mFilterSet.Remove(previousFilterText);
            mFilterSet.Add(NewFilterText);
            for (int i = 0; i < JqlFilters.Count; ++i)
            {
                if (JqlFilters[i].Equals(previousFilterText))
                {
                    JqlFilters[i] = NewFilterText;
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
                    if (JQLs[i].Equals(previousFilterText))
                    {
                        JQLs[i] = NewFilterText;
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
            string filterToDelete = NewFilterText;
            if (!mFilterSet.Contains(filterToDelete))
            {
                return;
            }

            mFilterSet.Remove(filterToDelete);
            JqlFilters.Remove(filterToDelete);

            try
            {
                if (!Path.Exists(USER_JQL_PATH))
                {
                    _ = Logger.Log(MessageType.Error, $"User JQL Path: {USER_JQL_PATH} does not exist");
                    return;
                }

                string[] JQLs = File.ReadAllLines(USER_JQL_PATH);
                var updatedJQLs = JQLs.Where(line => !line.Equals(filterToDelete)).ToArray();

                File.WriteAllLines(USER_JQL_PATH, updatedJQLs);
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
                    JqlFilters.Add(JQL);
                    mFilterSet.Add(JQL);
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
