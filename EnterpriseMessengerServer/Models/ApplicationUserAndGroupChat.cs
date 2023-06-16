using Microsoft.EntityFrameworkCore;

namespace EnterpriseMessengerServer.Models
{
    #pragma warning disable CS8618
    [PrimaryKey(nameof(GroupChatId), nameof(ParticipantId))]
    public class ApplicationUserAndGroupChat
    {
        public string GroupChatId { get; set; }

        public GroupChat GroupChat { get; set; }

        public string ParticipantId { get; set; }

        public ApplicationUser Participant { get; set; }

        public long? LastReadMessageId { get; set; }

        public Message? LastReadMessage { get; set; }
    }
    #pragma warning restore CS8618
}
