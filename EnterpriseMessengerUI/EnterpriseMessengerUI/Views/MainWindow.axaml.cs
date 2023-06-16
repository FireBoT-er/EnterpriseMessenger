using Avalonia.Controls;
using EnterpriseMessengerUI.ViewModels;
using MessageBox.Avalonia;
using MessageBox.Avalonia.DTO;

namespace EnterpriseMessengerUI.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (MainWindowViewModel.FilesAndServer.Count > 0)
            {
                MessageBoxManager.GetMessageBoxStandardWindow(
                    new MessageBoxStandardParams
                    {
                        ContentTitle = "Загрузка файлов",
                        ContentMessage = "Вы не можете выйти из программы, пока загружаются файлы",
                        WindowIcon = this.Icon,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    }).ShowDialog(this);

                e.Cancel = true;
            }
        }
    }
}