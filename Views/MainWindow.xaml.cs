using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using JiraClient.DllWrappers;
using System.Windows.Controls;
using System.Windows.Interop;
using JiraClient.Utilities;

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

        public MainWindow()
        {
            InitializeComponent();

            SetupTrayIcon();
            RegisterGlobalHotKey();
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
            Logger.Log(MessageType.Info, $"Hot Key Registration: {bSuccess}");

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