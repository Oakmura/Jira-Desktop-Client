using System.Windows;
using System.Windows.Controls;
using JiraClient.Utilities;

namespace JiraClient.Views
{
    public partial class LoggerView : UserControl
    {
        public LoggerView()
        {
            InitializeComponent();
        }

        private void OnClearButtonClick(object sender, RoutedEventArgs e)
        {
            Task task = Logger.Clear();
        }

        private void OnMessageFilterButtonClick(object sender, RoutedEventArgs e)
        {
            var filter = 0x0;
            if (toggleButtonInfo.IsChecked == true) filter |= (int)MessageType.Info;
            if (toggleButtonWarnings.IsChecked == true) filter |= (int)MessageType.Warning;
            if (toggleButtonErrors.IsChecked == true) filter |= (int)MessageType.Error;
            Logger.SetMessageFilter(filter);
        }

        private void OnCopyLogMessageClick(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem &&
                menuItem.Parent is ContextMenu contextMenu &&
                contextMenu.PlacementTarget is FrameworkElement fe &&
                fe.DataContext is LogMessage message)
            {
                Clipboard.SetText(message.Message);
            }
        }
    }
}
