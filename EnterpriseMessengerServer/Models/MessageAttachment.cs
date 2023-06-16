namespace EnterpriseMessengerServer.Models
{
    public enum MessageAttachmentType
    {
        File,
        Message
    }

    #pragma warning disable CS8618
    public class MessageAttachment
    {
        public long Id { get; set; }

        public long? MessageId { get; set; }

        public Message? Message { get; set; }

        public MessageAttachmentType Type { get; set; }

        public string Data { get; set; }

        public string? AdditionalData { get; set; }

        public bool CanDownload { get; set; }

        public MessageAttachment()
        {
            CanDownload = false;
        }
    }
    #pragma warning restore CS8618
}
