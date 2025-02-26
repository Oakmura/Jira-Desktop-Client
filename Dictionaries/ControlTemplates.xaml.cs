using System.Windows;

namespace JiraClient.Dictionaries
{
    public partial class ControlTemplates : ResourceDictionary
    {
        private void OnCloseButtonClick(object sender, RoutedEventArgs e)
        {
            var window = (Window)((FrameworkElement)sender).TemplatedParent;
            window.Close();
        }

        private void OnMaximizeRestoreButtonClick(object sender, RoutedEventArgs e)
        {
            var window = (Window)((FrameworkElement)sender).TemplatedParent;
            window.WindowState = (window.WindowState == WindowState.Normal) ? WindowState.Maximized : WindowState.Normal;
        }

        private void OnMinimizeButtonClick(object sender, RoutedEventArgs e)
        {
            var window = (Window)((FrameworkElement)sender).TemplatedParent;
            window.WindowState = WindowState.Minimized;
        }
    }
}
