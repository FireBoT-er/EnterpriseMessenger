using Avalonia.Threading;
using EnterpriseMessengerUI.Models;
using EnterpriseMessengerUI.ViewModels;
using EnterpriseMessengerUI.Views;
using NetCoreAudio;
using System;
using System.Threading;

namespace EnterpriseMessengerUI
{
    public static class Notification
    {
        public static void Show(string photoFileName, string surname, string name, string patronymic, string text)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (CurrentUserModel.NetworkStatus == NetworkStatus.Busy)
                {
                    return;
                }

                var pseudoNotification = new PseudoNotificationWindow() { DataContext = new PseudoNotificationWindowViewModel() };
                ((PseudoNotificationWindowViewModel)pseudoNotification.DataContext).PhotoFileName = photoFileName;
                ((PseudoNotificationWindowViewModel)pseudoNotification.DataContext).Surname = surname;
                ((PseudoNotificationWindowViewModel)pseudoNotification.DataContext).Name = name;
                ((PseudoNotificationWindowViewModel)pseudoNotification.DataContext).Patronymic = patronymic;
                ((PseudoNotificationWindowViewModel)pseudoNotification.DataContext).Text = text;
                pseudoNotification.Show();

                try
                {
                    new Player().Play(AppDomain.CurrentDomain.BaseDirectory + "Assets/notification.mp3").Wait();
                }
                catch { }

                Timer pseudoNotificationTimer = new(state =>
                {
                    Dispatcher.UIThread.InvokeAsync(() => pseudoNotification.Close());
                }, null, 5000, Timeout.Infinite);
            });
        }
    }
}
