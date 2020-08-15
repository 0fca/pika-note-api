using Microsoft.EntityFrameworkCore;

namespace PikaNoteAPI.Data
{
    public class MainDbContext : DbContext
    {
        public MainDbContext(DbContextOptions<MainDbContext> options)
            : base(options)
        {
        }
        
        public DbSet<Note> Notes { get; set; }
        public DbSet<PermaLinkReference> PermaLinkReferences { get; set; }
    }
}