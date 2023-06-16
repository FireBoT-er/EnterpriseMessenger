using ReactiveUI;

namespace EnterpriseMessengerUI.ViewModels
{
#pragma warning disable CS8618
    public class GroupChatEditWindowViewModel : ViewModelBase
    {
        public string Id { get; set; }

        private string _PhotoFileName;

        public string PhotoFileName
        {
            get => _PhotoFileName;
            set => this.RaiseAndSetIfChanged(ref _PhotoFileName, value);
        }

        private bool _CanDelete;

        public bool CanDelete
        {
            get => _CanDelete;
            set => this.RaiseAndSetIfChanged(ref _CanDelete, value);
        }
    }
    #pragma warning restore CS8618
}
