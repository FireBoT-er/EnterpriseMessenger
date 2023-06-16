using Avalonia.Threading;
using EnterpriseMessengerUI.Models;
using Microsoft.AspNetCore.SignalR.Client;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Text.Json;

namespace EnterpriseMessengerUI.ViewModels
{
    public class NewChatWindowViewModel : ViewModelBase
    {
        public NewChatWindowViewModel()
        {
            this.WhenAnyValue(x => x.SelectedUsers.Count).Subscribe(_ => UpdateConfirmEnabled());
            this.WhenAnyValue(x => x.ChatName).Subscribe(_ => UpdateConfirmEnabled());

            _Users = new();

            AddConnectionHandlers();

            ConfirmCommand = ReactiveCommand.Create(() =>
            {
                if (SelectedUsers.Count > 1)
                {
                    string result = ChatName + ".";
                    foreach (var user in SelectedUsers)
                    {
                        result += user.Id + ";";
                    }

                    return result.Remove(result.Length-1);
                }
                else
                {
                    return SelectedUsers[0].Id;
                }
            });

            this.WhenAnyValue(x => x.Users.Count).Subscribe(_ => UpdateUsersVisible());
        }

        private void AddConnectionHandlers()
        {
            MainWindowViewModel.Connection.On<List<JsonElement>>("SendUsers", (users) =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    foreach (var item in users)
                    {
                        _Users.Add(new UserModel(
                            item.GetProperty("id").ToString(),
                            item.GetProperty("hasPhoto").GetBoolean(),
                            $"{item.GetProperty("surname")} {item.GetProperty("name")} {item.GetProperty("patronymic")}",
                            item.GetProperty("position").ToString())
                        );
                    }
                });
            });
        }

        private ObservableCollection<UserModel> _Users;

        public ObservableCollection<UserModel> Users
        {
            get => _Users;
            set => this.RaiseAndSetIfChanged(ref _Users, value);
        }

        public ObservableCollection<UserModel> SelectedUsers { get; } = new ObservableCollection<UserModel>();

        public ReactiveCommand<Unit, string> ConfirmCommand { get; }

        private string _ChatName = string.Empty;

        public string ChatName
        {
            get => _ChatName;
            set => this.RaiseAndSetIfChanged(ref _ChatName, value);
        }

        private string _ConfirmBText = string.Empty;

        public string ConfirmBText
        {
            get => _ConfirmBText;
            set => this.RaiseAndSetIfChanged(ref _ConfirmBText, value);
        }

        private bool _ChatNameEnabled;

        public bool ChatNameEnabled
        {
            get => _ChatNameEnabled;
            protected set => this.RaiseAndSetIfChanged(ref _ChatNameEnabled, value);
        }

        private bool _ConfirmEnabled;

        public bool ConfirmEnabled
        {
            get => _ConfirmEnabled;
            protected set => this.RaiseAndSetIfChanged(ref _ConfirmEnabled, value);
        }

        private void UpdateConfirmEnabled()
        {
            switch (SelectedUsers.Count)
            {
                case > 1:
                    ChatNameEnabled = true;
                    ConfirmEnabled = !string.IsNullOrWhiteSpace(ChatName);
                    ConfirmBText = "Создать групповой чат";
                    break;
                case > 0:
                    ChatNameEnabled = false;
                    ConfirmEnabled = true;
                    ConfirmBText = "Открыть диалог";
                    break;
                default:
                    ChatNameEnabled = false;
                    ConfirmEnabled = false;
                    ConfirmBText = "Открыть диалог";
                    break;
            }
        }

        private bool _IsUsersVisible;

        public bool IsUsersVisible
        {
            get => _IsUsersVisible;
            protected set => this.RaiseAndSetIfChanged(ref _IsUsersVisible, value);
        }

        private void UpdateUsersVisible()
        {
            IsUsersVisible = Users != null && Users.Count > 0;
        }
    }
}
