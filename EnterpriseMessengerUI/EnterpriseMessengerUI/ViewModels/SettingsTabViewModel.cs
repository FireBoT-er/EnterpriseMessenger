using ReactiveUI;

namespace EnterpriseMessengerUI.ViewModels
{
    #pragma warning disable CS8618
    public class SettingsTabViewModel : ViewModelBase
    {
        private string _Surname;

        public string Surname
        {
            get => _Surname;
            set => this.RaiseAndSetIfChanged(ref _Surname, value);
        }

        private string _Name;

        public string Name
        {
            get => _Name;
            set => this.RaiseAndSetIfChanged(ref _Name, value);
        }

        private string _Patronymic;

        public string Patronymic
        {
            get => _Patronymic;
            set => this.RaiseAndSetIfChanged(ref _Patronymic, value);
        }

        private string _Position;

        public string Position
        {
            get => _Position;
            set => this.RaiseAndSetIfChanged(ref _Position, value);
        }

        private string _PhotoFileName;

        public string PhotoFileName
        {
            get => _PhotoFileName;
            set => this.RaiseAndSetIfChanged(ref _PhotoFileName, value);
        }

        private string _Information;

        public string Information
        {
            get => _Information;
            set => this.RaiseAndSetIfChanged(ref _Information, value);
        }
    }
    #pragma warning restore CS8618
}
