namespace EnterpriseMessengerServer.Models
{
    public class Message
    {
        public long Id { get; set; }

        public string Text { get; set; }

        public string? AuthorId { get; set; }

        public string? ReceiverUserId { get; set; }

        public string? ReceiverChatId { get; set; }

        public DateTime SendDateTime { get; set; }

        public DateTime? EditDateTime { get; set; }

        public bool HasRead { get; set; }

        public List<MessageAttachment> Attachments { get; set; }

        #pragma warning disable CS8618
        public Message()
        {
            HasRead = false;
            Attachments = new List<MessageAttachment>();
        }
        #pragma warning restore CS8618
    }
}
