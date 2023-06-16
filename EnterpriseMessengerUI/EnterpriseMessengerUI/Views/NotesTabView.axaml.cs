using Avalonia.Controls;
using Avalonia.ReactiveUI;
using EnterpriseMessengerUI.ViewModels;
using MessageBox.Avalonia;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Models;
using Microsoft.AspNetCore.SignalR.Client;
using ReactiveUI;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EnterpriseMessengerUI.Views
{
    public partial class NotesTabView : ReactiveUserControl<NotesTabViewModel>
    {
        public NotesTabView()
        {
            InitializeComponent();
            Loaded += NotesTabView_Loaded;

            this.WhenActivated(d => d(ViewModel!.ShowNoteDialog.RegisterHandler(DoShowNoteDialogAsync)));
            this.WhenActivated(d => d(ViewModel!.ShowShareDialog.RegisterHandler(DoShowShareDialogAsync)));
        }

        private async void NotesTabView_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            ViewModel!.Parent = (MainWindowViewModel)this.Parent!.Parent!.Parent!.Parent!.Parent!.DataContext!;

            try
            {
                await MainWindowViewModel.Connection.InvokeAsync("GetNotes");
            }
            catch
            {
                ConnectionLost();
            }
        }

        public void Create()
        {
            ViewModel?.NoteOpenDialogCommand.Execute((true, (long)-1, false));
        }

        public void Edit(long id, bool isAuthor)
        {
            ViewModel?.NoteOpenDialogCommand.Execute((false, id, isAuthor));
        }

        public void Share(bool showOwners, long id, bool isAuthor)
        {
            if (ViewModel != null)
            {
                ViewModel.SharingNoteId = id;
                ViewModel.ShareOpenDialogCommand.Execute((showOwners, id, isAuthor));
            }
        }

        public async void Delete(long id)
        {
            if (ViewModel != null)
            {
                var deleteMessage = MessageBoxManager.GetMessageBoxCustomWindow(new MessageBoxCustomParams
                {
                    ContentMessage = "Вы действительно хотите удалить данную заметку?",
                    ContentTitle = "Удаление заметки",
                    ButtonDefinitions = new[]
                    {
                        new ButtonDefinition() { Name = "Да", IsDefault = true },
                        new ButtonDefinition() { Name = "Нет", IsCancel = true },
                    },
                    WindowIcon = ((MainWindow)this.Parent!.Parent!.Parent!.Parent!.Parent!).Icon,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                });

                if (await deleteMessage.ShowDialog((MainWindow)this.Parent!.Parent!.Parent!.Parent!.Parent!) == "Да")
                {
                    try
                    {
                        await MainWindowViewModel.Connection.InvokeAsync("DeleteNote", id);
                    }
                    catch
                    {
                        ConnectionLost();
                    }
                }
            }
        }

        private void ConnectionLost()
        {
            ServerSettings.ConnectionLost = true;
            ((MainWindowViewModel)this.Parent!.Parent!.Parent!.Parent!.Parent!.DataContext!).NavigatePreviousCommand.Execute(null);
        }

        private async Task DoShowNoteDialogAsync(InteractionContext<NoteWindowViewModel, string?> interaction)
        {
            NoteWindow dialog = new() { DataContext = interaction.Input };

            var result = await dialog.ShowDialog<string?>((MainWindow)this.Parent!.Parent!.Parent!.Parent!.Parent!);
            interaction.SetOutput(result);
        }

        private async Task DoShowShareDialogAsync(InteractionContext<ShareWindowViewModel, List<string>?> interaction)
        {
            ShareWindow dialog = new() { DataContext = interaction.Input };

            var result = await dialog.ShowDialog<List<string>?>((MainWindow)this.Parent!.Parent!.Parent!.Parent!.Parent!);
            interaction.SetOutput(result);
        }
    }
}
