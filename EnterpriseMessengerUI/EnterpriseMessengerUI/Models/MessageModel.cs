using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using EnterpriseMessengerUI.Views;
using System;
using System.ComponentModel;

namespace EnterpriseMessengerUI.Models
{
    public class MessageModel : INotifyPropertyChanged
    {
        public long Id { get; }

        private string _Text;
        public string Text
        {
            get { return _Text; }
            set
            {
                if (_Text != value)
                {
                    _Text = value;
                    RaisePropertyChanged(nameof(Text));
                }
            }
        }

        public string SendDateTime { get; }

        private string _EditedTT;
        public string EditedTT
        {
            get { return _EditedTT; }
            private set
            {
                if (_EditedTT != value)
                {
                    _EditedTT = value;
                    RaisePropertyChanged(nameof(EditedTT));
                }
            }
        }

        private string _Edited;
        public string Edited
        {
            get { return _Edited; }
            private set
            {
                if (_Edited != value)
                {
                    _Edited = value;
                    RaisePropertyChanged(nameof(Edited));
                }
            }
        }

        public string HorizontalAlignment { get; }

        public const string HorizontalAlignmentLeft = "Left";
        public const string HorizontalAlignmentRight = "Right";

        private bool _HasRead;
        public bool HasRead
        {
            get { return _HasRead; }
            set
            {
                if (_HasRead != value)
                {
                    _HasRead = value;
                    SetBackground(value);
                    RaisePropertyChanged(nameof(HasRead));
                }
            }
        }

        private SolidColorBrush _Background;
        public SolidColorBrush Background
        {
            get { return _Background; }
            private set
            {
                if (_Background != value)
                {
                    _Background = value;
                    RaisePropertyChanged(nameof(Background));
                }
            }
        }

        private bool _CanEditDelete;
        public bool CanEditDelete
        {
            get { return _CanEditDelete; }
            private set
            {
                if (_CanEditDelete != value)
                {
                    _CanEditDelete = value;
                    RaisePropertyChanged(nameof(CanEditDelete));
                }
            }
        }

        private bool _IsAttachmentsVisible;
        public bool IsAttachmentsVisible
        {
            get { return _IsAttachmentsVisible; }
            set
            {
                if (_IsAttachmentsVisible != value)
                {
                    _IsAttachmentsVisible = value;
                    RaisePropertyChanged(nameof(IsAttachmentsVisible));
                }
            }
        }

        public string AuthorId { get; }

        public string PhotoFileName { get; }

        public string AuthorName { get; }

        #pragma warning disable CS8618
        public MessageModel(long id, string text, string sendDateTime, DateTime? edited, string horizontalAlignment, bool hasRead, bool isAttachmentsVisible, string authorId, bool authorHasPhoto, string authorName)
        #pragma warning restore CS8618
        {
            Id = id;
            Text = text;
            SendDateTime = sendDateTime;
            SetEdited(edited);
            HorizontalAlignment = horizontalAlignment;
            HasRead = hasRead;
            SetBackground(hasRead);
            IsAttachmentsVisible = isAttachmentsVisible;

            AuthorId = authorId;
            PhotoFileName = authorHasPhoto ? $"{ServerSettings.ServerAddress}/UserFiles/Avatars/{authorId}.png" : "/Assets/user.png";
            AuthorName = authorName;
        }

        public void SetEdited(DateTime? edited)
        {
            EditedTT = edited != null ? $"Изменено {edited.Value.ToString("G")}" : string.Empty;
            Edited = EditedTT != string.Empty ? "(изм.)" : string.Empty;
        }

        private void SetBackground(bool hasRead)
        {
            Background = HorizontalAlignment == HorizontalAlignmentRight
                ? hasRead ? new SolidColorBrush(Color.FromRgb(38, 48, 76)) : new SolidColorBrush(Color.FromRgb(44, 63, 112))
                : new SolidColorBrush(Color.FromRgb(38, 48, 76));
        }

        public void SetCanEditDelete(long messagesCount)
        {
            if (HorizontalAlignment == HorizontalAlignmentRight)
            {
                CanEditDelete = messagesCount == 1 && (DateTime.Now - DateTime.Parse(SendDateTime)) <= ServerSettings.EditDeleteTimeLimit;
            }
            else
            {
                CanEditDelete = false;
            }
        }

        public static void Respond(object listBox)
        {
            ((MessagesTabView)((ListBox)listBox).Parent!.Parent!.Parent!).Respond();
        }

        public static void Forward(object listBox)
        {
            ((MessagesTabView)((ListBox)listBox).Parent!.Parent!.Parent!).Forward();
        }

        public async void Copy()
        {
            if (Application.Current!.Clipboard != null)
            {
                await Application.Current!.Clipboard.SetTextAsync(Text);
            }
        }

        public static void Edit(object listBox)
        {
            ((MessagesTabView)((ListBox)listBox).Parent!.Parent!.Parent!).Edit();
        }

        public static void Delete(object listBox)
        {
            ((MessagesTabView)((ListBox)listBox).Parent!.Parent!.Parent!).Delete();
        }

        public void ShowAttachments(object listBox)
        {
            ((MessagesTabView)((ListBox)listBox).Parent!.Parent!.Parent!).ShowAttachments(Id);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
