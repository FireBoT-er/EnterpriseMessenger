using AsyncImageLoader;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using EnterpriseMessengerUI.Models;
using EnterpriseMessengerUI.ViewModels;
using MessageBox.Avalonia;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Models;
using Microsoft.AspNetCore.SignalR.Client;
using NetCoreAudio;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace EnterpriseMessengerUI.Views
{
    public partial class SettingsTabView : ReactiveUserControl<SettingsTabViewModel>
    {
        public SettingsTabView()
        {
            InitializeComponent();
            Loaded += SettingsTabView_Loaded;
            AddConnectionHandlers();

            this.WhenAnyValue(x => x.invAvatarI.CurrentImage).Subscribe(_ => ShowImage());
        }

        private void AddConnectionHandlers()
        {
            MainWindowViewModel.Connection.On<JsonElement?>("ChangePasswordResults", (results) =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (results == null)
                    {
                        MessageBoxManager.GetMessageBoxStandardWindow(
                            new MessageBoxStandardParams
                            {
                                ContentTitle = "Изменение пароля",
                                ContentMessage = "Пароль успешно изменён",
                                WindowIcon = ((MainWindow)this.Parent!.Parent!.Parent!.Parent!.Parent!).Icon,
                                WindowStartupLocation = WindowStartupLocation.CenterOwner
                            }).ShowDialog((MainWindow)this.Parent!.Parent!.Parent!.Parent!.Parent!);

                        FinishСhangePassword();
                    }
                    else
                    {
                        ErrorsInfoMessage = string.Empty;

                        for (int i = 0; i < results.Value.GetArrayLength(); i++)
                        {
                            ErrorsInfoMessage += results.Value[i].GetProperty("description").ToString() + "\n";
                        }
                        ErrorsInfoMessage = ErrorsInfoMessage.Remove(ErrorsInfoMessage.Length - 1);

                        showErrorsB.IsVisible = true;
                    }
                });
            });

            MainWindowViewModel.Connection.On<string, string, string, string, string>("SendUpdatedUserFullNamePosition", (userId, surname, name, patronymic, position) =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (ViewModel != null)
                    {
                        if (userId == CurrentUserModel.Id)
                        {
                            if (!string.IsNullOrWhiteSpace(surname))
                            {
                                CurrentUserModel.Surname = surname;
                                ViewModel.Surname = CurrentUserModel.Surname!;
                            }

                            if (!string.IsNullOrWhiteSpace(name))
                            {
                                CurrentUserModel.Name = name;
                                ViewModel.Name = CurrentUserModel.Name!;
                            }

                            if (!string.IsNullOrWhiteSpace(patronymic))
                            {
                                CurrentUserModel.Patronymic = patronymic;
                                ViewModel.Patronymic = CurrentUserModel.Patronymic!;
                            }

                            if (!string.IsNullOrWhiteSpace(position))
                            {
                                CurrentUserModel.Position = position;
                                ViewModel.Position = CurrentUserModel.Position!;
                            }
                        }
                    }
                });
            });
        }

        private void SettingsTabView_Loaded(object? sender, RoutedEventArgs e)
        {
            ViewModel!.Surname = CurrentUserModel.Surname!;
            ViewModel!.Name = CurrentUserModel.Name!;
            ViewModel!.Patronymic = CurrentUserModel.Patronymic!;
            ViewModel!.Position = CurrentUserModel.Position!;
            ViewModel!.PhotoFileName = CurrentUserModel.PhotoFileName!;
            ViewModel!.Information = CurrentUserModel.Information!;

            switch (CurrentUserModel.NetworkStatus)
            {
                case NetworkStatus.Online:
                    onlineRB.IsChecked = true;
                    break;
                case NetworkStatus.Busy:
                    busyRB.IsChecked = true;
                    break;
            }
        }

        private void ShowImage()
        {
            ((ImageBrush)avatarI.Background!).Source = (Bitmap)invAvatarI.CurrentImage!;
        }

        private async void ChangeAvatar(object? sender, RoutedEventArgs e)
        {
            if (avatarI.IsVisible)
            {
                avatarI.IsVisible = false;
                changeAvatarB.Content = "Выбрать файл";
                changeAvatarB.SetValue(ToolTip.TipProperty, "Размер изображения не должен превышать 5 Мб");
                changeAvatarClearB.IsVisible = true;
                changeAvatarCancelB.IsVisible = true;
            }
            else
            {
                var result = await ((MainWindow)this.Parent!.Parent!.Parent!.Parent!.Parent!).StorageProvider.
                    OpenFilePickerAsync(new FilePickerOpenOptions
                    {
                        Title = "Выберите изображение:",
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
                                    ContentTitle = "Выбор изображения",
                                    ContentMessage = "Размер файла превышает 5 Мб",
                                    WindowIcon = ((MainWindow)this.Parent!.Parent!.Parent!.Parent!.Parent!).Icon,
                                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                                }).ShowDialog((MainWindow)this.Parent!.Parent!.Parent!.Parent!.Parent!);

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
                                    ContentTitle = "Выбор изображения",
                                    ContentMessage = "Выбранный файл не является изображением",
                                    WindowIcon = ((MainWindow)this.Parent!.Parent!.Parent!.Parent!.Parent!).Icon,
                                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                                }).ShowDialog((MainWindow)this.Parent!.Parent!.Parent!.Parent!.Parent!);

                            return;
                        }

                        using HttpClient client = new();
                        client.DefaultRequestHeaders.Authorization = MainWindowViewModel.Client.DefaultRequestHeaders.Authorization;
                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/png"));

                        fstream.Position = 0;
                        using StreamContent fileStreamContent = new(fstream);
                        fileStreamContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");

                        using MultipartFormDataContent content = new() { { fileStreamContent, "uploadedFile", "newAvatar" } };

                        HttpResponseMessage response;
                        Stream responseResult;
                        try
                        {
                            response = await client.PostAsync(ServerSettings.ServerAddress + "/api/files/changeAvatar", content);
                            responseResult = await response.Content.ReadAsStreamAsync();
                        }
                        catch
                        {
                            ConnectionLost();
                            return;
                        }

                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            using var reader = new StreamReader(responseResult);

                            await MessageBoxManager.GetMessageBoxStandardWindow(
                                new MessageBoxStandardParams
                                {
                                    ContentTitle = "Изменение изображения профиля",
                                    ContentMessage = "Изображение профиля успешно изменено",
                                    WindowIcon = ((MainWindow)this.Parent!.Parent!.Parent!.Parent!.Parent!).Icon,
                                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                                }).ShowDialog((MainWindow)this.Parent!.Parent!.Parent!.Parent!.Parent!);

                            FinishChangeAvatar();

                            invAvatarI.Source = "/Assets/user.png";
                            ((UpdatableDiskCachedWebImageLoader)ImageLoader.AsyncImageLoader).Reload = true;
                            CurrentUserModel.PhotoFileName = ServerSettings.ServerAddress + reader.ReadToEnd();
                            invAvatarI.Source = CurrentUserModel.PhotoFileName;
                            ViewModel!.PhotoFileName = CurrentUserModel.PhotoFileName;
                        }
                        else
                        {
                            await MessageBoxManager.GetMessageBoxStandardWindow(
                                new MessageBoxStandardParams
                                {
                                    ContentTitle = "Загрузка изображения",
                                    ContentMessage = "Ошибка, попробуйте ещё раз",
                                    WindowIcon = ((MainWindow)this.Parent!.Parent!.Parent!.Parent!.Parent!).Icon,
                                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                                }).ShowDialog((MainWindow)this.Parent!.Parent!.Parent!.Parent!.Parent!);
                        }

                        response.Dispose();
                        responseResult.Dispose();
                    }
                    else
                    {
                        await MessageBoxManager.GetMessageBoxStandardWindow(
                            new MessageBoxStandardParams
                            {
                                ContentTitle = "Выбор изображения",
                                ContentMessage = "Ошибка, попробуйте ещё раз",
                                WindowIcon = ((MainWindow)this.Parent!.Parent!.Parent!.Parent!.Parent!).Icon,
                                WindowStartupLocation = WindowStartupLocation.CenterOwner
                            }).ShowDialog((MainWindow)this.Parent!.Parent!.Parent!.Parent!.Parent!);
                    }
                }
            }
        }

        private async void ClearAvatar(object? sender, RoutedEventArgs e)
        {
            if (invAvatarI.Source != "/Assets/user.png")
            {
                var deleteMessage = MessageBoxManager.GetMessageBoxCustomWindow(new MessageBoxCustomParams
                {
                    ContentMessage = "Вы действительно хотите удалить изображение профиля?",
                    ContentTitle = "Удаление изображения профиля",
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
                        await MainWindowViewModel.Connection.InvokeAsync("DeleteUserAvatar");
                    }
                    catch
                    {
                        ConnectionLost();
                        return;
                    }

                    await MessageBoxManager.GetMessageBoxStandardWindow(
                        new MessageBoxStandardParams
                        {
                            ContentTitle = "Удаление изображения профиля",
                            ContentMessage = "Изображение успешно удалено",
                            WindowIcon = ((MainWindow)this.Parent!.Parent!.Parent!.Parent!.Parent!).Icon,
                            WindowStartupLocation = WindowStartupLocation.CenterOwner
                        }).ShowDialog((MainWindow)this.Parent!.Parent!.Parent!.Parent!.Parent!);

                    FinishChangeAvatar();

                    ((UpdatableDiskCachedWebImageLoader)ImageLoader.AsyncImageLoader).Reload = true;
                    CurrentUserModel.PhotoFileName = "/Assets/user.png";
                    invAvatarI.Source = CurrentUserModel.PhotoFileName;
                    ViewModel!.PhotoFileName = CurrentUserModel.PhotoFileName;
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
            changeAvatarB.Content = "Изменить";
            changeAvatarB.ClearValue(ToolTip.TipProperty);
            changeAvatarClearB.IsVisible = false;
            changeAvatarCancelB.IsVisible = false;
        }

        private void About(object? sender, RoutedEventArgs e)
        {
            if ((DateTime.Now.Month == 12 && DateTime.Now.Day > 20) || (DateTime.Now.Month == 1 && DateTime.Now.Day < 15))
            {
                try
                {
                    new Player().Play(AppDomain.CurrentDomain.BaseDirectory + "Assets/bells.mp3").Wait();
                }
                catch { }
            }

            MessageBoxManager.GetMessageBoxStandardWindow(
                new MessageBoxStandardParams
                {
                    ContentTitle = "Об авторе",
                    ContentMessage = "Емельянов Владислав\n" +
                                     "vlademel2016@yandex.ru\n" +
                                     "МИВлГУ, ФИТР, ПИн-119\n" +
                                     "2023 г.",
                    WindowIcon = ((MainWindow)this.Parent!.Parent!.Parent!.Parent!.Parent!).Icon,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                }).ShowDialog((MainWindow)this.Parent!.Parent!.Parent!.Parent!.Parent!);
        }

        private void Licenses(object? sender, RoutedEventArgs e)
        {
            MessageBoxManager.GetMessageBoxStandardWindow(
                new MessageBoxStandardParams
                {
                    ContentTitle = "Информация о лицензируемых материалах",
                    ContentMessage = "Conversation icons created by Pixel perfect - Flaticon: https://www.flaticon.com/free-icons/conversation\n" +
                                     "Essay icons created by Pixel perfect - Flaticon: https://www.flaticon.com/free-icons/essay\n" +
                                     "Settings icons created by Pixel perfect - Flaticon: https://www.flaticon.com/free-icons/settings\n" +
                                     "Attach icons created by Pixel perfect - Flaticon: https://www.flaticon.com/free-icons/attach\n" +
                                     "Send icons created by inkubators - Flaticon: https://www.flaticon.com/free-icons/send\n" +
                                     "Add icons created by Pixel perfect - Flaticon: https://www.flaticon.com/free-icons/add\n" +
                                     "Search icons created by Pixel perfect - Flaticon: https://www.flaticon.com/free-icons/search\n" +
                                     "Profile icons created by Freepik - Flaticon: https://www.flaticon.com/free-icons/profile\n" +
                                     "Info icons created by Freepik - Flaticon: https://www.flaticon.com/free-icons/info\n" +
                                     "Close icons created by Pixel perfect - Flaticon: https://www.flaticon.com/free-icons/close\n" +
                                     "Blank page icons created by Tempo_doloe - Flaticon: https://www.flaticon.com/free-icons/blank-page\n" +
                                     "Right arrow icons created by Hexagon075 - Flaticon: https://www.flaticon.com/free-icons/right-arrow\n" +
                                     "Download icons created by Pixel perfect - Flaticon: https://www.flaticon.com/free-icons/download\n" +
                                     "Post it icons created by Freepik - Flaticon: https://www.flaticon.com/free-icons/post-it\n" +
                                     "List icons created by Freepik - Flaticon: https://www.flaticon.com/free-icons/list\n" +
                                     "Group icons created by Freepik - Flaticon: https://www.flaticon.com/free-icons/group\n" +
                                     "Logout icons created by Pixel perfect - Flaticon: https://www.flaticon.com/free-icons/logout\n" +
                                     "Ui icons created by Fathema Khanom - Flaticon: https://www.flaticon.com/free-icons/ui\n" +
                                     "Circle icons created by Freepik - Flaticon: https://www.flaticon.com/free-icons/circle" +
                                     (new Random().Next(4) == 0 ? "\nОтветы на главные вопросы жизни, вселенной и всего такого - Sage (gpt-3.5-turbo)" : string.Empty),
                    WindowIcon = ((MainWindow)this.Parent!.Parent!.Parent!.Parent!.Parent!).Icon,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                }).ShowDialog((MainWindow)this.Parent!.Parent!.Parent!.Parent!.Parent!);
        }

        private async void OnlineRB_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                await MainWindowViewModel.Connection.InvokeAsync("IWannaBe", NetworkStatus.Online);
                CurrentUserModel.NetworkStatus = NetworkStatus.Online;
            }
            catch
            {
                ConnectionLost();
            }
        }

        private async void BusyRB_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                await MainWindowViewModel.Connection.InvokeAsync("IWannaBe", NetworkStatus.Busy);
                CurrentUserModel.NetworkStatus = NetworkStatus.Busy;
            }
            catch
            {
                ConnectionLost();
            }
        }

        private string InformationTemp = string.Empty;

        private async void EditInformation(object? sender, RoutedEventArgs e)
        {
            if (infoTB.IsReadOnly)
            {
                infoEditB.Content = "Сохранить";
                infoEditB.SetValue(Grid.ColumnSpanProperty, 1);
                infoEditCancelB.IsVisible = true;
                infoTB.IsReadOnly = false;
                InformationTemp = infoTB.Text!;
            }
            else
            {
                if (infoTB.Text! != InformationTemp)
                {
                    try
                    {
                        await MainWindowViewModel.Connection.InvokeAsync("UpdateUserInformation", infoTB.Text!);
                    }
                    catch
                    {
                        ConnectionLost();
                        return;
                    }

                    CurrentUserModel.Information = infoTB.Text!;
                }
                
                FinishEditInformation();
            }
        }

        private void CancelEditInformation(object? sender, RoutedEventArgs e)
        {
            FinishEditInformation();
            infoTB.Text = InformationTemp;
        }

        private void FinishEditInformation()
        {
            infoEditB.Content = "Изменить";
            infoEditB.SetValue(Grid.ColumnSpanProperty, 2);
            infoEditCancelB.IsVisible = false;
            infoTB.IsReadOnly = true;
        }

        private void OldPassword_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(((TextBox)sender!).Text))
            {
                newPasswordTB.Focus();
            }
        }

        private void NewPassword_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(((TextBox)sender!).Text))
            {
                СhangePassword(sender, e);
            }
        }

        private string ErrorsInfoMessage = string.Empty;

        private void ShowErrors(object? sender, RoutedEventArgs e)
        {
            MessageBoxManager.GetMessageBoxStandardWindow(
                new MessageBoxStandardParams
                {
                    ContentTitle = "Изменение пароля: ошибки",
                    ContentMessage = ErrorsInfoMessage,
                    WindowIcon = ((MainWindow)this.Parent!.Parent!.Parent!.Parent!.Parent!).Icon,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                }).ShowDialog((MainWindow)this.Parent!.Parent!.Parent!.Parent!.Parent!);
        }

        private async void СhangePassword(object? sender, RoutedEventArgs e)
        {
            if (!oldPasswordTB.IsVisible)
            {
                oldPasswordTB.IsVisible = true;
                newPasswordTB.IsVisible = true;
                changePasswordB.Content = "Сохранить";
                changePasswordСancelB.IsVisible = true;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(oldPasswordTB.Text) && !string.IsNullOrWhiteSpace(newPasswordTB.Text))
                {
                    showErrorsB.IsVisible = false;

                    try
                    {
                        await MainWindowViewModel.Connection.InvokeAsync("ChangePassword", new { OldPassword = oldPasswordTB.Text, NewPassword = newPasswordTB.Text });
                    }
                    catch
                    {
                        ConnectionLost();
                        return;
                    }
                }
            }
        }

        private void CancelСhangePassword(object? sender, RoutedEventArgs e)
        {
            FinishСhangePassword();
        }

        private void FinishСhangePassword()
        {
            oldPasswordTB.IsVisible = false;
            oldPasswordTB.Clear();
            newPasswordTB.IsVisible = false;
            newPasswordTB.Clear();
            showErrorsB.IsVisible = false;
            ErrorsInfoMessage = string.Empty;
            changePasswordB.Content = "Изменить";
            changePasswordСancelB.IsVisible = false;
        }

        private async void Logout(object? sender, RoutedEventArgs e)
        {
            if (MainWindowViewModel.FilesAndServer.Count > 0)
            {
                await MessageBoxManager.GetMessageBoxStandardWindow(
                    new MessageBoxStandardParams
                    {
                        ContentTitle = "Загрузка файлов",
                        ContentMessage = "Вы не можете выйти из аккаунта, пока загружаются файлы",
                        WindowIcon = ((MainWindow)this.Parent!.Parent!.Parent!.Parent!.Parent!).Icon,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    }).ShowDialog((MainWindow)this.Parent!.Parent!.Parent!.Parent!.Parent!);
            }
            else
            {
                ((MainWindowViewModel)this.Parent!.Parent!.Parent!.Parent!.Parent!.DataContext!).NavigatePreviousCommand.Execute(null);

                try { await MainWindowViewModel.Connection.StopAsync(); }
                finally { await MainWindowViewModel.Connection.DisposeAsync(); }
            }
        }

        private void ConnectionLost()
        {
            ServerSettings.ConnectionLost = true;
            ((MainWindowViewModel)this.Parent!.Parent!.Parent!.Parent!.Parent!.DataContext!).NavigatePreviousCommand.Execute(null);
        }
    }
}
