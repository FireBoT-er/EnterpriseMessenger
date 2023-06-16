using EnterpriseMessengerUI.Views;
using System.ComponentModel;

namespace EnterpriseMessengerUI.Models
{
    public class NoteSubPointModel : INotifyPropertyChanged
    {
        public long Id { get; }

        private bool _IsChecked;
        public bool IsChecked
        {
            get { return _IsChecked; }
            set
            {
                if (_IsChecked != value)
                {
                    _IsChecked = value;
                    RaisePropertyChanged(nameof(IsChecked));
                }
            }
        }

        public string Text { get; set; }

        public NoteSubPointModel()
        {
            Id = 0;
            IsChecked = false;
            Text = string.Empty;
        }

        public NoteSubPointModel(long id, bool isChecked, string text)
        {
            Id = id;
            IsChecked = isChecked;
            Text = text;
        }

        public void CheckboxMask()
        {
            IsChecked = !IsChecked;
        }

        public void RemoveSubPoint(object window)
        {
            ((NoteWindow)window).RemoveSubPoint(this);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
