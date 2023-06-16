using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using EnterpriseMessengerUI.Models;
using EnterpriseMessengerUI.ViewModels;
using MessageBox.Avalonia;
using MessageBox.Avalonia.DTO;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Net.Http;

namespace EnterpriseMessengerUI.Views
{
    public partial class ShowAttachmentsWindow : ReactiveWindow<ShowAttachmentsWindowViewModel>
    {
        public ShowAttachmentsWindow()
        {
            InitializeComponent();
            Loaded += ShowAttachmentsWindow_Loaded;
        }

        private async void ShowAttachmentsWindow_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            try
            {
                await MainWindowViewModel.Connection.InvokeAsync("GetAttachmentsForMessage", ViewModel!.MessageId, false);
            }
            catch
            {
                ServerSettings.ConnectionLost = true;
                this.Close();
            }
        }

        public async void Download(string data, string additionalData)
        {
            lock (((ICollection)MainWindowViewModel.FilesAndServer).SyncRoot)
            {
                MainWindowViewModel.FilesAndServer.Add(data);
            }

            var additionalDataSplitted = additionalData.Split('.');

            var result = await (this).StorageProvider.
                SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "��������� ����:",
                    ShowOverwritePrompt = true,
                    SuggestedFileName = additionalData,
                    DefaultExtension = additionalDataSplitted.Length > 1 ? additionalDataSplitted.Last() : string.Empty,
                });

            if (result != null)
            {
                if (result.TryGetUri(out Uri? path))
                {
                    try
                    {
                        await MessageBoxManager.GetMessageBoxStandardWindow(
                            new MessageBoxStandardParams
                            {
                                ContentTitle = "���������� �����",
                                ContentMessage = "�� �������� �����������, ����� �������� ����������",
                                WindowIcon = this.Icon,
                                WindowStartupLocation = WindowStartupLocation.CenterOwner
                            }).ShowDialog(this);

                        using HttpClient client = new();
                        client.DefaultRequestHeaders.Authorization = MainWindowViewModel.Client.DefaultRequestHeaders.Authorization;
                        using Stream stream = await client.GetStreamAsync(ServerSettings.ServerAddress + "/UserFiles/Attachments/" + data);
                        using FileStream fs = new(path.OriginalString, FileMode.Create);
                        await stream.CopyToAsync(fs);

                        Notification.Show(CurrentUserModel.PhotoFileName!, CurrentUserModel.Surname!, CurrentUserModel.Name!, CurrentUserModel.Patronymic!, "�������� ���������");
                    }
                    catch (IOException)
                    {
                        await MessageBoxManager.GetMessageBoxStandardWindow(
                            new MessageBoxStandardParams
                            {
                                ContentTitle = "���������� �����",
                                ContentMessage = "���������� ��������� ����. ���������, ���������� �� ��������� ����, � ����� ������� ���������� ����� �� �����",
                                WindowIcon = this.Icon,
                                WindowStartupLocation = WindowStartupLocation.CenterOwner
                            }).ShowDialog(this);
                    }
                    catch
                    {
                        await MessageBoxManager.GetMessageBoxStandardWindow(
                            new MessageBoxStandardParams
                            {
                                ContentTitle = "���������� �����",
                                ContentMessage = "���� ����������",
                                WindowIcon = this.Icon,
                                WindowStartupLocation = WindowStartupLocation.CenterOwner
                            }).ShowDialog(this);
                    }
                }
                else
                {
                    await MessageBoxManager.GetMessageBoxStandardWindow(
                        new MessageBoxStandardParams
                        {
                            ContentTitle = "���������� �����",
                            ContentMessage = "������, ���������� ��� ���",
                            WindowIcon = this.Icon,
                            WindowStartupLocation = WindowStartupLocation.CenterOwner
                        }).ShowDialog(this);
                }
            }

            lock (((ICollection)MainWindowViewModel.FilesAndServer).SyncRoot)
            {
                MainWindowViewModel.FilesAndServer.Remove(data);
            }
        }
    }
}
