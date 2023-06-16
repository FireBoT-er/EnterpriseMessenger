using ReactiveUI;

namespace EnterpriseMessengerUI.ViewModels
{
    public class PseudoNotificationWindowViewModel : ViewModelBase
    {
        public PseudoNotificationWindowViewModel() { }

        private string? _PhotoFileName;

        public string? PhotoFileName
        {
            get => _PhotoFileName;
            set => this.RaiseAndSetIfChanged(ref _PhotoFileName, value);
        }

        private string? _Surname;

        public string? Surname
        {
            get => _Surname;
            set => this.RaiseAndSetIfChanged(ref _Surname, value);
        }

        private string? _Name;

        public string? Name
        {
            get => _Name;
            set => this.RaiseAndSetIfChanged(ref _Name, value);
        }

        private string? _Patronymic;

        public string? Patronymic
        {
            get => _Patronymic;
            set => this.RaiseAndSetIfChanged(ref _Patronymic, value);
        }

        private string? _Text;

        public string? Text
        {
            get => _Text;
            set => this.RaiseAndSetIfChanged(ref _Text, value);
        }
    }
}
