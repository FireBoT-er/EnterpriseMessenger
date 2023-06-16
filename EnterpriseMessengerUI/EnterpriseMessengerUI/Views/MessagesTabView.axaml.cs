using AsyncImageLoader;
using Avalonia.Controls;
using Avalonia.Controls.Generators;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using EnterpriseMessengerUI.Converters;
using EnterpriseMessengerUI.Models;
using EnterpriseMessengerUI.ViewModels;
using MessageBox.Avalonia;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Models;
using Microsoft.AspNetCore.SignalR.Client;
using ReactiveUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EnterpriseMessengerUI.Views
{
    public partial class MessagesTabView : ReactiveUserControl<MessagesTabViewModel>
    {
        public MessagesTabView()
        {
            InitializeComponent();

            Loaded += MessagesTabView_Loaded;
            Unloaded += MessagesTabView_Unloaded;

            messageTB.AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
            messagesLB.ItemContainerGenerator.Materialized += MessagesLBItemContainerGenerator_Materialized;
            realSearchTB.TextChanged += RealSearchTB_TextChanged;
            chatsLB.SelectionChanged += ChatsLB_SelectionChanged;

            messageTB.TextChanged += MessageTB_TextChanged;
            messageTB.AddHandler(DragDrop.DragEnterEvent, MessageTB_DragEnter);
            messageTB.AddHandler(DragDrop.DragLeaveEvent, MessageTB_DragLeave);
            messageTB.AddHandler(DragDrop.DropEvent, MessageTB_Drop);

            this.WhenActivated(d => d(ViewModel!.ShowNewChatDialog.RegisterHandler(DoShowNewChatDialogAsync)));
            this.WhenActivated(d => d(ViewModel!.ShowGroupChatEditDialog.RegisterHandler(DoShowGroupChatEditDialogAsync)));
            this.WhenActivated(d => d(ViewModel!.ShowShareDialog.RegisterHandler(DoShowShareDialogAsync)));
        }

        private async void MessagesTabView_Loaded(object? sender, RoutedEventArgs e)
        {
            ViewModel!.Parent = (MainWindowViewModel)this.Parent!.Parent!.Parent!.Parent!.Parent!.DataContext!;
            Thread.Sleep(50);

            try
            {
                await MainWindowViewModel.Connection.InvokeAsync("GetChats");
            }
            catch
            {
                ConnectionLost();
                return;
            }
            
            AddConnectionHandlers();
        }

        private void AddConnectionHandlers()
        {
            MainWindowViewModel.Connection.On<JsonElement, bool, JsonElement>("SendMessage", (message, hasAttachments, author) =>
            {
                Dispatcher.UIThread.InvokeAsync(async() =>
                {
                    var authorId = message.GetProperty("authorId").ToString();
                    string chatId = message.GetProperty("receiverChatId").ToString();

                    string text = message.GetProperty("text").ToString();
                    text = string.IsNullOrEmpty(text) ? "[Вложения]" : text;

                    if (ViewModel != null)
                    {
                        var receiverChat = string.IsNullOrWhiteSpace(chatId)
                            ? ViewModel.ChatItems.FirstOrDefault(ci => ci.ChatId == authorId || ci.ChatId == message.GetProperty("receiverUserId").ToString())
                            : ViewModel.ChatItems.FirstOrDefault(ci => ci.ChatId == chatId);

                        if (receiverChat == null)
                        {
                            try
                            {
                                var tcs = new TaskCompletionSource<string>();
                                await MainWindowViewModel.Connection.InvokeAsync("GetNewChat", authorId).ContinueWith(task => { tcs.SetResult(task.Status.ToString()); });
                                await tcs.Task;
                            }
                            catch
                            {
                                ConnectionLost();
                                return;
                            }

                            receiverChat = ViewModel.ChatItems[0];
                        }
                        else
                        {
                            ViewModel.ChatItems.Move(ViewModel.ChatItems.IndexOf(receiverChat!), 0);
                        }

                        receiverChat!.LastMessageId = message.GetProperty("id").GetInt64();
                        receiverChat!.LastMessageText = authorId == CurrentUserModel.Id ? $"Вы: {text}" : text;
                        receiverChat!.LastMessageSendDateTimeFromDateTime(message.GetProperty("sendDateTime").GetDateTime());

                        if (ViewModel.SelectedChat != null)
                        {
                            if (ViewModel.SelectedChat == receiverChat!)
                            {
                                chatsLB.GetLogicalChildren().OfType<ListBoxItem>().ElementAt(0).IsSelected = true;

                                ViewModel.Messages.Add(new MessageModel(
                                    message.GetProperty("id").GetInt64(),
                                    message.GetProperty("text").ToString(),
                                    message.GetProperty("sendDateTime").GetDateTime().ToString("G"),
                                    null,
                                    authorId == CurrentUserModel.Id ? MessageModel.HorizontalAlignmentRight : MessageModel.HorizontalAlignmentLeft,
                                    authorId != CurrentUserModel.Id || message.GetProperty("hasRead").GetBoolean(),
                                    hasAttachments,
                                    ViewModel.SelectedChat.IsGroupChat ? author.GetProperty("id").ToString() : string.Empty,
                                    ViewModel.SelectedChat.IsGroupChat && author.GetProperty("hasPhoto").GetBoolean(),
                                    ViewModel.SelectedChat.IsGroupChat
                                        ? $"{author.GetProperty("surname")} {author.GetProperty("name").ToString()[0]}. {author.GetProperty("patronymic").ToString()[0]}."
                                        : string.Empty)
                                );

                                await Dispatcher.UIThread.InvokeAsync(() => messagesLB.ScrollIntoView(ViewModel.Messages[ViewModel.Messages.Count - 1]), DispatcherPriority.Loaded);

                                if (authorId != CurrentUserModel.Id)
                                {
                                    try
                                    {
                                        await MainWindowViewModel.Connection.InvokeAsync("ReadMessage", message.GetProperty("id").GetInt64());
                                    }
                                    catch
                                    {
                                        ConnectionLost();
                                        return;
                                    }
                                }
                            }
                            else
                            {
                                if (ViewModel.ChatItems.IndexOf(ViewModel.SelectedChat) == 1)
                                {
                                    chatsLB.GetLogicalChildren().OfType<ListBoxItem>().ElementAt(1).IsSelected = true;
                                }
                            }
                        }

                        if (authorId == CurrentUserModel.Id)
                        {
                            receiverChat!.ReadStatus = message.GetProperty("hasRead").GetBoolean() ? string.Empty : " ";
                        }
                        else
                        {
                            if (ViewModel.SelectedChat == receiverChat!)
                            {
                                receiverChat!.ReadStatus = string.Empty;
                            }
                            else
                            {
                                if (string.IsNullOrWhiteSpace(receiverChat!.ReadStatus))
                                {
                                    receiverChat!.ReadStatus = "1";
                                }
                                else
                                {
                                    int readStatus = Convert.ToInt32(receiverChat!.ReadStatus);
                                    receiverChat!.ReadStatus = readStatus > 99 ? "99" : (++readStatus).ToString();
                                }
                            }
                        }
                    }

                    if (authorId != CurrentUserModel.Id)
                    {
                        Notification.Show(
                            author.GetProperty("hasPhoto").GetBoolean() ? $"{ServerSettings.ServerAddress}/UserFiles/Avatars/{authorId}.png" : "/Assets/user.png",
                            author.GetProperty("surname").ToString(),
                            author.GetProperty("name").ToString(),
                            author.GetProperty("patronymic").ToString(),
                            string.IsNullOrWhiteSpace(chatId) ? text : $"В \"{author.GetProperty("chatName")}\": {text}"
                        );
                    }
                });
            });

            MainWindowViewModel.Connection.On<List<JsonElement>, List<bool>, int, List<JsonElement>>("SendMessages", (messages, hasAttachments, unreadedMessagesCount, authors) =>
            {
                Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    if (ViewModel != null)
                    {
                        if (messageTB.Text != null)
                        {
                            messageTB.Clear();
                        }

                        HasAccessChanged(ViewModel.SelectedChat!.HasAccess);

                        var visibleMessages = messagesLB.GetLogicalDescendants().OfType<ListBoxItem>();

                        MessageModel? lastItem = null;
                        if (visibleMessages.Any())
                        {
                            try
                            {
                                lastItem = ViewModel.Messages[ViewModel.Messages.IndexOf(ViewModel.Messages.Where(m => m.SendDateTime == visibleMessages.OrderByDescending(lbi => lbi.Bounds.Y).First().GetLogicalDescendants().OfType<TextBlock>().ElementAt(2).Text).First()) - 1];
                            }
                            catch
                            {
                                lastItem = ViewModel.Messages[ViewModel.Messages.IndexOf(ViewModel.Messages.Where(m => m.SendDateTime == visibleMessages.OrderByDescending(lbi => lbi.Bounds.Y).First().GetLogicalDescendants().OfType<TextBlock>().ElementAt(3).Text).First()) - 1];
                            }
                        }

                        int messagesPrevCount = ViewModel.Messages.Count;

                        for (int i = 0; i < messages.Count; i++)
                        {
                            string editDateTimeString = messages[i].GetProperty("editDateTime").ToString();

                            ViewModel.Messages.Insert(0, new MessageModel(
                                messages[i].GetProperty("id").GetInt64(),
                                messages[i].GetProperty("text").ToString(),
                                messages[i].GetProperty("sendDateTime").GetDateTime().ToString("G"),
                                editDateTimeString != string.Empty ? DateTime.Parse(editDateTimeString) : null,
                                messages[i].GetProperty("authorId").ToString() == CurrentUserModel.Id ? MessageModel.HorizontalAlignmentRight : MessageModel.HorizontalAlignmentLeft,
                                messages[i].GetProperty("hasRead").GetBoolean(),
                                hasAttachments[i],
                                ViewModel.SelectedChat.IsGroupChat ? authors[i].GetProperty("id").ToString() : string.Empty,
                                ViewModel.SelectedChat.IsGroupChat && authors[i].GetProperty("hasPhoto").GetBoolean(),
                                ViewModel.SelectedChat.IsGroupChat
                                    ? $"{authors[i].GetProperty("surname")} {authors[i].GetProperty("name")}. {authors[i].GetProperty("patronymic")}."
                                    : string.Empty)
                            );
                        }

                        if (ViewModel.FoundChat != null && ViewModel.FoundChat.LastMessageText != string.Empty && ViewModel.LoadCount == 0)
                        {
                            MessageModel? foundMessage = ViewModel.Messages.FirstOrDefault(m => m.Id == ViewModel.FoundChat.LastMessageId);
                            if (foundMessage != null)
                            {
                                ViewModel.SelectedMessages.Add(foundMessage);
                                ViewModel.FoundChat = null;
                            }
                            else
                            {
                                try
                                {
                                    await MainWindowViewModel.Connection.InvokeAsync("GetMessagesSearchLoadCount", ViewModel.FoundChat.LastMessageId);
                                }
                                catch
                                {
                                    ConnectionLost();
                                    return;
                                }
                            }
                        }
                        else
                        {
                            if (unreadedMessagesCount > 0)
                            {
                                await Dispatcher.UIThread.InvokeAsync(() => messagesLB.ScrollIntoView(ViewModel.Messages[ViewModel.Messages.Count - unreadedMessagesCount - 1]), DispatcherPriority.Loaded);
                            }
                            else if (messagesPrevCount == 0 && ViewModel.Messages.Count > 0)
                            {
                                await Dispatcher.UIThread.InvokeAsync(() => messagesLB.ScrollIntoView(ViewModel.Messages[ViewModel.Messages.Count - 1]), DispatcherPriority.Loaded);
                            }
                            else if (messages.Count > 0)
                            {
                                await Dispatcher.UIThread.InvokeAsync(() => messagesLB.ScrollIntoView(lastItem!), DispatcherPriority.Loaded);
                            }
                        }

                        if (ViewModel.LoadCount > 0)
                        {
                            await ShowSearchResult();
                        }

                        if (!string.IsNullOrWhiteSpace(ViewModel.SelectedChat!.ReadStatus))
                        {
                            ViewModel.SelectedChat!.ReadStatus = string.Empty;
                        }
                    }
                });
            });

            MainWindowViewModel.Connection.On<string>("MessageHasBeenRead", (id) =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (ViewModel != null)
                    {
                        var receiverChat = ViewModel.ChatItems.First(ci => ci.ChatId == id);
                        receiverChat.ReadStatus = string.Empty;

                        if (ViewModel.SelectedChat == receiverChat)
                        {
                            for (int i = ViewModel.Messages.Count - 1; i >= 0; i--)
                            {
                                if (!ViewModel.Messages[i].HasRead)
                                {
                                    ViewModel.Messages[i].HasRead = true;
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                    }
                });
            });

            MainWindowViewModel.Connection.On<long, string, DateTime, bool>("SendEditedMessage", (id, text, editDateTime, hasAttachments) =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (ViewModel != null)
                    {
                        if (ViewModel.SelectedChat != null)
                        {
                            var message = ViewModel.Messages.FirstOrDefault(m => m.Id == id);

                            if (message != null)
                            {
                                message.Text = text;
                                message.SetEdited(editDateTime);
                                message.IsAttachmentsVisible = hasAttachments;

                                if (ViewModel.Messages[ViewModel.Messages.Count - 1] == message)
                                {
                                    text = string.IsNullOrEmpty(text) ? "[Вложения]" : text;
                                    ViewModel.SelectedChat.LastMessageText = message.HorizontalAlignment == MessageModel.HorizontalAlignmentLeft ? text : "Вы: " + text;
                                }
                            }
                        }
                        else
                        {
                            ChatModel? chat = ViewModel.ChatItems.FirstOrDefault(ci => ci.LastMessageId == id);
                            if (chat != null)
                            {
                                chat.LastMessageText = text;
                            }
                        }
                    }
                });
            });

            MainWindowViewModel.Connection.On<long, JsonElement>("MessageHasBeenDeleted", (id, previousMessage) =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (ViewModel != null)
                    {
                        if (ViewModel.SelectedChat != null)
                        {
                            var message = ViewModel.Messages.FirstOrDefault(m => m.Id == id);

                            if (message != null)
                            {
                                if (ViewModel.Messages.Count > 1)
                                {
                                    if (ViewModel.Messages[ViewModel.Messages.Count - 1] == message)
                                    {
                                        SetLastMessageAfterDelete(previousMessage, ViewModel.SelectedChat);
                                    }
                                }
                                else
                                {
                                    SetLastMessageAfterDelete(previousMessage, ViewModel.SelectedChat);
                                }

                                ViewModel.Messages.Remove(message);
                            }
                        }
                        else
                        {
                            ChatModel? chat = ViewModel.ChatItems.FirstOrDefault(ci => ci.LastMessageId == id);
                            if (chat != null)
                            {
                                SetLastMessageAfterDelete(previousMessage, chat);
                            }
                        }
                    }
                });
            });

            MainWindowViewModel.Connection.On<int>("SendMessagesSearchLoadCount", (count) =>
            {
                Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    if (ViewModel != null)
                    {
                        while (count > 50)
                        {
                            ViewModel.LoadCount++;
                            count -= 50;
                        }

                        await ShowSearchResult();
                    }
                });
            });

            MainWindowViewModel.Connection.On<string>("SendUserUpdatedPhoto", (userId) =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (ViewModel != null)
                    {
                        var userChatItem = ViewModel.ChatItems.FirstOrDefault(ci => ci.ChatId == userId);

                        if (userChatItem != null)
                        {
                            userChatItem.PhotoFileNameSet(false);
                            ((UpdatableDiskCachedWebImageLoader)ImageLoader.AsyncImageLoader).Reload = true;
                            userChatItem.PhotoFileNameSet(true);

                            ShowImage(userChatItem);
                        }
                    }
                });
            });

            MainWindowViewModel.Connection.On<string>("SendUserDeletedPhoto", (userId) =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (ViewModel != null)
                    {
                        var userChatItem = ViewModel.ChatItems.FirstOrDefault(ci => ci.ChatId == userId);

                        if (userChatItem != null)
                        {
                            ((UpdatableDiskCachedWebImageLoader)ImageLoader.AsyncImageLoader).Reload = true;
                            userChatItem.PhotoFileNameSet(false);

                            ShowImage(userChatItem);
                        }
                    }
                });
            });

            MainWindowViewModel.Connection.On<string>("SendGroupChatUpdatedPhoto", (chatId) =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (ViewModel != null)
                    {
                        var сhatItem = ViewModel.ChatItems.FirstOrDefault(ci => ci.ChatId == chatId);

                        if (сhatItem != null)
                        {
                            сhatItem.PhotoFileNameSet(false);
                            ((UpdatableDiskCachedWebImageLoader)ImageLoader.AsyncImageLoader).Reload = true;
                            сhatItem.PhotoFileNameSet(true);

                            ShowImage(сhatItem);
                        }
                    }
                });
            });

            MainWindowViewModel.Connection.On<string>("SendGroupChatDeletedPhoto", (chatId) =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (ViewModel != null)
                    {
                        var chatItem = ViewModel.ChatItems.FirstOrDefault(ci => ci.ChatId == chatId);

                        if (chatItem != null)
                        {
                            ((UpdatableDiskCachedWebImageLoader)ImageLoader.AsyncImageLoader).Reload = true;
                            chatItem.PhotoFileNameSet(false);

                            ShowImage(chatItem);
                        }
                    }
                });
            });

            MainWindowViewModel.Connection.On("InvalidOperationAttachmentIsUploadingEdit", () =>
            {
                Dispatcher.UIThread.InvokeAsync(async() =>
                {
                    await MessageBoxManager.GetMessageBoxStandardWindow(
                        new MessageBoxStandardParams
                        {
                            ContentTitle = "Изменение сообщения",
                            ContentMessage = "Ошибка: нельзя открепить вложение, если оно не загрузилось на сервер или скачивается в данный момент",
                            WindowIcon = ((MainWindow)this.Parent!.Parent!.Parent!.Parent!.Parent!).Icon,
                            WindowStartupLocation = WindowStartupLocation.CenterOwner
                        }).ShowDialog((MainWindow)this.Parent!.Parent!.Parent!.Parent!.Parent!);
                });
            });

            MainWindowViewModel.Connection.On("InvalidOperationAttachmentIsUploadingDelete", () =>
            {
                Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await MessageBoxManager.GetMessageBoxStandardWindow(
                        new MessageBoxStandardParams
                        {
                            ContentTitle = "Удаление сообщения",
                            ContentMessage = "Ошибка: нельзя удалить сообщение, если его вложение не загрузилось на сервер или скачивается в данный момент",
                            WindowIcon = ((MainWindow)this.Parent!.Parent!.Parent!.Parent!.Parent!).Icon,
                            WindowStartupLocation = WindowStartupLocation.CenterOwner
                        }).ShowDialog((MainWindow)this.Parent!.Parent!.Parent!.Parent!.Parent!);
                });
            });

            MainWindowViewModel.Connection.On<string, bool>("SendUpdatedUserHasAccess", (userId, hasAccess) =>
            {
                Dispatcher.UIThread.InvokeAsync(async() =>
                {
                    if (ViewModel != null)
                    {
                        var user = ViewModel.ChatItems.FirstOrDefault(ci => ci.ChatId == userId);

                        if (user != null)
                        {
                            user.HasAccess = hasAccess;

                            if (ViewModel.SelectedChat == user)
                            {
                                HasAccessChanged(hasAccess);
                            }
                        }
                        else if (userId == CurrentUserModel.Id)
                        {
                            await MessageBoxManager.GetMessageBoxStandardWindow(
                                new MessageBoxStandardParams
                                {
                                    ContentTitle = "Ограничение доступа",
                                    ContentMessage = "Вам ограничен доступ к системе",
                                    WindowIcon = ((MainWindow)this.Parent!.Parent!.Parent!.Parent!.Parent!).Icon,
                                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                                }).ShowDialog((MainWindow)this.Parent!.Parent!.Parent!.Parent!.Parent!);

                            ((MainWindowViewModel)this.Parent!.Parent!.Parent!.Parent!.Parent!.DataContext!).NavigatePreviousCommand.Execute(null);

                            try { await MainWindowViewModel.Connection.StopAsync(); }
                            finally { await MainWindowViewModel.Connection.DisposeAsync(); }
                        }
                    }
                });
            });
        }

        private void SetLastMessageAfterDelete(JsonElement previousMessage, ChatModel chat)
        {
            if (previousMessage.ValueKind != JsonValueKind.Null)
            {
                string text = previousMessage.GetProperty("text").ToString();
                text = string.IsNullOrEmpty(text) ? "[Вложения]" : text;

                string authorId = previousMessage.GetProperty("authorId").ToString();

                chat.LastMessageId = previousMessage.GetProperty("id").GetInt64();
                chat.LastMessageText = authorId == CurrentUserModel.Id ? "Вы: " + text : text;
                chat.LastMessageSendDateTimeFromDateTime(previousMessage.GetProperty("sendDateTime").GetDateTime());

                if (chat.ChatId != authorId)
                {
                    chat.ReadStatus = previousMessage.GetProperty("hasRead").GetBoolean() ? string.Empty : " ";
                }
                else
                {
                    if (ViewModel!.SelectedChat == chat)
                    {
                        chat.ReadStatus = string.Empty;
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(chat.ReadStatus))
                        {
                            int readStatus = Convert.ToInt32(chat.ReadStatus);
                            chat.ReadStatus = readStatus > 99 ? "99" : (--readStatus > 0 ? readStatus.ToString() : string.Empty);
                        }
                    }
                }
            }
            else
            {
                chat.LastMessageId = -1;
                chat.LastMessageText = string.Empty;
                chat.LastMessageSendDateTimeFromDateTime(null);
                chat.ReadStatus = string.Empty;
            }
        }

        private async Task ShowSearchResult()
        {
            try
            {
                await MainWindowViewModel.Connection.InvokeAsync("GetMessagesFromChat", ViewModel!.SelectedChat!.ChatId, ((ObservableCollection<MessageModel>)messagesLB.Items!).Count, ViewModel!.SelectedChat!.IsGroupChat);
            }
            catch
            {
                ConnectionLost();
                return;
            }

            ViewModel.LoadCount--;
        }

        private void ShowImage(ChatModel userChatItem)
        {
            var userChatItemContainer = chatsLB.ItemContainerGenerator.ContainerFromIndex(ViewModel!.ChatItems.IndexOf(userChatItem));
            var avatarI = userChatItemContainer!.GetLogicalDescendants().OfType<Border>().ElementAt(0);
            var binding = new Binding { DefaultAnchor = new WeakReference(avatarI) };
            new AsyncToDefaultConverter().Convert(string.Empty, typeof(IBitmap), binding, System.Globalization.CultureInfo.CurrentCulture);
        }

        private void HasAccessChanged(bool hasAccess)
        {
            if (!hasAccess)
            {
                attachmentsB.IsEnabled = false;
                messageTB.IsEnabled = false;
                messageTB.Watermark = "Данному пользователю нельзя отправить сообщение";
                sendB.IsEnabled = false;
            }
            else
            {
                attachmentsB.IsEnabled = true;
                messageTB.IsEnabled = true;
                messageTB.Watermark = "Введите сообщение...";
                sendB.IsEnabled = true;
            }
        }

        private void MessagesTabView_Unloaded(object? sender, RoutedEventArgs e)
        {
            ((MessagesTabViewModel)DataContext!).SelectedChat = null;
        }

        private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && e.KeyModifiers != KeyModifiers.Shift)
            {
                OnSendBClick(sender, e);
                e.Handled = true;
            }
        }

        private void MessageTB_TextChanged(object? sender, TextChangedEventArgs e)
        {
            if (realSearchTB.Text != null)
            {
                ViewModel!.FoundChat = ViewModel!.SelectedChat!;
                realSearchTB.Clear();
            }
        }

        private void ChatsLB_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                if (!string.IsNullOrWhiteSpace(realSearchTB.Text))
                {
                    ChatModel FoundChat = (ChatModel)e.AddedItems[0]!;
                    ViewModel!.FoundChat = FoundChat;

                    if (FoundChat.LastMessageText == string.Empty)
                    {
                        realSearchTB.Clear();
                    }
                }
            }
        }

        private void MessagesLBItemContainerGenerator_Materialized(object? sender, ItemContainerEventArgs e)
        {
            messagesLB.GetVisualDescendants().OfType<ScrollViewer>().First().ScrollChanged += MessagesLB_ScrollChanged;
        }

        private Timer? SearchTimer;

        private void RealSearchTB_TextChanged(object? sender, TextChangedEventArgs e)
        {
            SearchTimer?.Dispose();

            string chatId = string.Empty;
            bool isGroupChat = false;
            if (ViewModel != null)
            {
                if (ViewModel.SelectedChat == null)
                {
                    ViewModel.ChatsAreEmptyMessage = "Диалоги, групповые чаты или сообщения не найдены";
                }
                else
                {
                    chatId = ViewModel.SelectedChat.ChatId;
                    isGroupChat = ViewModel.SelectedChat.IsGroupChat;

                    ViewModel.ChatsAreEmptyMessage = "Сообщения не найдены";
                }
            }

            SearchTimer = new Timer(async state =>
            {
                try
                {
                    await MainWindowViewModel.Connection.InvokeAsync("MessagesChatsSearch", ((TextBox)sender!).Text!.ToLower(), chatId, isGroupChat);
                }
                catch
                {
                    await Dispatcher.UIThread.InvokeAsync(ConnectionLost);
                    return;
                }
            }, null, 500, Timeout.Infinite);
        }

        private Timer? MessagesLoadTimer;

        private void MessagesLB_ScrollChanged(object? sender, ScrollChangedEventArgs e)
        {
            var scrollViewer = (ScrollViewer)sender!;
            int messagesCount = ((ObservableCollection<MessageModel>)messagesLB.Items!).Count;

            if (scrollViewer.Offset.Y <= 0 && e.OffsetDelta.Y < 0 && messagesCount > 0)
            {
                if (ViewModel != null && ViewModel.SelectedChat != null)
                {
                    MessagesLoadTimer?.Dispose();

                    string chatId = ViewModel.SelectedChat.ChatId;
                    bool isGroupChat = ViewModel.SelectedChat.IsGroupChat;
                    MessagesLoadTimer = new Timer(async state =>
                    {
                        try
                        {
                            await MainWindowViewModel.Connection.InvokeAsync("GetMessagesFromChat", chatId, messagesCount, isGroupChat);
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

        private void MessagesLBItem_ContextRequested(object? sender, ContextRequestedEventArgs e)
        {
            if (ViewModel != null)
            {
                ((MessageModel)((Border)sender!).DataContext!).SetCanEditDelete(ViewModel.SelectedMessages.Count);
            }
        }

        private void OnInfoBClick(object sender, RoutedEventArgs e)
        {
            MessageBoxManager.GetMessageBoxStandardWindow(
                new MessageBoxStandardParams
                {
                    ContentTitle = $"Информация о пользователе {ViewModel!.SelectedChat!.Surname} {ViewModel!.SelectedChat!.Name[0]}. {ViewModel!.SelectedChat!.Patronymic[0]}.:",
                    ContentMessage = ViewModel!.SelectedChat!.Information,
                    MaxWidth = ((MainWindow)this.Parent!.Parent!.Parent!.Parent!.Parent!).Width-15,
                    WindowIcon = ((MainWindow)this.Parent!.Parent!.Parent!.Parent!.Parent!).Icon,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                }).ShowDialog((MainWindow)this.Parent!.Parent!.Parent!.Parent!.Parent!);
        }

        private void OnShareChatBClick(object sender, RoutedEventArgs e)
        {
            ViewModel?.ShareOpenDialogCommand.Execute((false, ViewModel.SelectedChat!.ChatId, false));
        }

        private void OnChatParticipantsBClick(object sender, RoutedEventArgs e)
        {
            ViewModel?.ShareOpenDialogCommand.Execute((true, ViewModel.SelectedChat!.ChatId, ViewModel.SelectedChat!.IsGroupChatAuthor));
        }

        private async void OnLeaveChatBClick(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                var deleteMessage = MessageBoxManager.GetMessageBoxCustomWindow(new MessageBoxCustomParams
                {
                    ContentMessage = "Вы действительно хотите покинуть данный чат?\nДоступ ко всем сообщениям чата будет потерян",
                    ContentTitle = "Выход из чата",
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
                        await MainWindowViewModel.Connection.InvokeAsync("LeaveGroupChat", ViewModel.SelectedChat!.ChatId);
                        ViewModel.ChatItems.Remove(ViewModel.SelectedChat!);
                    }
                    catch
                    {
                        ConnectionLost();
                    }
                }
            }
        }

        public void Respond()
        {
            if (ViewModel != null)
            {
                List<AttachmentModel> attachmentsCopy = new(ViewModel.Attachments);

                foreach (var message in (IEnumerable<MessageModel>)(ViewModel.MessageToEdit == null ? ViewModel.SelectedMessages.OrderBy(sm => sm.SendDateTime) : ViewModel.SelectedMessages))
                {
                    if (!attachmentsCopy.Any(a => a.Data == message.Id.ToString() && a.Type == MessageAttachmentType.Message))
                    {
                        ViewModel.Attachments.Add(new AttachmentModel(
                            message.HorizontalAlignment == MessageModel.HorizontalAlignmentLeft
                                ? (ViewModel.SelectedChat!.IsGroupChat
                                    ? message.PhotoFileName
                                    : ViewModel.SelectedChat!.PhotoFileName)
                                : CurrentUserModel.PhotoFileName!,
                            message.HorizontalAlignment == MessageModel.HorizontalAlignmentLeft
                                ? (ViewModel.SelectedChat!.IsGroupChat
                                    ? message.AuthorName
                                    : $"{ViewModel.SelectedChat!.Surname} {ViewModel.SelectedChat!.Name[0]}. {ViewModel.SelectedChat!.Patronymic[0]}.")
                                : $"{CurrentUserModel.Surname!} {CurrentUserModel.Name![0]}. {CurrentUserModel.Patronymic![0]}.",
                            message.SendDateTime,
                            message.Text,
                            MessageAttachmentType.Message,
                            message.Id.ToString()));
                    }
                }
                
                ViewModel.SelectedMessages.Clear();
            }
        }

        public void Forward()
        {
            if (ViewModel != null)
            {
                Respond();
                ViewModel.ForwardActive = true;
                ViewModel.SelectedChat = null;
            }
        }

        public async void Edit()
        {
            if (ViewModel != null)
            {
                ViewModel.MessageToEdit = ViewModel.SelectedMessages[0];

                messageTB.Text = ViewModel.SelectedMessages[0].Text;
                messageTB.Focus();

                try
                {
                    await MainWindowViewModel.Connection.InvokeAsync("GetAttachmentsForMessage", ViewModel.MessageToEdit.Id, true);
                }
                catch
                {
                    ConnectionLost();
                }
            }
        }

        private void CancelEdit(object? sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.MessageToEdit = null;
                messageTB.Clear();
                ViewModel.Attachments.Clear();
            }
        }

        public async void Delete()
        {
            if (ViewModel != null)
            {
                var deleteMessage = MessageBoxManager.GetMessageBoxCustomWindow(new MessageBoxCustomParams
                {
                    ContentMessage = "Вы действительно хотите удалить данное сообщение?",
                    ContentTitle = "Удаление сообщения",
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
                    if (ViewModel.MessageToEdit == ViewModel.SelectedMessages[0])
                    {
                        ViewModel.MessageToEdit = null;
                        messageTB.Clear();
                        ViewModel.Attachments.Clear();
                    }

                    try
                    {
                        await MainWindowViewModel.Connection.InvokeAsync("DeleteMessage", ViewModel.SelectedMessages[0].Id, DateTime.Now);
                    }
                    catch
                    {
                        ConnectionLost();
                    }
                }
            }
        }

        private void MessageTB_DragEnter(object? sender, DragEventArgs e)
        {
            messageTB.Focus();
        }

        private void MessageTB_DragLeave(object? sender, DragEventArgs e)
        {
            this.Focus();
        }

        private void MessageTB_Drop(object? sender, DragEventArgs e)
        {
            if (ViewModel != null)
            {
                if (e.Data.GetDataFormats().Contains(DataFormats.FileNames))
                {
                    var fileNames = e.Data.GetFileNames();
                    if (fileNames != null)
                    {
                        List<AttachmentModel> attachmentsCopy = new(ViewModel.Attachments);
                        foreach (string fileName in fileNames!)
                        {
                            if (!attachmentsCopy.Any(a => a.Data == fileName && a.Type == MessageAttachmentType.File) && !File.GetAttributes(fileName).HasFlag(FileAttributes.Directory))
                            {
                                ViewModel.Attachments.Add(new AttachmentModel(
                                    fileName.Split(System.IO.Path.DirectorySeparatorChar).Last(),
                                    MessageAttachmentType.File,
                                    fileName));
                            }
                        }
                    }
                }
                else
                {
                    messageTB.Text += e.Data.GetText();
                }
            }
        }

        public async void ShowAttachments(long id)
        {
            var showAttachmentsWindow = new ShowAttachmentsWindow() { DataContext = new ShowAttachmentsWindowViewModel() };
            ((ShowAttachmentsWindowViewModel)showAttachmentsWindow.DataContext).MessageId = id;

            var tcs = new TaskCompletionSource<string>();
            await showAttachmentsWindow.ShowDialog((MainWindow)this.Parent!.Parent!.Parent!.Parent!.Parent!).ContinueWith(task => { tcs.SetResult(task.Status.ToString()); });
            await tcs.Task;

            if (ServerSettings.ConnectionLost)
            {
                ((MainWindowViewModel)this.Parent!.Parent!.Parent!.Parent!.Parent!.DataContext!).NavigatePreviousCommand.Execute(null);
            }
        }

        private async void OnAttachmentsBClick(object? sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                var result = await ((MainWindow)this.Parent!.Parent!.Parent!.Parent!.Parent!).StorageProvider.
                    OpenFilePickerAsync(new FilePickerOpenOptions
                    {
                        Title = "Выберите файл:",
                        AllowMultiple = true
                    });

                if (result != null && result.Count > 0)
                {
                    bool notOpened = false;
                    List<AttachmentModel> attachmentsCopy = new(ViewModel.Attachments);
                    foreach (var file in result)
                    {
                        if (file.TryGetUri(out Uri? path))
                        {
                            if (!attachmentsCopy.Any(a => a.Data == path.OriginalString && a.Type == MessageAttachmentType.File))
                            {
                                ViewModel.Attachments.Add(new AttachmentModel(
                                    path.OriginalString.Split(System.IO.Path.DirectorySeparatorChar).Last(),
                                    MessageAttachmentType.File,
                                    path.OriginalString));
                            }
                        }
                        else
                        {
                            notOpened = true;
                        }
                    }

                    if (notOpened)
                    {
                        await MessageBoxManager.GetMessageBoxStandardWindow(
                            new MessageBoxStandardParams
                            {
                                ContentTitle = "Выбор файла",
                                ContentMessage = "Ошибка при открытии одного или нескольких файлов, попробуйте ещё раз",
                                WindowIcon = ((MainWindow)this.Parent!.Parent!.Parent!.Parent!.Parent!).Icon,
                                WindowStartupLocation = WindowStartupLocation.CenterOwner
                            }).ShowDialog((MainWindow)this.Parent!.Parent!.Parent!.Parent!.Parent!);
                    }
                }
            }
        }

        private async void OnSendBClick(object? sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                if (!string.IsNullOrWhiteSpace(messageTB.Text) || ViewModel.Attachments.Count > 0)
                {
                    bool filesDisappeared = false;
                    List<AttachmentModel> files = new(ViewModel.Attachments.Where(a => a.Type == MessageAttachmentType.File && a.Id == null));
                    for (int i = 0; i < files.Count; i++)
                    {
                        if (!File.Exists(files[i].Data))
                        {
                            ViewModel.Attachments.Remove(files[i]);
                            files.Remove(files[i]);
                            i--;
                            filesDisappeared = true;
                        }
                    }

                    if (filesDisappeared)
                    {
                        await MessageBoxManager.GetMessageBoxStandardWindow(
                            new MessageBoxStandardParams
                            {
                                ContentTitle = "Отправка файлов",
                                ContentMessage = "Некоторые из файлов, которые вы пытались отправить, не найдены",
                                WindowIcon = ((MainWindow)this.Parent!.Parent!.Parent!.Parent!.Parent!).Icon,
                                WindowStartupLocation = WindowStartupLocation.CenterOwner
                            }).ShowDialog((MainWindow)this.Parent!.Parent!.Parent!.Parent!.Parent!);

                        if (ViewModel.Attachments.Count == 0 && string.IsNullOrWhiteSpace(messageTB.Text))
                        {
                            return;
                        }
                    }

                    try
                    {
                        if (ViewModel.MessageToEdit != null)
                        {
                            await MainWindowViewModel.Connection.InvokeAsync("EditMessage", ViewModel.MessageToEdit.Id, messageTB.Text, DateTime.Now,
                                ViewModel.Attachments.Select(a => new {
                                    Id = a.Id ?? 0,
                                    a.Type,
                                    Data = a.Id == null ? (a.Type != MessageAttachmentType.File ? a.Data : a.Guid.ToString()) : string.Empty,
                                    AdditionalData = a.Id == null ? (a.Type == MessageAttachmentType.File ? a.AdditionalData : null) : string.Empty}).ToList());

                            ViewModel.MessageToEdit = null;
                        }
                        else
                        {
                            await MainWindowViewModel.Connection.InvokeAsync("SendMessageToReceivers", messageTB.Text, ViewModel.SelectedChat!.ChatId, DateTime.Now, ViewModel.SelectedChat!.IsGroupChat,
                                ViewModel.Attachments.Select(a => new {
                                    a.Type,
                                    Data = a.Type != MessageAttachmentType.File ? a.Data : a.Guid.ToString(),
                                    AdditionalData = a.Type == MessageAttachmentType.File ? a.AdditionalData : null}).ToList());
                        }

                        foreach (var file in files)
                        {
                            ThreadPool.QueueUserWorkItem(SendFileToServer, new { file.Data, Guid = file.Guid.ToString() });
                        }
                    }
                    catch
                    {
                        ConnectionLost();
                        return;
                    }

                    ViewModel.Attachments.Clear();
                    messageTB.Clear();
                }
            }
        }

        private async void SendFileToServer(object? obj)
        {
            lock (((ICollection)MainWindowViewModel.FilesAndServer).SyncRoot)
            {
                MainWindowViewModel.FilesAndServer.Add((string)((dynamic)obj!).Guid);
            }

            using HttpClient client = new();
            client.DefaultRequestHeaders.Authorization = MainWindowViewModel.Client.DefaultRequestHeaders.Authorization;
            client.Timeout = Timeout.InfiniteTimeSpan;

            using FileStream fstream = File.OpenRead((string)((dynamic)obj!).Data);
            using StreamContent fileStreamContent = new(fstream);
            using MultipartFormDataContent content = new() { { fileStreamContent, "uploadedFile", (string)((dynamic)obj!).Guid } };

            HttpResponseMessage response;
            try
            {
                response = await client.PostAsync(ServerSettings.ServerAddress + "/api/files/sendFile", content);
            }
            catch
            {
                await Dispatcher.UIThread.InvokeAsync(ConnectionLost);

                lock (((ICollection)MainWindowViewModel.FilesAndServer).SyncRoot)
                {
                    MainWindowViewModel.FilesAndServer.Remove((string)((dynamic)obj!).Guid);
                }

                return;
            }
            
            if (response.StatusCode == HttpStatusCode.OK)
            {
                try
                {
                    await MainWindowViewModel.Connection.InvokeAsync("CanDownloadFile", (string)((dynamic)obj!).Guid);
                    Notification.Show(CurrentUserModel.PhotoFileName!, CurrentUserModel.Surname!, CurrentUserModel.Name!, CurrentUserModel.Patronymic!, "Загрузка завершена");
                }
                catch
                {
                    await Dispatcher.UIThread.InvokeAsync(ConnectionLost);
                }
            }
            else
            {
                await Dispatcher.UIThread.InvokeAsync(async() =>
                {
                    await MessageBoxManager.GetMessageBoxStandardWindow(
                        new MessageBoxStandardParams
                        {
                            ContentTitle = "Загрузка файла",
                            ContentMessage = "Файл не загружен; возможно, закончилось место на сервере. Попробуйте ещё раз позже",
                            WindowIcon = ((MainWindow)this.Parent!.Parent!.Parent!.Parent!.Parent!).Icon,
                            WindowStartupLocation = WindowStartupLocation.CenterOwner
                        }).ShowDialog((MainWindow)this.Parent!.Parent!.Parent!.Parent!.Parent!);
                });

                try
                {
                    await MainWindowViewModel.Connection.InvokeAsync("RemoveUnuploadedAttachment", (string)((dynamic)obj!).Guid);
                }
                catch
                {
                    await Dispatcher.UIThread.InvokeAsync(ConnectionLost);
                }
            }

            response.Dispose();

            lock (((ICollection)MainWindowViewModel.FilesAndServer).SyncRoot)
            {
                MainWindowViewModel.FilesAndServer.Remove((string)((dynamic)obj!).Guid);
            }
        }

        public void RemoveAttachment(AttachmentModel attachment)
        {
            ViewModel?.Attachments.Remove(attachment);
        }

        private void RemoveAllAttachments(object? sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                int count = ViewModel.SelectedAttachments.Count;
                if (count > 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        ViewModel.Attachments.Remove(ViewModel.SelectedAttachments[0]);
                    }
                }
                else
                {
                    ViewModel.Attachments.Clear();
                }
            }
        }

        private void ConnectionLost()
        {
            ServerSettings.ConnectionLost = true;
            ((MainWindowViewModel)this.Parent!.Parent!.Parent!.Parent!.Parent!.DataContext!).NavigatePreviousCommand.Execute(null);
        }

        private async Task DoShowNewChatDialogAsync(InteractionContext<NewChatWindowViewModel, string?> interaction)
        {
            NewChatWindow dialog = new() { DataContext = interaction.Input };

            var result = await dialog.ShowDialog<string?>((MainWindow)this.Parent!.Parent!.Parent!.Parent!.Parent!);
            interaction.SetOutput(result);
        }

        private async Task DoShowGroupChatEditDialogAsync(InteractionContext<GroupChatEditWindowViewModel, string?> interaction)
        {
            GroupChatEditWindow dialog = new() { DataContext = interaction.Input };

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
