using Avalonia.Threading;
using EnterpriseMessengerUI.Models;
using Microsoft.AspNetCore.SignalR.Client;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Text.Json;

namespace EnterpriseMessengerUI.ViewModels
{
    public enum ShareContentType
    {
        Message,
        Note
    }

    public class ShareWindowViewModel : ViewModelBase
    {
        public ShareWindowViewModel()
        {
            this.WhenAnyValue(x => x.SelectedUsers.Count).Subscribe(_ => UpdateConfirmEnabled());

            _Users = new();
            _ConfirmText = string.Empty;

            AddConnectionHandlers();

            ConfirmCommand = ReactiveCommand.Create(() =>
            {
                return SelectedUsers.Select(su => su.Id).ToList();
            });

            this.WhenAnyValue(x => x.Users.Count).Subscribe(_ => UpdateUsersVisible());
        }

        private void AddConnectionHandlers()
        {
            MainWindowViewModel.Connection.On<List<JsonElement>>("SendUsers", (users) =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    bool firstUser = ShowOwners;
                    foreach (var item in users)
                    {
                        _Users.Add(new UserModel(
                            item.GetProperty("id").ToString(),
                            item.GetProperty("hasPhoto").GetBoolean(),
                            $"{item.GetProperty("surname")} {item.GetProperty("name")} {item.GetProperty("patronymic")}",
                            item.GetProperty("position").ToString(),
                            firstUser)
                        );

                        firstUser = false;
                    }
                });
            });
        }

        public ShareContentType ContentType { get; set; }

        public bool ShowOwners { get; set; }

        public long IdLong { get; set; }

        public string IdString { get; set; } = string.Empty;

        public bool IsAuthor { get; set; }

        private ObservableCollection<UserModel> _Users;

        public ObservableCollection<UserModel> Users
        {
            get => _Users;
            set => this.RaiseAndSetIfChanged(ref _Users, value);
        }

        public ObservableCollection<UserModel> SelectedUsers { get; } = new ObservableCollection<UserModel>();

        public ReactiveCommand<Unit, List<string>> ConfirmCommand { get; }

        private string _ConfirmText;

        public string ConfirmText
        {
            get => _ConfirmText;
            set => this.RaiseAndSetIfChanged(ref _ConfirmText, value);
        }

        private bool _ConfirmEnabled;

        public bool ConfirmEnabled
        {
            get => _ConfirmEnabled;
            protected set => this.RaiseAndSetIfChanged(ref _ConfirmEnabled, value);
        }

        private void UpdateConfirmEnabled()
        {
            ConfirmEnabled = SelectedUsers.Count > 0;
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
