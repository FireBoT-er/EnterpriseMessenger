using DynamicData;
using Microsoft.AspNetCore.SignalR.Client;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Windows.Input;

namespace EnterpriseMessengerUI.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel()
        {
            _CurrentPage = Pages[0];

            var canNavNext = this.WhenAnyValue(x => x.CurrentPage.CanNavigateNext);
            var canNavPrev = this.WhenAnyValue(x => x.CurrentPage.CanNavigatePrevious);

            NavigateNextCommand = ReactiveCommand.Create(NavigateNext, canNavNext);
            NavigatePreviousCommand = ReactiveCommand.Create(NavigatePrevious, canNavPrev);

            FilesAndServer = new List<string>();
        }

        #pragma warning disable CS8618
        public static HttpClient Client { get; set; }
        public static HubConnection Connection { get; set; }

        public static List<string> FilesAndServer { get; set; }
        #pragma warning restore CS8618

        private readonly PageViewModelBase[] Pages =
        {
            new AuthorizationViewModel(),
            new TabsViewModel()
        };

        private PageViewModelBase _CurrentPage;

        public PageViewModelBase CurrentPage
        {
            get { return _CurrentPage; }
            private set { this.RaiseAndSetIfChanged(ref _CurrentPage, value); }
        }

        public ICommand NavigateNextCommand { get; }

        public void NavigateNext()
        {
            var index = Pages.IndexOf(CurrentPage) + 1;
            CurrentPage = Pages[index];
        }

        public ICommand NavigatePreviousCommand { get; }

        private void NavigatePrevious()
        {
            var index = Pages.IndexOf(CurrentPage) - 1;
            CurrentPage = Pages[index];
        }
    }
}
