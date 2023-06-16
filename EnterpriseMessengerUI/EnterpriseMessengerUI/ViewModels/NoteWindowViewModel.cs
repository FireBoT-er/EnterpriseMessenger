using EnterpriseMessengerUI.Models;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;

namespace EnterpriseMessengerUI.ViewModels
{
    public class NoteWindowViewModel : ViewModelBase
    {
        public NoteWindowViewModel()
        {
            this.WhenAnyValue(x => x.NoteText).Subscribe(_ => UpdateConfirmEnabled());

            _NoteText = string.Empty;
            _SubPoints = new();
        }

        public bool IsFirst { get; set; }

        public long Id { get; set; }

        public bool IsAuthor { get; set; }

        private string _NoteText;

        public string NoteText
        {
            get => _NoteText;
            set => this.RaiseAndSetIfChanged(ref _NoteText, value);
        }

        private bool _ConfirmEnabled;

        public bool ConfirmEnabled
        {
            get => _ConfirmEnabled;
            protected set => this.RaiseAndSetIfChanged(ref _ConfirmEnabled, value);
        }

        private void UpdateConfirmEnabled()
        {
            ConfirmEnabled = !string.IsNullOrWhiteSpace(NoteText);
        }

        private ObservableCollection<NoteSubPointModel> _SubPoints;

        public ObservableCollection<NoteSubPointModel> SubPoints
        {
            get => _SubPoints;
            set => this.RaiseAndSetIfChanged(ref _SubPoints, value);
        }
    }
}
