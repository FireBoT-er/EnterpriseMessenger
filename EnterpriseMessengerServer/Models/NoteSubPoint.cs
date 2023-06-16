namespace EnterpriseMessengerServer.Models
{
#pragma warning disable CS8618
    public class NoteSubPoint
    {
        public long Id { get; set; }

        public long? NoteId { get; set; }

        public Note? Note { get; set; }

        public string Text { get; set; }

        public bool IsChecked { get; set; }
    }
    #pragma warning restore CS8618
}
