using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using EnterpriseMessengerUI.Models;
using EnterpriseMessengerUI.ViewModels;
using MessageBox.Avalonia;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Models;
using Microsoft.AspNetCore.SignalR.Client;
using System.Linq;
using System.Text.Json;

namespace EnterpriseMessengerUI.Views
{
    public partial class NoteWindow : ReactiveWindow<NoteWindowViewModel>
    {
        public NoteWindow()
        {
            InitializeComponent();
            Loaded += NoteWindow_Loaded;
            AddConnectionHandlers();
        }

        private void AddConnectionHandlers()
        {
            MainWindowViewModel.Connection.On<JsonElement>("SendNote", (note) =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (ViewModel != null)
                    {
                        noteTextTB.Text = note.GetProperty("name").ToString();
                        isCheckedCB.IsChecked = note.GetProperty("isChecked").GetBoolean();

                        if (note.GetProperty("subPoints").GetArrayLength() > 0)
                        {
                            ViewModel.SubPoints.Clear();
                            ThereAreSubPoints();

                            foreach (var subPoint in note.GetProperty("subPoints").EnumerateArray())
                            {
                                ViewModel.SubPoints.Add(new NoteSubPointModel(
                                    subPoint.GetProperty("id").GetInt64(),
                                    subPoint.GetProperty("isChecked").GetBoolean(),
                                    subPoint.GetProperty("text").ToString())
                                );
                            }
                        }
                    }
                });
            });
        }

        private async void NoteWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            if (!ViewModel!.IsFirst)
            {
                Title = "Редактирование заметки";
                
                try
                {
                    await MainWindowViewModel.Connection.InvokeAsync("GetNote", ViewModel!.Id);
                }
                catch
                {
                    ServerSettings.ConnectionLost = true;
                    this.Close();
                }
            }
        }

        private void NoteCheckedChange(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                if (ViewModel.SubPoints.Count > 0)
                {
                    foreach (var subPoint in ViewModel.SubPoints)
                    {
                        subPoint.IsChecked = isCheckedCB.IsChecked!.Value;
                    }
                }
            }
        }

        private void AddSubPoint(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                if (!subPointsLB.IsVisible)
                {
                    ThereAreSubPoints();
                }

                ViewModel.SubPoints.Add(new NoteSubPointModel());
            }
        }

        private void ThereAreSubPoints()
        {
            subPointsLB.IsVisible = true;
            noteTextTB.Watermark = "Название заметки";
            noteTextTB.MinHeight = 0;
            noteTextTB.MaxHeight = 200;
        }

        private void SubPointTextTB_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                ((TextBox)sender!).Text = ((TextBox)sender!).Text!.Insert(((TextBox)sender!).CaretIndex, " ");
                ((TextBox)sender!).CaretIndex += 1;
            }
        }

        public void RemoveSubPoint(NoteSubPointModel subPoint)
        {
            if (ViewModel != null)
            {
                ViewModel.SubPoints.Remove(subPoint);

                if (ViewModel.SubPoints.Count == 0)
                {
                    NoSubPoints();
                }
            }
        }

        private async void RemoveAllSubPoints(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                var deleteMessage = MessageBoxManager.GetMessageBoxCustomWindow(new MessageBoxCustomParams
                {
                    ContentMessage = "Вы действительно хотите удалить все подпункты?",
                    ContentTitle = "Удаление подпунктов",
                    ButtonDefinitions = new[]
                    {
                        new ButtonDefinition() { Name = "Да", IsDefault = true },
                        new ButtonDefinition() { Name = "Нет", IsCancel = true },
                    },
                    WindowIcon = this.Icon,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                });

                if (await deleteMessage.ShowDialog(this) == "Да")
                {
                    ViewModel.SubPoints.Clear();
                    NoSubPoints();
                }
            }
        }

        private void NoSubPoints()
        {
            subPointsLB.IsVisible = false;
            noteTextTB.Watermark = "Заметка";
            noteTextTB.MinHeight = 350;
            noteTextTB.MaxHeight = 350;
        }

        private async void Delete(object sender, RoutedEventArgs e)
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
                    WindowIcon = this.Icon,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                });

                if (await deleteMessage.ShowDialog(this) == "Да")
                {
                    try
                    {
                        await MainWindowViewModel.Connection.InvokeAsync("DeleteNote", ViewModel.Id);
                    }
                    catch
                    {
                        ServerSettings.ConnectionLost = true;
                    }

                    this.Close();
                }
            }
        }

        private async void Save(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                try
                {
                    if (ViewModel.IsFirst)
                    {
                        await MainWindowViewModel.Connection.InvokeAsync("CreateNote", noteTextTB.Text, isCheckedCB.IsChecked, ViewModel.SubPoints.Where(sp => !string.IsNullOrWhiteSpace(sp.Text)));
                    }
                    else
                    {
                        await MainWindowViewModel.Connection.InvokeAsync("EditNote", ViewModel.Id, noteTextTB.Text, isCheckedCB.IsChecked, ViewModel.SubPoints.Where(sp => !string.IsNullOrWhiteSpace(sp.Text)));
                    }
                }
                catch
                {
                    ServerSettings.ConnectionLost = true;
                }

                this.Close();
            }
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
