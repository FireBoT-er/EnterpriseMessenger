using AsyncImageLoader;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using EnterpriseMessengerUI.ViewModels;
using MessageBox.Avalonia;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Models;
using Microsoft.AspNetCore.SignalR.Client;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace EnterpriseMessengerUI.Views
{
    public partial class GroupChatEditWindow : ReactiveWindow<GroupChatEditWindowViewModel>
    {
        public GroupChatEditWindow()
        {
            InitializeComponent();
            Loaded += GroupChatEditWindow_Loaded;
            AddConnectionHandlers();

            this.WhenAnyValue(x => x.invAvatarI.CurrentImage).Subscribe(_ => ShowImage());
        }

        private void AddConnectionHandlers()
        {
            MainWindowViewModel.Connection.On<string, bool, bool>("SendGroupChatInformation", (name, hasPhoto, canDelete) =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (ViewModel != null)
                    {
                        chatNameTB.Text = name;
                        ViewModel.CanDelete = canDelete;
                        ViewModel.PhotoFileName = hasPhoto ? $"{ServerSettings.ServerAddress}/UserFiles/GroupChatAvatars/{ViewModel.Id}.png" : "/Assets/group.png";
                    }
                });
            });
        }

        private async void GroupChatEditWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            try
            {
                await MainWindowViewModel.Connection.InvokeAsync("GetGroupChatInformation", ViewModel!.Id);
            }
            catch
            {
                ServerSettings.ConnectionLost = true;
                this.Close();
            }
        }

        private void ShowImage()
        {
            ((ImageBrush)avatarI.Background!).Source = (Bitmap)invAvatarI.CurrentImage!;
        }

        private async void ChangeAvatar(object? sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                if (avatarI.IsVisible)
                {
                    avatarI.IsVisible = false;
                    changeAvatarB.Content = "������� ����";
                    changeAvatarB.SetValue(ToolTip.TipProperty, "������ ����������� �� ������ ��������� 5 ��");
                    changeAvatarClearB.IsVisible = true;
                    changeAvatarCancelB.IsVisible = true;
                }
                else
                {
                    var result = await this.StorageProvider.
                        OpenFilePickerAsync(new FilePickerOpenOptions
                        {
                            Title = "�������� �����������:",
                            FileTypeFilter = new List<FilePickerFileType> { FilePickerFileTypes.ImageAll }
                        });

                    if (result != null && result.Count > 0)
                    {
                        if (result[0].TryGetUri(out Uri? path))
                        {
                            if (new FileInfo(path.OriginalString).Length > 5000000)
                            {
                                await MessageBoxManager.GetMessageBoxStandardWindow(
                                    new MessageBoxStandardParams
                                    {
                                        ContentTitle = "����� �����������",
                                        ContentMessage = "������ ����� ��������� 5 ��",
                                        WindowIcon = this.Icon,
                                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                                    }).ShowDialog(this);

                                return;
                            }

                            using FileStream fstream = File.OpenRead(path.OriginalString);
                            byte[] buffer = new byte[fstream.Length];
                            await fstream.ReadAsync(buffer);

                            if (!FormatCheck.IsImage(buffer))
                            {
                                await MessageBoxManager.GetMessageBoxStandardWindow(
                                    new MessageBoxStandardParams
                                    {
                                        ContentTitle = "����� �����������",
                                        ContentMessage = "��������� ���� �� �������� ������������",
                                        WindowIcon = this.Icon,
                                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                                    }).ShowDialog(this);

                                return;
                            }

                            using HttpClient client = new();
                            client.DefaultRequestHeaders.Authorization = MainWindowViewModel.Client.DefaultRequestHeaders.Authorization;
                            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/png"));

                            fstream.Position = 0;
                            using StreamContent fileStreamContent = new(fstream);
                            fileStreamContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");

                            using MultipartFormDataContent content = new() { { fileStreamContent, "uploadedFile", ViewModel.Id } };

                            HttpResponseMessage response;
                            Stream responseResult;
                            try
                            {
                                response = await client.PostAsync(ServerSettings.ServerAddress + "/api/files/changeGroupChatAvatar", content);
                                responseResult = await response.Content.ReadAsStreamAsync();
                            }
                            catch
                            {
                                ServerSettings.ConnectionLost = true;
                                this.Close();
                                return;
                            }

                            if (response.StatusCode == HttpStatusCode.OK)
                            {
                                using var reader = new StreamReader(responseResult);

                                await MessageBoxManager.GetMessageBoxStandardWindow(
                                    new MessageBoxStandardParams
                                    {
                                        ContentTitle = "��������� ����������� ����",
                                        ContentMessage = "����������� ���� ������� ��������",
                                        WindowIcon = this.Icon,
                                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                                    }).ShowDialog(this);

                                FinishChangeAvatar();

                                invAvatarI.Source = "/Assets/group.png";
                                ((UpdatableDiskCachedWebImageLoader)ImageLoader.AsyncImageLoader).Reload = true;
                                string newPhotoPath = ServerSettings.ServerAddress + reader.ReadToEnd();
                                invAvatarI.Source = newPhotoPath;
                                ViewModel!.PhotoFileName = newPhotoPath;
                            }
                            else
                            {
                                await MessageBoxManager.GetMessageBoxStandardWindow(
                                    new MessageBoxStandardParams
                                    {
                                        ContentTitle = "�������� �����������",
                                        ContentMessage = "������, ���������� ��� ���",
                                        WindowIcon = this.Icon,
                                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                                    }).ShowDialog(this);
                            }

                            response.Dispose();
                            responseResult.Dispose();
                        }
                        else
                        {
                            await MessageBoxManager.GetMessageBoxStandardWindow(
                                new MessageBoxStandardParams
                                {
                                    ContentTitle = "����� �����������",
                                    ContentMessage = "������, ���������� ��� ���",
                                    WindowIcon = this.Icon,
                                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                                }).ShowDialog(this);
                        }
                    }
                }
            }
        }

        private async void ClearAvatar(object? sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                if (invAvatarI.Source != "/Assets/group.png")
                {
                    var deleteMessage = MessageBoxManager.GetMessageBoxCustomWindow(new MessageBoxCustomParams
                    {
                        ContentMessage = "�� ������������� ������ ������� ����������� ����?",
                        ContentTitle = "�������� ����������� ����",
                        ButtonDefinitions = new[]
                        {
                        new ButtonDefinition() { Name = "��", IsDefault = true },
                        new ButtonDefinition() { Name = "���", IsCancel = true },
                    },
                        WindowIcon = this.Icon,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    });

                    if (await deleteMessage.ShowDialog(this) == "��")
                    {
                        try
                        {
                            await MainWindowViewModel.Connection.InvokeAsync("DeleteGroupChatAvatar", ViewModel.Id);
                        }
                        catch
                        {
                            ServerSettings.ConnectionLost = true;
                            this.Close();
                            return;
                        }

                        await MessageBoxManager.GetMessageBoxStandardWindow(
                            new MessageBoxStandardParams
                            {
                                ContentTitle = "�������� ����������� ����",
                                ContentMessage = "����������� ������� �������",
                                WindowIcon = this.Icon,
                                WindowStartupLocation = WindowStartupLocation.CenterOwner
                            }).ShowDialog(this);

                        FinishChangeAvatar();

                        ((UpdatableDiskCachedWebImageLoader)ImageLoader.AsyncImageLoader).Reload = true;
                        invAvatarI.Source = "/Assets/group.png";
                        ViewModel!.PhotoFileName = "/Assets/group.png";
                    }
                }
            }
        }

        private void CancelChangeAvatar(object? sender, RoutedEventArgs e)
        {
            FinishChangeAvatar();
        }

        private void FinishChangeAvatar()
        {
            avatarI.IsVisible = true;
            changeAvatarB.Content = "��������";
            changeAvatarB.ClearValue(ToolTip.TipProperty);
            changeAvatarClearB.IsVisible = false;
            changeAvatarCancelB.IsVisible = false;
        }

        private string ChatNameTemp = string.Empty;

        private async void EditChatName(object? sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                if (chatNameTB.IsReadOnly)
                {
                    chatNameEditB.Content = "���������";
                    chatNameEditB.SetValue(Grid.ColumnSpanProperty, 1);
                    chatNameEditCancelB.IsVisible = true;
                    chatNameTB.IsReadOnly = false;
                    ChatNameTemp = chatNameTB.Text!;
                }
                else
                {
                    if (chatNameTB.Text! != ChatNameTemp)
                    {
                        try
                        {
                            await MainWindowViewModel.Connection.InvokeAsync("UpdateGroupChatName", ViewModel.Id, chatNameTB.Text!);
                        }
                        catch
                        {
                            ServerSettings.ConnectionLost = true;
                            this.Close();
                            return;
                        }
                    }

                    FinishEditChatName();
                }
            }
        }

        private void CancelEditChatName(object? sender, RoutedEventArgs e)
        {
            FinishEditChatName();
            chatNameTB.Text = ChatNameTemp;
        }

        private void FinishEditChatName()
        {
            chatNameEditB.Content = "��������";
            chatNameEditB.SetValue(Grid.ColumnSpanProperty, 2);
            chatNameEditCancelB.IsVisible = false;
            chatNameTB.IsReadOnly = true;
        }

        private async void Delete(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                var deleteMessage = MessageBoxManager.GetMessageBoxCustomWindow(new MessageBoxCustomParams
                {
                    ContentMessage = "�� ������������� ������ ������� ������ ���?",
                    ContentTitle = "�������� ����",
                    ButtonDefinitions = new[]
                    {
                        new ButtonDefinition() { Name = "��", IsDefault = true },
                        new ButtonDefinition() { Name = "���", IsCancel = true },
                    },
                    WindowIcon = this.Icon,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                });

                if (await deleteMessage.ShowDialog(this) == "��")
                {
                    try
                    {
                        await MainWindowViewModel.Connection.InvokeAsync("DeleteGroupChat", ViewModel.Id);
                    }
                    catch
                    {
                        ServerSettings.ConnectionLost = true;
                    }

                    this.Close();
                }
            }
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
