using Avalonia.Threading;
using EnterpriseMessengerUI.Models;
using Microsoft.AspNetCore.SignalR.Client;
using ReactiveUI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text.Json;
using System.Windows.Input;

namespace EnterpriseMessengerUI.ViewModels
{
    public class NotesTabViewModel : ViewModelBase
    {
        public NotesTabViewModel()
        {
            ShowNoteDialog = new();

            NoteOpenDialogCommand = ReactiveCommand.CreateFromTask(async ((bool, long, bool) settings) =>
            {
                var store = new NoteWindowViewModel { IsFirst = settings.Item1, Id = settings.Item2, IsAuthor = settings.Item3 };
                await ShowNoteDialog.Handle(store);

                if (ServerSettings.ConnectionLost)
                {
                    Parent!.NavigatePreviousCommand.Execute(null);
                }
            });

            ShowShareDialog = new();

            ShareOpenDialogCommand = ReactiveCommand.CreateFromTask(async((bool, long, bool) settings) =>
            {
                var store = new ShareWindowViewModel() { ContentType = ShareContentType.Note, ShowOwners = settings.Item1, IdLong = settings.Item2, IsAuthor = settings.Item3 };
                var result = await ShowShareDialog.Handle(store);

                if (result != null)
                {
                    try
                    {
                        if (!settings.Item1)
                        {
                            await MainWindowViewModel.Connection.InvokeAsync("ShareNote", SharingNoteId, result);
                        }
                        else
                        {
                            await MainWindowViewModel.Connection.InvokeAsync("RemoveOwnersFromNote", SharingNoteId, result);
                        }
                    }
                    catch
                    {
                        ConnectionLost();
                    }
                }
                else if (ServerSettings.ConnectionLost)
                {
                    Parent!.NavigatePreviousCommand.Execute(null);
                }
            });

            _Notes = new() { new NoteModel() };

            AddConnectionHandlers();
        }

        public MainWindowViewModel? Parent { get; set; }

        private void AddConnectionHandlers()
        {
            MainWindowViewModel.Connection.On<List<JsonElement>, List<bool>, List<bool>>("SendNotes", (notes, hasSubPoints, isAuthor) =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Notes = new() { new NoteModel() };

                    for (int i = 0; i < notes.Count; i++)
                    {
                        Notes.Add(new NoteModel(
                            notes[i].GetProperty("id").GetInt64(),
                            notes[i].GetProperty("name").ToString(),
                            notes[i].GetProperty("isChecked").GetBoolean(),
                            hasSubPoints[i],
                            isAuthor[i])
                        );
                    }
                });
            });

            MainWindowViewModel.Connection.On<JsonElement, bool, bool, JsonElement>("SendNewNote", (note, hasSubPoints, isAuthor, author) =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    string name = note.GetProperty("name").ToString();

                    Notes.Add(new NoteModel(
                        note.GetProperty("id").GetInt64(),
                        name,
                        note.GetProperty("isChecked").GetBoolean(),
                        hasSubPoints,
                        isAuthor)
                    );

                    if (!isAuthor)
                    {
                        Notification.Show(
                            author.GetProperty("hasPhoto").GetBoolean() ? $"{ServerSettings.ServerAddress}/UserFiles/Avatars/{author.GetProperty("id")}.png" : "/Assets/user.png",
                            author.GetProperty("surname").ToString(),
                            author.GetProperty("name").ToString(),
                            author.GetProperty("patronymic").ToString(),
                            $"Поделился с вами заметкой: «{name}»"
                        );
                    }
                });
            });

            MainWindowViewModel.Connection.On<JsonElement, bool>("SendEditedNote", (note, hasSubPoints) =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    var editedNote = Notes.First(n => n.Id == note.GetProperty("id").GetInt64());
                    editedNote.Name = note.GetProperty("name").ToString();
                    editedNote.IsChecked = note.GetProperty("isChecked").GetBoolean();
                    editedNote.HasSubPoints = hasSubPoints;
                    editedNote.RelatedPropertiesSet();
                });
            });

            MainWindowViewModel.Connection.On<long>("NoteHasBeenDeleted", (id) =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Notes.Remove(Notes.First(n => n.Id == id));
                });
            });
        }

        public long SharingNoteId;

        private ObservableCollection<NoteModel> _Notes;

        public ObservableCollection<NoteModel> Notes
        {
            get => _Notes;
            set => this.RaiseAndSetIfChanged(ref _Notes, value);
        }

        private void ConnectionLost()
        {
            ServerSettings.ConnectionLost = true;
            Parent!.NavigatePreviousCommand.Execute(null);
        }

        public ICommand NoteOpenDialogCommand { get; }

        public Interaction<NoteWindowViewModel, string?> ShowNoteDialog { get; }

        public ICommand ShareOpenDialogCommand { get; }

        public Interaction<ShareWindowViewModel, List<string>?> ShowShareDialog { get; }
    }
}
