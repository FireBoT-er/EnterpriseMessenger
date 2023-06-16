using Avalonia;
using Avalonia.Controls;
using EnterpriseMessengerUI.Views;
using System;

namespace EnterpriseMessengerUI.Models
{
    public enum MessageAttachmentType
    {
        File,
        Message
    }

    public class AttachmentModel
    {
        public Guid Guid { get; }

        public long? Id { get; }

        public CornerRadius PictureFileNameCornerRadius { get; }

        public string PictureFileName { get; }

        public string AdditionalData { get; }

        public string SendDateTime { get; }

        public string DataText { get; }

        public MessageAttachmentType Type { get; }

        public string Data { get; }

        public bool IsDownloadVisible { get; }

        public bool CanDownload { get; } = false;

        #pragma warning disable CS8618
        public AttachmentModel(string pictureFileName, string additionalData, string sendDateTime, string dataText, MessageAttachmentType type, string data)
        #pragma warning restore CS8618
        {
            Guid = Guid.NewGuid();

            switch (type)
            {
                case MessageAttachmentType.File:
                    PictureFileNameCornerRadius = new CornerRadius(0);
                    PictureFileName = "/Assets/blank-page.png";
                    SendDateTime = string.Empty;
                    DataText = "Файл";
                    IsDownloadVisible = true;
                    break;
                case MessageAttachmentType.Message:
                    PictureFileNameCornerRadius = new CornerRadius(180);
                    PictureFileName = pictureFileName;
                    SendDateTime = sendDateTime;
                    DataText = string.IsNullOrEmpty(dataText) ? "[Вложения]" : dataText;
                    IsDownloadVisible = false;
                    break;
            }

            AdditionalData = additionalData;
            Type = type;
            Data = data;
        }

        public AttachmentModel(string additionalData, MessageAttachmentType type, string data) : this(string.Empty, additionalData, string.Empty, string.Empty, type, data) { }

        public AttachmentModel(long id, string pictureFileName, string additionalData, string sendDateTime, string dataText, MessageAttachmentType type, string data) : this(pictureFileName, additionalData, sendDateTime, dataText, type, data)
        {
            Id = id;
        }

        public AttachmentModel(long id, string additionalData, MessageAttachmentType type, string data) : this(id, string.Empty, additionalData, string.Empty, string.Empty, type, data) { }

        public AttachmentModel(string additionalData, string data, bool canDownload) : this(string.Empty, additionalData, string.Empty, string.Empty, MessageAttachmentType.File, data)
        {
            CanDownload = canDownload;
        }

        public void RemoveAttachment(object listBox)
        {
            ((MessagesTabView)((ListBox)listBox).Parent!.Parent!.Parent!.Parent!).RemoveAttachment(this);
        }

        public void Download(object window)
        {
            ((ShowAttachmentsWindow)window).Download(Data, AdditionalData);
        }
    }
}
