using Avalonia.Threading;
using EnterpriseMessengerUI.Models;
using Microsoft.AspNetCore.SignalR.Client;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text.Json;
using System.Windows.Input;

namespace EnterpriseMessengerUI.ViewModels
{
    public class MessagesTabViewModel : ViewModelBase
    {
        public MessagesTabViewModel()
        {
            this.WhenAnyValue(x => x.SelectedChat).Subscribe(_ => OnChatSelectionChange());

            ShowNewChatDialog = new();

            NewChatOpenDialogCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var store = new NewChatWindowViewModel();
                var result = await ShowNewChatDialog.Handle(store);

                if (result != null)
                {
                    string[] resultGroup = result.Split('.');

                    if (resultGroup.Length == 1)
                    {
                        var chatItem = ChatItems.Where(ci => ci.ChatId == result);
                        if (!chatItem.Any())
                        {
                            IWishNewChat = true;

                            try
                            {
                                await MainWindowViewModel.Connection.InvokeAsync("GetNewChat", result);
                            }
                            catch
                            {
                                ConnectionLost();
                            }
                        }
                        else
                        {
                            SelectedChat = chatItem.First();
                        }
                    }
                    else
                    {
                        try
                        {
                            await MainWindowViewModel.Connection.InvokeAsync("CreateGroupChat", resultGroup[0], resultGroup[1]);
                        }
                        catch
                        {
                            ConnectionLost();
                        }
                    }
                }
                else if (ServerSettings.ConnectionLost)
                {
                    Parent!.NavigatePreviousCommand.Execute(null);
                }
            });

            ShowGroupChatEditDialog = new();

            GroupChatEditOpenDialogCommand = ReactiveCommand.CreateFromTask(async (string id) =>
            {
                var store = new GroupChatEditWindowViewModel() { Id = id };
                await ShowGroupChatEditDialog.Handle(store);

                if (ServerSettings.ConnectionLost)
                {
                    Parent!.NavigatePreviousCommand.Execute(null);
                }
            });

            ShowShareDialog = new();

