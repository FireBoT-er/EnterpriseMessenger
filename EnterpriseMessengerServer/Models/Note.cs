namespace EnterpriseMessengerServer.Models
{
    public class Note
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public bool IsChecked { get; set; }

        public string? AuthorId { get; set; }

        public ApplicationUser? Author { get; set; }

        public List<NoteSubPoint> SubPoints { get; set; }

        public List<ApplicationUser> Owners { get; set; }

        #pragma warning disable CS8618
        public Note()
        {
            SubPoints = new List<NoteSubPoint>();
            Owners = new List<ApplicationUser>();
        }
        #pragma warning restore CS8618
    }
}
