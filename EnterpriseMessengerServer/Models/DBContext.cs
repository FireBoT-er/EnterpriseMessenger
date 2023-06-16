using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseMessengerServer.Models
{
    public class DBContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<Message> Messages { get; set; }
        public DbSet<MessageAttachment> MessageAttachments { get; set; }
        public DbSet<GroupChat> GroupChats { get; set; }
        public DbSet<ApplicationUserAndGroupChat> ApplicationUserAndGroupChat { get; set; }
        public DbSet<Note> Notes { get; set; }
        public DbSet<NoteSubPoint> NoteSubPoints { get; set; }

        public DBContext(DbContextOptions<DBContext> options) : base(options) { }
    }
}
