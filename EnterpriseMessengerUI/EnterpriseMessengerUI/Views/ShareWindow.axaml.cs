using Avalonia.Controls;
using Avalonia.Controls.Generators;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using Avalonia.VisualTree;
using EnterpriseMessengerUI.Models;
using EnterpriseMessengerUI.ViewModels;
using Microsoft.AspNetCore.SignalR.Client;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace EnterpriseMessengerUI.Views
{
    public partial class ShareWindow : ReactiveWindow<ShareWindowViewModel>
    {
        public ShareWindow()
        {
            InitializeComponent();
            Loaded += NewChatWindow_Loaded;
            this.WhenActivated(d => d(ViewModel!.ConfirmCommand.Subscribe(Close)));
        }

        private async void NewChatWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            if (ViewModel!.ShowOwners)
            {
                try
                {
                    switch (ViewModel.ContentType)
                    {
                        case ShareContentType.Message:
                            Title = "Участники чата";
                            await MainWindowViewModel.Connection.InvokeAsync("ShowGroupChatParticipants", ViewModel!.IdString);
                            break;
                        case ShareContentType.Note:
                            Title = "Соавторы заметки";
                            await MainWindowViewModel.Connection.InvokeAsync("ShowNoteOwners", ViewModel!.IdLong);
                            break;
                    }
                }
                catch
                {
                    ConnectionLost();
                }
            }
            else
            {
                realSearchTB.TextChanged += RealSearchTB_TextChanged;
                usersLB.ItemContainerGenerator.Materialized += ItemContainerGenerator_Materialized;

                try
                {
                    switch (ViewModel.ContentType)
                    {
                        case ShareContentType.Message:
                            Title = "Добавить участников";
                            ViewModel!.ConfirmText = "Добавить";
                            await MainWindowViewModel.Connection.InvokeAsync("GetNotGroupChatParticipants", ViewModel.IdString, string.Empty, 0);
                            break;
                        case ShareContentType.Note:
                            Title = "Поделиться заметкой";
                            ViewModel!.ConfirmText = "Поделиться";
                            await MainWindowViewModel.Connection.InvokeAsync("GetNotNoteOwners", ViewModel.IdLong, string.Empty, 0);
                            break;
                    }
                }
                catch
                {
                    ConnectionLost();
                }
            }
        }

        private void ItemContainerGenerator_Materialized(object? sender, ItemContainerEventArgs e)
        {
            usersLB.GetVisualDescendants().OfType<ScrollViewer>().First().ScrollChanged += UsersLB_ScrollChanged;
        }

        private void UsersLB_ScrollChanged(object? sender, ScrollChangedEventArgs e)
        {
            if (ViewModel != null)
            {
                var scrollViewer = (ScrollViewer)sender!;

                if (scrollViewer.Offset.Y + 6 >= scrollViewer.Extent.Height && e.OffsetDelta.Y > 0)
                {
                    SearchTimer?.Dispose();

                    ShareContentType contentType = ViewModel.ContentType;
                    string idString = ViewModel.IdString;
                    long idLong = ViewModel.IdLong;
                    SearchTimer = new Timer(async state =>
                    {
                        try
                        {
                            switch (contentType)
                            {
                                case ShareContentType.Message:
                                    await MainWindowViewModel.Connection.InvokeAsync("GetNotGroupChatParticipants", idString, realSearchTB!.Text == null ? string.Empty : realSearchTB!.Text.ToLower(), ((ObservableCollection<UserModel>)usersLB.Items!).Count);
                                    break;
                                case ShareContentType.Note:
                                    await MainWindowViewModel.Connection.InvokeAsync("GetNotNoteOwners", idLong, realSearchTB!.Text == null ? string.Empty : realSearchTB!.Text.ToLower(), ((ObservableCollection<UserModel>)usersLB.Items!).Count);
                                    break;
                            }
                        }
                        catch
                        {
                            await Dispatcher.UIThread.InvokeAsync(ConnectionLost);
                            return;
                        }
                    }, null, 500, Timeout.Infinite);
                }
            }
        }

        private Timer? SearchTimer;

        private void RealSearchTB_TextChanged(object? sender, TextChangedEventArgs e)
        {
            if (ViewModel != null)
            {
                SearchTimer?.Dispose();

                ShareContentType contentType = ViewModel.ContentType;
                string idString = ViewModel.IdString;
                long idLong = ViewModel.IdLong;
                SearchTimer = new Timer(async state =>
                {
                    ((ObservableCollection<UserModel>)usersLB.Items!).Clear();
                    try
                    {
                        switch (contentType)
                        {
                            case ShareContentType.Message:
                                await MainWindowViewModel.Connection.InvokeAsync("GetNotGroupChatParticipants", idString, ((TextBox)sender!).Text!.ToLower(), 0);
                                break;
                            case ShareContentType.Note:
                                await MainWindowViewModel.Connection.InvokeAsync("GetNotNoteOwners", idLong, ((TextBox)sender!).Text!.ToLower(), 0);
                                break;
                        }
                    }
                    catch
                    {
                        await Dispatcher.UIThread.InvokeAsync(ConnectionLost);
                        return;
                    }
                }, null, 500, Timeout.Infinite);
            }
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ConnectionLost()
        {
            ServerSettings.ConnectionLost = true;
            this.Close();
        }
    }
}
