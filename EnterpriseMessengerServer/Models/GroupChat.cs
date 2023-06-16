using System.ComponentModel.DataAnnotations.Schema;

namespace EnterpriseMessengerServer.Models
{
    public class GroupChat
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public bool HasPhoto { get; set; }

        public string? AuthorId { get; set; }

        public ApplicationUser? Author { get; set; }

        [ForeignKey("ReceiverChatId")]
        public List<Message> Messages { get; set; }

        #pragma warning disable CS8618
        public GroupChat()
        {
            Messages = new List<Message>();
            HasPhoto = false;
        }
        #pragma warning restore CS8618
    }
}