            ShareOpenDialogCommand = ReactiveCommand.CreateFromTask(async ((bool, string, bool) settings) =>
            {
                var store = new ShareWindowViewModel() { ContentType = ShareContentType.Message, ShowOwners = settings.Item1, IdString = settings.Item2, IsAuthor = settings.Item3 };
                var result = await ShowShareDialog.Handle(store);

                if (result != null)
                {
                    try
                    {
                        if (!settings.Item1)
                        {
                            await MainWindowViewModel.Connection.InvokeAsync("AddParticipantsToGroupChat", SelectedChat!.ChatId, result);
                        }
                        else
                        {
                            await MainWindowViewModel.Connection.InvokeAsync("RemoveParticipantsFromGroupChat", SelectedChat!.ChatId, result);
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

            _ChatItems = new();
            _Messages = new();
            _SelectedMessages = new();
            _Attachments = new();
            _SelectedAttachments = new();

            _ForwardActive = false;

            _SearchWatermark = "Поиск";
            _ChatsAreEmptyMessage = "Диалоги не найдены.\nНачните новый при помощи кнопки «+».";
            _RemoveAllAttachmentsBText = "Открепить все";

            LoadCount = 0;

            AddConnectionHandlers();

            this.WhenAnyValue(x => x.ChatItems.Count).Subscribe(_ => UpdateChatsVisible());
            this.WhenAnyValue(x => x.Messages.Count).Subscribe(_ => UpdateMessagesLBVisible());
            this.WhenAnyValue(x => x.Attachments.Count).Subscribe(_ => UpdateIsThereAttachments());
            this.WhenAnyValue(x => x.SelectedAttachments.Count).Subscribe(_ => UpdateRemoveAllAttachmentsBText());
        }

        private bool IWishNewChat = false;

        private void AddConnectionHandlers()
        {
            MainWindowViewModel.Connection.On<JsonElement>("SendNewChat", (chat) =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ChatItems.Insert(0, new ChatModel(
                        chat.GetProperty("id").ToString(),
                        chat.GetProperty("hasPhoto").GetBoolean(),
                        chat.GetProperty("surname").ToString(),
                        chat.GetProperty("name").ToString(),
                        chat.GetProperty("patronymic").ToString(),
                        chat.GetProperty("position").ToString(),
                        chat.GetProperty("information").ToString(),
                        true,
                        (NetworkStatus)chat.GetProperty("networkStatus").GetInt32(),
                        -1, string.Empty, null, string.Empty)
                    );

                    if (IWishNewChat)
                    {
                        SelectedChat = ChatItems[0];
                        IWishNewChat = false;
                    }
                });
            });

            MainWindowViewModel.Connection.On<JsonElement>("SendNewGroupChat", (groupChat) =>
            {
                Dispatcher.UIThread.InvokeAsync(async() =>
                {
                    string id = groupChat.GetProperty("id").ToString();
                    string authorId = groupChat.GetProperty("authorId").ToString();

                    ChatItems.Insert(0, new ChatModel(
                        id,
                        groupChat.GetProperty("hasPhoto").GetBoolean(),
                        groupChat.GetProperty("name").ToString(),
                        authorId,
                        -1, string.Empty, null, string.Empty)
                    );

                    if (authorId == CurrentUserModel.Id)
                    {
                        SelectedChat = ChatItems[0];
                    }

                    try
                    {
                        await MainWindowViewModel.Connection.InvokeAsync("AddToGroupChatNow", id);
                    }
                    catch
                    {
                        ConnectionLost();
                    }
                });
            });

            MainWindowViewModel.Connection.On<string, string>("SendUpdatedGroupChatName", (chatId, name) =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ChatItems.First(ci => ci.ChatId == chatId).Surname = name;
                });
            });

            MainWindowViewModel.Connection.On<string>("GroupChatHasBeenDeleted", (chatId) =>
            {
                Dispatcher.UIThread.InvokeAsync(async() =>
                {
                    ChatItems.Remove(ChatItems.First(ci => ci.ChatId == chatId));

                    try
                    {
                        await MainWindowViewModel.Connection.InvokeAsync("RemoveFromGroupChatNow", chatId);
                    }
                    catch
                    {
                        ConnectionLost();
                    }
                });
            });

            MainWindowViewModel.Connection.On<List<JsonElement>, List<JsonElement>, List<string>>("SendChats", (chats, lastMessages, readStatuses) =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _ChatItems.Clear();

                    for (int i = 0; i < chats.Count; i++)
                    {
                        if (chats[i].TryGetProperty("surname", out JsonElement surname))
                        {
                            _ChatItems.Insert(0, new ChatModel(
                                chats[i].GetProperty("id").ToString(),
                                chats[i].GetProperty("hasPhoto").GetBoolean(),
                                surname.ToString(),
                                chats[i].GetProperty("name").ToString(),
                                chats[i].GetProperty("patronymic").ToString(),
                                chats[i].GetProperty("position").ToString(),
                                chats[i].GetProperty("information").ToString(),
                                chats[i].GetProperty("hasAccess").GetBoolean(),
                                (NetworkStatus)chats[i].GetProperty("networkStatus").GetInt32(),
                                lastMessages[i].GetProperty("id").GetInt64(),
                                lastMessages[i].GetProperty("text").ToString(),
                                lastMessages[i].GetProperty("sendDateTime").TryGetDateTime(out DateTime dateTime) ? dateTime : null,
                                readStatuses[i])
                            );
                        }
                        else
                        {
                            long messageId = lastMessages[i].GetProperty("id").GetInt64();

                            ChatItems.Insert(0, new ChatModel(
                                chats[i].GetProperty("id").ToString(),
                                chats[i].GetProperty("hasPhoto").GetBoolean(),
                                chats[i].GetProperty("name").ToString(),
                                chats[i].GetProperty("authorId").ToString(),
                                messageId,
                                lastMessages[i].GetProperty("text").ToString(),
                                messageId == -1 ? null : (lastMessages[i].GetProperty("sendDateTime").TryGetDateTime(out DateTime dateTime) ? dateTime : null),
                                readStatuses[i])
                            );
                        }
                    }

                    if (FoundChat != null)
                    {
                        var chatItem = ChatItems.FirstOrDefault(ci => ci.ChatId == FoundChat.ChatId);
                        if (chatItem != null)
                        {
                            ChatItems.Move(ChatItems.IndexOf(chatItem), 0);
                            SelectedChat = chatItem;
                        }
                        else
                        {
                            ChatItems.Insert(0, FoundChat);
                            SelectedChat = FoundChat;
                        }

                        FoundChat = null;
                    }
                });
            });

            MainWindowViewModel.Connection.On<string, string>("SendUpdatedUserInformation", (userId, information) =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ChatItems.First(ci => ci.ChatId == userId).Information = information;
                });
            });

            MainWindowViewModel.Connection.On<string, string, string, string, string>("SendUpdatedUserFullNamePosition", (userId, surname, name, patronymic, position) =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    var user = ChatItems.FirstOrDefault(ci => ci.ChatId == userId);
                    if (user != null)
                    {
                        if (!string.IsNullOrWhiteSpace(surname))
                        {
                            user.Surname = surname;
                        }

                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            user.Name = name;
                        }

                        if (!string.IsNullOrWhiteSpace(patronymic))
                        {
                            user.Patronymic = patronymic;
                        }

                        if (!string.IsNullOrWhiteSpace(position))
                        {
                            user.Position = position;
                        }
                    }
                });
            });

            MainWindowViewModel.Connection.On<List<JsonElement>>("SendAttachmentsWithIds", (attachments) =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    foreach (var item in attachments)
                    {
                        MessageAttachmentType type = (MessageAttachmentType)item.GetProperty("type").GetInt32();

                        switch (type)
                        {
                            case MessageAttachmentType.File:
                                Attachments.Add(new AttachmentModel(
                                    item.GetProperty("id").GetInt64(),
                                    item.GetProperty("additionalData").ToString(),
                                    type,
                                    item.GetProperty("data").ToString())
                                );
                                break;
                            case MessageAttachmentType.Message:
                                Attachments.Add(new AttachmentModel(
                                    item.GetProperty("id").GetInt64(),
                                    item.GetProperty("data").GetProperty("hasPhoto").GetBoolean()
                                        ? $"{ServerSettings.ServerAddress}/UserFiles/Avatars/{item.GetProperty("data").GetProperty("authorId")}.png"
                                        : "/Assets/user.png",
                                    item.GetProperty("data").GetProperty("fullName").ToString(),
                                    item.GetProperty("data").GetProperty("sendDateTime").ToString(),
                                    item.GetProperty("data").GetProperty("text").ToString(),
                                    type,
                                    item.GetProperty("data").GetProperty("messageId").ToString())
                                );
                                break;
                        }
                    }
                });
            });

            MainWindowViewModel.Connection.On<int>("EditDeleteTimeLimitChanged", (editDeleteTimeLimit) =>
            {
                ServerSettings.EditDeleteTimeLimit = TimeSpan.FromMinutes(editDeleteTimeLimit);
            });

            MainWindowViewModel.Connection.On<string, NetworkStatus>("UserNetworkStatusChanged", (userId, networkStatus) =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ChatItems.FirstOrDefault(ci => ci.ChatId == userId)?.NetworkStatusIconSet(networkStatus);
                });
            });
        }

        public ChatModel? FoundChat { get; set; } = null;

        private ObservableCollection<ChatModel> _ChatItems;

        public ObservableCollection<ChatModel> ChatItems
        {
            get => _ChatItems;
            set => this.RaiseAndSetIfChanged(ref _ChatItems, value);
        }
        
        private ChatModel? _SelectedChat;

        public ChatModel? SelectedChat
        {
            get => _SelectedChat;
            set => this.RaiseAndSetIfChanged(ref _SelectedChat, value);
        }

        private ObservableCollection<MessageModel> _Messages;

        public ObservableCollection<MessageModel> Messages
        {
            get => _Messages;
            set => this.RaiseAndSetIfChanged(ref _Messages, value);
        }

        private ObservableCollection<MessageModel> _SelectedMessages;

        public ObservableCollection<MessageModel> SelectedMessages
        {
            get => _SelectedMessages;
            set => this.RaiseAndSetIfChanged(ref _SelectedMessages, value);
        }

        private MessageModel? _MessageToEdit;

        public MessageModel? MessageToEdit
        {
            get => _MessageToEdit;
            set => this.RaiseAndSetIfChanged(ref _MessageToEdit, value);
        }

        private bool _ForwardActive;

        public bool ForwardActive
        {
            get => _ForwardActive;
            set => this.RaiseAndSetIfChanged(ref _ForwardActive, value);
        }

        public MainWindowViewModel? Parent { get; set; }

        private string _SearchWatermark;

        public string SearchWatermark
        {
            get => _SearchWatermark;
            private set => this.RaiseAndSetIfChanged(ref _SearchWatermark, value);
        }

        private async void OnChatSelectionChange()
        {
            IsMessagesDPVisible = _SelectedChat != null;
            MessageToEdit = null;

            if (SelectedChat != null)
            {
                if (!ForwardActive)
                {
                    Attachments.Clear();
                }
                else
                {
                    ForwardActive = false;
                }

                SearchWatermark = SelectedChat.IsGroupChat ? "Поиск по чату" : "Поиск по диалогу";

                Messages.Clear();

                try
                {
                    await MainWindowViewModel.Connection.InvokeAsync("GetMessagesFromChat", SelectedChat.ChatId, 0, SelectedChat.IsGroupChat);
                }
                catch
                {
                    ConnectionLost();
                }
            }
            else
            {
                SearchWatermark = "Поиск";
            }
        }

        public int LoadCount { get; set; }

        private bool _IsMessagesDPVisible;

        public bool IsMessagesDPVisible
        {
            get => _IsMessagesDPVisible;
            protected set => this.RaiseAndSetIfChanged(ref _IsMessagesDPVisible, value);
        }

        private bool _IsMessagesLBVisible;

        public bool IsMessagesLBVisible
        {
            get => _IsMessagesLBVisible;
            protected set => this.RaiseAndSetIfChanged(ref _IsMessagesLBVisible, value);
        }

        private void UpdateMessagesLBVisible()
        {
            IsMessagesLBVisible = Messages != null && Messages.Count > 0;
        }

        private bool _ChatsVisible;

        public bool ChatsVisible
        {
            get => _ChatsVisible;
            protected set => this.RaiseAndSetIfChanged(ref _ChatsVisible, value);
        }

        private string _ChatsAreEmptyMessage;

        public string ChatsAreEmptyMessage
        {
            get => _ChatsAreEmptyMessage;
            set => this.RaiseAndSetIfChanged(ref _ChatsAreEmptyMessage, value);
        }

        private void UpdateChatsVisible()
        {
            ChatsVisible = ChatItems != null && ChatItems.Count > 0;
        }

        private ObservableCollection<AttachmentModel> _Attachments;

        public ObservableCollection<AttachmentModel> Attachments
        {
            get => _Attachments;
            set => this.RaiseAndSetIfChanged(ref _Attachments, value);
        }

        private ObservableCollection<AttachmentModel> _SelectedAttachments;

        public ObservableCollection<AttachmentModel> SelectedAttachments
        {
            get => _SelectedAttachments;
            set => this.RaiseAndSetIfChanged(ref _SelectedAttachments, value);
        }

        private bool _IsThereAttachments;

        public bool IsThereAttachments
        {
            get => _IsThereAttachments;
            set => this.RaiseAndSetIfChanged(ref _IsThereAttachments, value);
        }

        private void UpdateIsThereAttachments()
        {
            IsThereAttachments = Attachments.Count > 0;
        }

        private string _RemoveAllAttachmentsBText;

        public string RemoveAllAttachmentsBText
        {
            get => _RemoveAllAttachmentsBText;
            set => this.RaiseAndSetIfChanged(ref _RemoveAllAttachmentsBText, value);
        }

        private void UpdateRemoveAllAttachmentsBText()
        {
            RemoveAllAttachmentsBText = SelectedAttachments.Count > 0 ? "Открепить выбранные" : "Открепить все";
        }

        private void ConnectionLost()
        {
            ServerSettings.ConnectionLost = true;
            Parent!.NavigatePreviousCommand.Execute(null);
        }

        public ICommand NewChatOpenDialogCommand { get; }

        public Interaction<NewChatWindowViewModel, string?> ShowNewChatDialog { get; }

        public ICommand GroupChatEditOpenDialogCommand { get; }

        public Interaction<GroupChatEditWindowViewModel, string?> ShowGroupChatEditDialog { get; }

        public ICommand ShareOpenDialogCommand { get; }

        public Interaction<ShareWindowViewModel, List<string>?> ShowShareDialog { get; }
    }
}
