using Microsoft.EntityFrameworkCore;
using ReminderWeb.Models;

namespace ReminderWeb.Data
{
    public class ReminderDbContext : DbContext
    {
        public ReminderDbContext(DbContextOptions<ReminderDbContext> options)
            : base(options)
        {
        }

        public DbSet<Reminder> Reminders { get; set; }

        public DbSet<ReminderZone> ReminderZones { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ReminderZone>()
                .HasOne<Reminder>()
                .WithMany(r => r.Zones)
                .HasForeignKey(z => z.ReminderId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}