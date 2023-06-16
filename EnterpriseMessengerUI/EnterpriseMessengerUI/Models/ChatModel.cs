using Avalonia;
using System;
using System.ComponentModel;

namespace EnterpriseMessengerUI.Models
{
    public enum NetworkStatus
    {
        Online,
        Busy,
        Offline
    }

    public class ChatModel : INotifyPropertyChanged
    {
        public string ChatId { get; }

        private string _PhotoFileName;
        public string PhotoFileName
        {
            get { return _PhotoFileName; }
            private set
            {
                if (_PhotoFileName != value)
                {
                    _PhotoFileName = value;
                    RaisePropertyChanged(nameof(PhotoFileName));
                }
            }
        }

        private string _Surname;
        public string Surname
        {
            get { return _Surname; }
            set
            {
                if (_Surname != value)
                {
                    _Surname = value;
                    RaisePropertyChanged(nameof(Surname));
                }
            }
        }

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

        private string _Patronymic;
        public string Patronymic
        {
            get { return _Patronymic; }
            set
            {
                if (_Patronymic != value)
                {
                    _Patronymic = value;
                    RaisePropertyChanged(nameof(Patronymic));
                }
            }
        }

        private string _Position;
        public string Position
        {
            get { return _Position; }
            set
            {
                if (_Position != value)
                {
                    _Position = value;
                    RaisePropertyChanged(nameof(Position));
                }
            }
        }

        private string? _Information;
        public string? Information
        {
            get { return _Information; }
            set
            {
                if (_Information != value)
                {
                    _Information = value;
                    RaisePropertyChanged(nameof(Information));
                }
            }
        }

        private bool _HasAccess;
        public bool HasAccess
        {
            get { return _HasAccess; }
            set
            {
                if (_HasAccess != value)
                {
                    _HasAccess = value;
                    RaisePropertyChanged(nameof(HasAccess));
                }
            }
        }

        private string _NetworkStatusIcon;
        public string NetworkStatusIcon
        {
            get { return _NetworkStatusIcon; }
            private set
            {
                if (_NetworkStatusIcon != value)
                {
                    _NetworkStatusIcon = value;
                    RaisePropertyChanged(nameof(NetworkStatusIcon));
                }
            }
        }

        private string _NetworkStatusTT;
        public string NetworkStatusTT
        {
            get { return _NetworkStatusTT; }
            private set
            {
                if (_NetworkStatusTT != value)
                {
                    _NetworkStatusTT = value;
                    RaisePropertyChanged(nameof(NetworkStatusTT));
                }
            }
        }

        public long LastMessageId { get; set; }

        private string _LastMessageText;
        public string LastMessageText
        {
            get { return _LastMessageText; }
            set
            {
                if (_LastMessageText != value)
                {
                    _LastMessageText = value;
                    RaisePropertyChanged(nameof(LastMessageText));
                }
            }
        }

        private Thickness _LastMessageMargin;
        public Thickness LastMessageMargin
        {
            get { return _LastMessageMargin; }
            private set
            {
                if (_LastMessageMargin != value)
                {
                    _LastMessageMargin = value;
                    RaisePropertyChanged(nameof(LastMessageMargin));
                }
            }
        }

        private string _LastMessageSendDateTime;
        public string LastMessageSendDateTime
        {
            get { return _LastMessageSendDateTime; }
            private set
            {
                if (_LastMessageSendDateTime != value)
                {
                    _LastMessageSendDateTime = value;
                    RaisePropertyChanged(nameof(LastMessageSendDateTime));
                }
            }
        }

        private string _ReadStatus;
        public string ReadStatus
        {
            get { return _ReadStatus; }
            set
            {
                if (_ReadStatus != value)
                {
                    _ReadStatus = value;
                    ReadStatusRelatedPropertiesSet(value);
                    RaisePropertyChanged(nameof(ReadStatus));
                }
            }
        }

        private double _ReadStatusHeight;
        public double ReadStatusHeight
        {
            get { return _ReadStatusHeight; }
            private set
            {
                if (_ReadStatusHeight != value)
                {
                    _ReadStatusHeight = value;
                    RaisePropertyChanged(nameof(ReadStatusHeight));
                }
            }
        }

        private Thickness _ReadStatusMargin;
        public Thickness ReadStatusMargin
        {
            get { return _ReadStatusMargin; }
            private set
            {
                if (_ReadStatusMargin != value)
                {
                    _ReadStatusMargin = value;
                    RaisePropertyChanged(nameof(ReadStatusMargin));
                }
            }
        }

        public bool IsGroupChat { get; }

        public string GroupChatAuthorId { get; }

        public bool IsGroupChatAuthor { get; }

        public Thickness ShareChatBPadding { get; }

        public Thickness ChatParticipantsBPadding { get; }

