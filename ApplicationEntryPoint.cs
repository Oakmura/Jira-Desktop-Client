using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using JiraClient.ViewModels;

namespace JiraClient
{
    public class ApplicationEntryPoint
    {
        [STAThread]
        public static void Main()
        {
            App app = new App();
            app.InitializeComponent();

            MainWindowVM mainWindowVM = new MainWindowVM();
            MainWindow mainWindow = new MainWindow();
            mainWindow.DataContext = mainWindowVM;

            app.Run(mainWindow);
        }
    }
}
