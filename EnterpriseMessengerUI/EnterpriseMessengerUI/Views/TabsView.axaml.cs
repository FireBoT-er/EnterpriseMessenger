using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using EnterpriseMessengerUI.ViewModels;

namespace EnterpriseMessengerUI.Views
{
    public partial class TabsView : UserControl
    {
        public TabsView()
        {
            InitializeComponent();
            Loaded += TabsView_Loaded;
        }

        private void TabsView_Loaded(object? sender, RoutedEventArgs e)
        {
            tabs.SelectedIndex = 1;

            tabs.ItemContainerGenerator.ContainerFromIndex(0)!.SetValue(IsEnabledProperty, false);
            tabs.ItemContainerGenerator.ContainerFromIndex(4)!.SetValue(IsEnabledProperty, false);

            tabs.ItemContainerGenerator.ContainerFromIndex(1)!.AddHandler(PointerPressedEvent, DeselectChat);
        }

        private void DeselectChat(object? sender, PointerPressedEventArgs e)
        {
            if (((TabItem)sender!).IsSelected)
            {
                (((sender as TabItem)!.Parent!.LogicalChildren[1] as ContentControl)!.Content as MessagesTabViewModel)!.SelectedChat = null;
            }
        }
    }
}