namespace JiraClient
{
    public class ApplicationEntryPoint
    {
        [STAThread]
        public static void Main()
        {
            App app = new App();
            app.InitializeComponent();

            MainWindow mainWindow = new MainWindow();

            app.Run(mainWindow);
        }
    }
}
