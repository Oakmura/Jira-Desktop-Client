using JiraClient.JiraAPI;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace JiraClient.Views
{
    public partial class IssueFinderView : Window
    {
        private readonly Dictionary<string, JiraIssue> _allIssues;
        private ObservableCollection<JiraIssue> _filteredIssues;

        public JiraIssue SelectedIssue => IssueListBox.SelectedItem as JiraIssue;

        public IssueFinderView(Dictionary<string, JiraIssue> allIssues)
        {
            InitializeComponent();
            _allIssues = allIssues;
            _filteredIssues = new ObservableCollection<JiraIssue>(_allIssues.Values.OrderByDescending(i => i.Key));

            IssueListBox.ItemsSource = _filteredIssues;
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            string query = SearchBox.Text.Trim().ToLower();
            _filteredIssues.Clear();

            foreach (var issue in _allIssues.Values.Where(i =>
                i.Key.ToLower().Contains(query) ||
                (i.Fields?.Summary?.ToLower()?.Contains(query) ?? false)))
            {
                _filteredIssues.Add(issue);
            }
        }

        private void OnSearchBoxPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                if (IssueListBox.Items.Count > 0)
                {
                    IssueListBox.Focus();
                    IssueListBox.SelectedIndex = 0;

                    var item = (ListBoxItem)IssueListBox.ItemContainerGenerator.ContainerFromIndex(0);
                    item?.Focus();

                    e.Handled = true;
                }
            }
        }

        private void OnIssueListBoxPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up && IssueListBox.SelectedIndex == 0)
            {
                SearchBox.Focus();
                e.Handled = true;
            }
        }

        private void OnConfirmClick(object sender, RoutedEventArgs e)
        {
            if (SelectedIssue != null)
            {
                DialogResult = true;
                Close();
            }
        }

        private void OnIssueDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (SelectedIssue != null)
            {
                DialogResult = true;
                Close();
            }
        }

        private void OnCloseButtonClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
        {
            // ESC → 창 닫기
            if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
                return;
            }

            // SearchBox가 이미 포커스면 아무것도 안 함
            if (SearchBox.IsKeyboardFocusWithin)
            {
                return;
            }

            // 아래 키들은 기존 동작 유지
            switch (e.Key)
            {
                case Key.Up:
                case Key.Down:
                case Key.Left:
                case Key.Right:
                case Key.Enter:
                case Key.Tab:
                    return;
            }

            // 그 외 키가 눌렸으면 SearchBox로 포커스
            SearchBox.Focus();
        }
    }
}
