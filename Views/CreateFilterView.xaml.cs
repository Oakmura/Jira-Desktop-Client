using System.Windows;
using System.Windows.Controls;
using JiraClient.ViewModels;
using System.Windows.Input;
using System.Windows.Media;
using JiraClient.Utilities;

namespace JiraClient.Views
{
    /// <summary>
    /// Interaction logic for CreateFilterView.xaml
    /// </summary>
    public partial class CreateFilterView : UserControl
    {
        public CreateFilterView()
        {
            InitializeComponent();
        }

        private Point _dragStartPoint;

        private void FilterListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        private void FilterListBox_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = e.GetPosition(null);
            Vector diff = _dragStartPoint - mousePos;

            if (e.LeftButton == MouseButtonState.Pressed &&
                (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                 Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                ListBox listBox = sender as ListBox;
                FilterVM selectedItem = listBox?.SelectedItem as FilterVM;
                if (selectedItem != null)
                {
                    DragDrop.DoDragDrop(listBox, selectedItem, DragDropEffects.Move);
                }
            }
        }

        private void FilterListBox_Drop(object sender, DragEventArgs e)
        {
            FilterVM droppedData = e.Data.GetData(typeof(FilterVM)) as FilterVM;
            if (droppedData == null)
                return;

            ListBox listBox = sender as ListBox;
            Point pos = e.GetPosition(listBox);
            UIElement targetItemContainer = listBox.InputHitTest(pos) as UIElement;

            while (targetItemContainer != null)
            {
                object targetItem = listBox.ItemContainerGenerator.ItemFromContainer(targetItemContainer);
                if (targetItem is FilterVM targetData && !ReferenceEquals(targetData, droppedData))
                {
                    var vm = DataContext as CreateFilterVM;
                    var list = vm.JqlFilters;

                    int oldIndex = list.IndexOf(droppedData);
                    int newIndex = list.IndexOf(targetData);

                    if (oldIndex >= 0 && newIndex >= 0 && oldIndex != newIndex)
                    {
                        list.Move(oldIndex, newIndex);

                        MainWindowVM mainWindowVM = Application.Current.MainWindow.DataContext as MainWindowVM;
                        if (mainWindowVM == null)
                        {
                            Logger.Log(MessageType.Warning, "MainWindowVM을 찾을 수 없습니다.");
                            return;
                        }

                        mainWindowVM.OnFilterOrderChanged(oldIndex, newIndex);
                    }
                    break;
                }

                targetItemContainer = VisualTreeHelper.GetParent(targetItemContainer) as UIElement;
            }
        }
    }
}
