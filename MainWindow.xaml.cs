using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using JiraClient.DllWrappers;
using System.Windows.Controls;
using System.Windows.Interop;
using JiraClient.Utilities;
using System.Net.Http;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using JiraClient.ViewModels;
using JiraClient.Views;

namespace JiraClient
{
    public partial class MainWindow : Window
    {
        private TaskbarIcon mNotifyIcon;

        private const int HOTKEY_ID = 9000;
        private const int HOT_KEY_WINDOW_MESSAGE = 0x0312; // WM_HOTKEY

        private const int MOD_CONTROL = 0x0002;
        private const int MOD_SHIFT = 0x0004;
        private const int VK_F10 = 0x79;
        private const int VK_M = 0x4D;

        // Replace with your credentials
        private static string JIRA_BASE_URL;
        private static string USER_NAME;
        private static string API_TOKEN;

        public MainWindow()
        {
            InitializeComponent();

            SetupTrayIcon();
            RegisterGlobalHotKey();

            _ = FetchAllJiraIssues();
        }

        private async Task FetchAllJiraIssues()
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(JIRA_BASE_URL);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                string authInfo = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{USER_NAME}:{API_TOKEN}"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authInfo);

                // string apiUrl = $"/rest/api/latest/search?fields=issuetype,summary,description&maxResults=5000";
                string apiUrl = $"/rest/api/latest/search?maxResults=5000";

                Stopwatch sw = Stopwatch.StartNew();
                HttpResponseMessage response = await client.GetAsync(apiUrl);
                Debug.Assert(response.IsSuccessStatusCode);
                sw.Stop();
                _ = Logger.Log(MessageType.Info, $"Issue Fetch Took: {sw.Elapsed} seconds");

                string result = await response.Content.ReadAsStringAsync();
                JObject jsonResult = JObject.Parse(result);

                JiraIssueResponse jiraResponse = JsonConvert.DeserializeObject<JiraIssueResponse>(result);
                issueListView.DataContext = jiraResponse;
            }
        }

        private void SetupTrayIcon()
        {
            mNotifyIcon = new TaskbarIcon
            {
                Icon = new System.Drawing.Icon("./../../../Resources/jira-icon.ico"),
                ToolTipText = "MyApp",
                ContextMenu = new System.Windows.Controls.ContextMenu()
            };

            MenuItem settingMenuItem = new System.Windows.Controls.MenuItem { Header = "Setting" };
            MenuItem exitMenuItem = new System.Windows.Controls.MenuItem { Header = "Exit" };

            mNotifyIcon.TrayMouseDoubleClick += (s, e) => ShowWindow();
            exitMenuItem.Click += (s, e) => System.Windows.Application.Current.Shutdown();

            mNotifyIcon.ContextMenu.Items.Add(settingMenuItem);
            mNotifyIcon.ContextMenu.Items.Add(exitMenuItem);
        }

        private void RegisterGlobalHotKey()
        {
            WindowInteropHelper helper = new System.Windows.Interop.WindowInteropHelper(this);
            bool bSuccess = Win32DllWrappers.RegisterHotKey(helper.Handle, HOTKEY_ID, MOD_SHIFT, VK_F10);
            _ = Logger.Log(MessageType.Info, $"Hot Key Registration: {bSuccess}");

            System.Windows.Interop.ComponentDispatcher.ThreadFilterMessage += (ref System.Windows.Interop.MSG msg, ref bool handled) =>
            {
                if (msg.message == HOT_KEY_WINDOW_MESSAGE && msg.wParam.ToInt32() == HOTKEY_ID)
                {
                    ShowWindow();
                    handled = true;
                }
            };
        }

        private void ShowWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        private void OnWindowClosingEvent(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide(); // Hide the window instead of closing
        }

        protected override void OnClosed(EventArgs e)
        {
            Win32DllWrappers.UnregisterHotKey(new System.Windows.Interop.WindowInteropHelper(this).Handle, HOTKEY_ID);
            base.OnClosed(e);
        }
    }
}