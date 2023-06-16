using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnterpriseMessengerServer.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Surname { get; set; }

        public string Name { get; set; }

        public string Patronymic { get; set; }

        public string Position { get; set; }

        public bool HasPhoto { get; set; }

        public string? Information { get; set; }

        public bool HasAccess { get; set; }

        [ForeignKey("AuthorId")]
        public List<Message> SentMessages { get; set; }

        [ForeignKey("ReceiverUserId")]
        public List<Message> ReceivedMessages { get; set; }

        public List<GroupChat> GroupChatsAuthor { get; set; }

        [InverseProperty("Owners")]
        public List<Note> Notes { get; set; }

        [InverseProperty("Author")]
        public List<Note> NotesAuthor { get; set; }

        #pragma warning disable CS8618
        public ApplicationUser()
        {
            SentMessages = new List<Message>();
            ReceivedMessages = new List<Message>();
            GroupChatsAuthor = new List<GroupChat>();
            Notes = new List<Note>();
            NotesAuthor = new List<Note>();

            HasPhoto = false;
            HasAccess = true;
        }
        #pragma warning restore CS8618
    }
}
