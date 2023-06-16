using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using EnterpriseMessengerUI.Views;
using System.ComponentModel;

namespace EnterpriseMessengerUI.Models
{
    public class NoteModel : INotifyPropertyChanged
    {
        public long Id { get; }

        public bool IsFirst { get; }

        public string MainActionText { get; }

        public string? ButtonToolTip { get; }

        private string _Name;
        public string Name
        {
            get { return _Name; }
            set
            {
                if (_Name != value)
                {
                    _Name = value;
                    RaisePropertyChanged(nameof(Name));
                }
            }
        }

        public bool IsChecked { get; set; }

        private TextDecorationCollection? _TextDecorations;
        public TextDecorationCollection? TextDecorations
        {
            get { return _TextDecorations; }
            private set
            {
                if (_TextDecorations != value)
                {
                    _TextDecorations = value;
                    RaisePropertyChanged(nameof(TextDecorations));
                }
            }
        }

        private bool _HasSubPoints;
        public bool HasSubPoints
        {
            get { return _HasSubPoints; }
            set
            {
                if (_HasSubPoints != value)
                {
                    _HasSubPoints = value;
                    RaisePropertyChanged(nameof(HasSubPoints));
                }
            }
        }

        private Thickness _NameMargin;
        public Thickness NameMargin
        {
            get { return _NameMargin; }
            private set
            {
                if (_NameMargin != value)
                {
                    _NameMargin = value;
                    RaisePropertyChanged(nameof(NameMargin));
                }
            }
        }

        public bool IsAuthor { get; }

        #pragma warning disable CS8618
        public NoteModel()
        #pragma warning restore CS8618
        {
            Id = -1;
            IsFirst = true;
            MainActionText = "Создать";
            ButtonToolTip = MainActionText;
            Name = string.Empty;
            IsChecked = false;
            HasSubPoints = false;
            RelatedPropertiesSet();
            IsAuthor = false;
        }

        #pragma warning disable CS8618
        public NoteModel(long id, string name, bool isChecked, bool hasSubPoints, bool isAuthor)
        #pragma warning restore CS8618
        {
            Id = id;
            IsFirst = false;
            MainActionText = "Редактировать";
            ButtonToolTip = null;
            Name = name;
            IsChecked = isChecked;
            HasSubPoints = hasSubPoints;
            IsAuthor = isAuthor;
            RelatedPropertiesSet();
        }

        public void RelatedPropertiesSet()
        {
            TextDecorations = IsChecked ? Avalonia.Media.TextDecorations.Strikethrough : null;
            NameMargin = (HasSubPoints || !IsAuthor) ? new Thickness(0, 25, 0, 0) : new Thickness(0, 0, 0, 0);
        }

        public void OnClick(object itemsRepeater)
        {
            if (Id == -1)
            {
                ((NotesTabView)((ItemsRepeater)itemsRepeater).Parent!.Parent!).Create();
            }
            else
            {
                ((NotesTabView)((ItemsRepeater)itemsRepeater).Parent!.Parent!).Edit(Id, IsAuthor);
            }
        }

        public void Share(object itemsRepeater)
        {
            ((NotesTabView)((ItemsRepeater)itemsRepeater).Parent!.Parent!).Share(false, Id, false);
        }

        public void ShowOwners(object itemsRepeater)
        {
            ((NotesTabView)((ItemsRepeater)itemsRepeater).Parent!.Parent!).Share(true, Id, IsAuthor);
        }

        public void Delete(object itemsRepeater)
        {
            ((NotesTabView)((ItemsRepeater)itemsRepeater).Parent!.Parent!).Delete(Id);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
