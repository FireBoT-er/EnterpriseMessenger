using Avalonia;
using Avalonia.Controls;
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
    public partial class NewChatWindow : ReactiveWindow<NewChatWindowViewModel>
    {
        public NewChatWindow()
        {
            InitializeComponent();

            Loaded += NewChatWindow_Loaded;
            realSearchTB.TextChanged += RealSearchTB_TextChanged;
            usersLB.ItemContainerGenerator.Materialized += ItemContainerGenerator_Materialized;

            this.WhenActivated(d => d(ViewModel!.ConfirmCommand.Subscribe(Close)));
        }

        private async void NewChatWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            try
            {
                await MainWindowViewModel.Connection.InvokeAsync("GetUsers", string.Empty, 0);
            }
            catch
            {
                ConnectionLost();
            }
        }

        private void ItemContainerGenerator_Materialized(object? sender, Avalonia.Controls.Generators.ItemContainerEventArgs e)
        {
            usersLB.GetVisualDescendants().OfType<ScrollViewer>().First().ScrollChanged += UsersLB_ScrollChanged;
        }

        private Timer? SearchTimer;

        private void RealSearchTB_TextChanged(object? sender, TextChangedEventArgs e)
        {
            SearchTimer?.Dispose();

            SearchTimer = new Timer(async state =>
            {
                ((ObservableCollection<UserModel>)usersLB.Items!).Clear();
                
                try
                {
                    await MainWindowViewModel.Connection.InvokeAsync("GetUsers", ((TextBox)sender!).Text!.ToLower(), 0);
                }
                catch
                {
                    await Dispatcher.UIThread.InvokeAsync(ConnectionLost);
                    return;
                }
            }, null, 500, Timeout.Infinite);
        }

        private void ChatNameTB_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == IsEnabledProperty)
            {
                chatNameTB.Clear();
            }
        }

        private void UsersLB_ScrollChanged(object? sender, ScrollChangedEventArgs e)
        {
            var scrollViewer = (ScrollViewer)sender!;

            if (scrollViewer.Offset.Y+6 >= scrollViewer.Extent.Height && e.OffsetDelta.Y > 0)
            {
                SearchTimer?.Dispose();

                SearchTimer = new Timer(async state =>
                {
                    try
                    {
                        await MainWindowViewModel.Connection.InvokeAsync("GetUsers", realSearchTB!.Text == null ? string.Empty : realSearchTB!.Text.ToLower(), ((ObservableCollection<UserModel>)usersLB.Items!).Count);
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
