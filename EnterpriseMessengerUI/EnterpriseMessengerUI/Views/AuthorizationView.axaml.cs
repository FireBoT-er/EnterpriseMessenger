using AsyncImageLoader;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using EnterpriseMessengerUI.Models;
using EnterpriseMessengerUI.ViewModels;
using MessageBox.Avalonia;
using MessageBox.Avalonia.DTO;
using Microsoft.AspNetCore.SignalR.Client;
using ReactiveUI;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows.Input;

namespace EnterpriseMessengerUI.Views
{
    public partial class AuthorizationView : UserControl
    {
        public AuthorizationView()
        {
            InitializeComponent();
            AuthorizeCommand = ReactiveCommand.Create(Authorize);
            Loaded += AuthorizationView_Loaded;
        }

        private async void AuthorizationView_Loaded(object? sender, RoutedEventArgs e)
        {
            loginB.Command = AuthorizeCommand;

            if (ServerSettings.ConnectionLost)
            {
                ServerSettings.ConnectionLost = false;

                await MessageBoxManager.GetMessageBoxStandardWindow(
                    new MessageBoxStandardParams
                    {
                        ContentTitle = "Соединение потеряно",
                        ContentMessage = "Потеряно соединение с сервером. Попробуйте подключиться ещё раз или обратитесь к техническому специалисту",
                        WindowIcon = ((MainWindow)this.Parent!.Parent!).Icon,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    }).ShowDialog((MainWindow)this.Parent!.Parent!);
            }

            var menuItemsTB = (AvaloniaList<object>)((MenuFlyout)loginTB.ContextFlyout!).Items!;
            ((MenuItem)menuItemsTB[0]).Header = "Вырезать";
            ((MenuItem)menuItemsTB[1]).Header = "Копировать";
            ((MenuItem)menuItemsTB[2]).Header = "Вставить";

            ((MenuItem)((AvaloniaList<object>)((MenuFlyout)dummy.ContextFlyout!).Items!)[0]).Header = "Копировать";
        }

        private ICommand AuthorizeCommand { get; }

        private async void Authorize()
        {
            errorTB.Text = string.Empty;

            loginTB.IsEnabled = false;
            passwordTB.IsEnabled = false;
            loginB.IsEnabled = false;
            errorInfoB.IsVisible = false;

            try
            {
                var jsonObject = JsonNode.Parse(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "server.json"))!.AsObject();
                ServerSettings.ServerAddress = jsonObject["address"]!.ToString();
            }
            catch (Exception ex)
            {
                ShowError("Не удалось подключиться к серверу", ex.Message);
                return;
            }

            string token;

            MainWindowViewModel.Client = new();
            var pkg = new { UserName = loginTB.Text, Password = passwordTB.Text };
            HttpResponseMessage response;
            Stream result;
            try
            {
                response = await MainWindowViewModel.Client.PostAsync(ServerSettings.ServerAddress + "/api/authenticate/login", new StringContent(JsonSerializer.Serialize(pkg), Encoding.UTF8, "application/json"));
                result = await response.Content.ReadAsStreamAsync();
            }
            catch (Exception ex)
            {
                ShowError("Не удалось подключиться к серверу", ex.Message);
                return;
            }

            using (StreamReader reader = new(result))
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    JsonDocument doc = JsonDocument.Parse(reader.ReadToEnd());
                    JsonElement root = doc.RootElement;
                    token = root.GetProperty("token").ToString();
                }
                else
                {
                    ShowError(JsonNode.Parse(response.Content.ReadAsStringAsync().Result)!["message"]!.ToString(), null);
                    return;
                }
            }

            response.Dispose();
            result.Dispose();

            MainWindowViewModel.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            MainWindowViewModel.Connection = new HubConnectionBuilder()
                .WithUrl(ServerSettings.ServerAddress + "/messenger", options =>
                {
                    options.AccessTokenProvider = () =>
                    {
                        return Task.FromResult<string?>(token);
                    };
                })
                .Build();

            MainWindowViewModel.Connection.Closed += async (error) =>
            {
                await Task.Delay(new Random().Next(0, 5) * 1000);
                await MainWindowViewModel.Connection.StartAsync();
            };

            MainWindowViewModel.Connection.On<JsonElement, int>("SendCurrentUserInformation", (currentUser, editDeleteTimeLimit) =>
            {
                CurrentUserModel.Id = currentUser.GetProperty("id").ToString();
                CurrentUserModel.Surname = currentUser.GetProperty("surname").ToString();
                CurrentUserModel.Name = currentUser.GetProperty("name").ToString();
                CurrentUserModel.Patronymic = currentUser.GetProperty("patronymic").ToString();
                CurrentUserModel.Position = currentUser.GetProperty("position").ToString();
                CurrentUserModel.PhotoFileName = currentUser.GetProperty("hasPhoto").GetBoolean() ? $"{ServerSettings.ServerAddress}/UserFiles/Avatars/{CurrentUserModel.Id}.png" : "/Assets/user.png";
                CurrentUserModel.Information = currentUser.GetProperty("information").ToString();
                CurrentUserModel.NetworkStatus = NetworkStatus.Online;

                ServerSettings.EditDeleteTimeLimit = TimeSpan.FromMinutes(editDeleteTimeLimit);
            });

            try
            {
                await MainWindowViewModel.Connection.StartAsync();
                ImageLoader.AsyncImageLoader = new UpdatableDiskCachedWebImageLoader(MainWindowViewModel.Client, false);

                ((MainWindowViewModel)dummy.DataContext!).NavigateNextCommand.Execute(null);
            }
            catch (Exception ex)
            {
                ShowError("Внутренняя ошибка сервера", ex.Message);
            }
        }

        private void ShowError(string message, string? errorInfoMessage)
        {
            errorTB.Text = message;

            loginTB.IsEnabled = true;
            passwordTB.IsEnabled = true;
            loginB.IsEnabled = true;

            if (errorInfoMessage != null)
            {
                ErrorInfoMessage = errorInfoMessage;
                errorInfoB.IsVisible = true;
            }
        }

        private string ErrorInfoMessage = string.Empty;

        private async void ErrorInfo(object sender, RoutedEventArgs e)
        {
            await MessageBoxManager.GetMessageBoxStandardWindow(
                new MessageBoxStandardParams
                {
                    ContentTitle = "Подробности для технического специалиста",
                    ContentMessage = ErrorInfoMessage,
                    WindowIcon = ((MainWindow)this.Parent!.Parent!).Icon,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                }).ShowDialog((MainWindow)this.Parent!.Parent!);
        }

        private void Login_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(((TextBox)sender!).Text))
            {
                passwordTB.Focus();
            }
        }

        private void Password_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(((TextBox)sender!).Text))
            {
                loginB.Command!.Execute(null);
            }
        }
    }
}
