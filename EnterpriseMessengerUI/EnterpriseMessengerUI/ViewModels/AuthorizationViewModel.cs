using ReactiveUI;
using System;
using System.ComponentModel.DataAnnotations;

namespace EnterpriseMessengerUI.ViewModels
{
    public class AuthorizationViewModel : PageViewModelBase
    {
        public AuthorizationViewModel()
        {
            this.WhenAnyValue(x => x.Login, x => x.Password).Subscribe(_ => UpdateCanNavigateNext());
        }

        private string? _Login;

        [Required(ErrorMessage = "Введите логин")]
        public string? Login
        {
            get { return _Login; }
            set { this.RaiseAndSetIfChanged(ref _Login, value); }
        }

        private string? _Password;

        [Required(ErrorMessage = "Введите пароль")]
        public string? Password
        {
            get { return _Password; }
            set { this.RaiseAndSetIfChanged(ref _Password, value); }
        }

        private bool _CanNavigateNext;

        public override bool CanNavigateNext
        {
            get { return _CanNavigateNext; }
            protected set { this.RaiseAndSetIfChanged(ref _CanNavigateNext, value); }
        }

        public override bool CanNavigatePrevious
        {
            get => false;
            protected set => throw new NotSupportedException();
        }

        private void UpdateCanNavigateNext()
        {
            CanNavigateNext = !string.IsNullOrWhiteSpace(_Login) && !string.IsNullOrWhiteSpace(_Password);
        }
    }
}
