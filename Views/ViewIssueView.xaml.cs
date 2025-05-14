using System.Windows;
using System.Windows.Controls;
using JiraClient.ViewModels;

namespace JiraClient.Views
{
    /// <summary>
    /// Interaction logic for ViewIssueView.xaml
    /// </summary>
    public partial class ViewIssueView : UserControl
    {
        public ViewIssueView()
        {
            InitializeComponent();
        }

        private void AttachmentDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
        }

        private async void AttachmentDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (files != null && files.Length > 0)
                {
                    ViewIssueVM vm = DataContext as ViewIssueVM;

                    if (vm != null)
                    {
                        foreach (string file in files)
                        {
                            await vm.UploadAttachmentAsync(file);
                        }

                        await vm.RefreshAttachmentsAsync();
                    }
                }
            }
        }

    }
}
