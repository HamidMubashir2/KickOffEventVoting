using KickOffEvent.Models;
using Microsoft.EntityFrameworkCore;

namespace KickOffEventVoting.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<AppUser> Users { get; set; }
        public DbSet<VotingSession> VotingSessions { get; set; }

        public DbSet<Vote> Votes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ✅ Rule: one user can cast only ONE Male + ONE Female vote per session
            modelBuilder.Entity<Vote>()
                .HasIndex(v => new { v.SessionId, v.VoterUserId, v.Category })
                .IsUnique();

            modelBuilder.Entity<Vote>()
                .Property(v => v.VoterName)
                .HasMaxLength(200);

            modelBuilder.Entity<Vote>()
                .Property(v => v.CandidateName)
                .HasMaxLength(200);

            modelBuilder.Entity<Vote>()
                .Property(v => v.Category)
                .HasMaxLength(10);
        }
    }

}