        #pragma warning disable CS8618
        public ChatModel(string userId, bool hasPhoto, string surname, string name, string patronymic, string position, string? information, bool hasAccess, NetworkStatus networkStatus, long lastMessageId, string lastMessageText, DateTime? lastMessageSendDateTime, string readStatus)
        #pragma warning restore CS8618
        {
            ChatId = userId;
            PhotoFileNameSet(hasPhoto);
            Surname = surname;
            Name = name;
            Patronymic = patronymic;
            Position = position;
            Information = information;
            HasAccess = hasAccess;
            NetworkStatusIconSet(networkStatus);
            LastMessageId = lastMessageId;
            LastMessageText = lastMessageText;
            LastMessageSendDateTimeFromDateTime(lastMessageSendDateTime);
            ReadStatus = readStatus;
            ReadStatusRelatedPropertiesSet(readStatus);

            IsGroupChat = false;
            GroupChatAuthorId = string.Empty;
            IsGroupChatAuthor = false;
            ShareChatBPadding = new Thickness(8, 5, 8, 6);
            ChatParticipantsBPadding = new Thickness(8, 5, 8, 6);
        }

        #pragma warning disable CS8618
        public ChatModel(string chatId, bool hasPhoto, string name, string groupChatAuthorId, long lastMessageId, string lastMessageText, DateTime? lastMessageSendDateTime, string readStatus)
        #pragma warning restore CS8618
        {
            IsGroupChat = true;
            Position = "Групповой чат";

            ChatId = chatId;
            PhotoFileNameSet(hasPhoto);
            Surname = name;
            GroupChatAuthorId = groupChatAuthorId;
            IsGroupChatAuthor = groupChatAuthorId == CurrentUserModel.Id!;
            ShareChatBPadding = IsGroupChatAuthor ? new Thickness(4, 5, 4, 6) : new Thickness(8, 5, 4, 6);
            ChatParticipantsBPadding = IsGroupChatAuthor ? new Thickness(4, 5, 8, 6) : new Thickness(4, 5, 4, 6);
            LastMessageId = lastMessageId;
            LastMessageText = lastMessageText;
            LastMessageSendDateTimeFromDateTime(lastMessageSendDateTime);
            ReadStatus = readStatus;
            ReadStatusRelatedPropertiesSet(readStatus);

            Name = string.Empty;
            Patronymic = string.Empty;
            Information = null;
            HasAccess = true;
            NetworkStatusIconSet(NetworkStatus.Offline);
        }

        public void PhotoFileNameSet(bool hasPhoto)
        {
            PhotoFileName = !IsGroupChat
                ? hasPhoto ? $"{ServerSettings.ServerAddress}/UserFiles/Avatars/{ChatId}.png" : "/Assets/user.png"
                : hasPhoto ? $"{ServerSettings.ServerAddress}/UserFiles/GroupChatAvatars/{ChatId}.png" : "/Assets/group.png";
        }

        public void NetworkStatusIconSet(NetworkStatus networkStatus)
        {
            switch (networkStatus)
            {
                case NetworkStatus.Online:
                    NetworkStatusIcon = "/Assets/new-moon-green.png";
                    NetworkStatusTT = "Пользователь в сети";
                    break;
                case NetworkStatus.Busy:
                    NetworkStatusIcon = "/Assets/new-moon-yellow.png";
                    NetworkStatusTT = "Пользователь занят";
                    break;
                case NetworkStatus.Offline:
                    NetworkStatusIcon = "/Assets/empty.png";
                    NetworkStatusTT = !IsGroupChat ? "Пользователь не в сети" : "Групповой чат";
                    break;
            }
        }

        public void LastMessageSendDateTimeFromDateTime(DateTime? lastMessageSendDateTime)
        {
            if (lastMessageSendDateTime != null)
            {
                DateTime dateTime = lastMessageSendDateTime.GetValueOrDefault();
                LastMessageSendDateTime = dateTime.Date == DateTime.Now.Date ? dateTime.ToShortTimeString() : (dateTime.Year == DateTime.Now.Year ? dateTime.ToString("dd MMM") : dateTime.ToString("dd MMM yyyy"));
            }
            else
            {
                LastMessageSendDateTime = string.Empty;
            }
        }

        private void ReadStatusRelatedPropertiesSet(string readStatus)
        {
            LastMessageMargin = string.IsNullOrEmpty(readStatus) ? new Thickness(0, 3, 5, 3) : new Thickness(0, 0, 5, 0);
            ReadStatusHeight = string.IsNullOrWhiteSpace(readStatus) ? 10 : double.NaN;
            ReadStatusMargin = string.IsNullOrWhiteSpace(readStatus) ? new Thickness(0, 6.5, 0, 6) : new Thickness(0);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
