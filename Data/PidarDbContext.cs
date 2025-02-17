using Microsoft.EntityFrameworkCore;
using Pidar.Models;

namespace Pidar.Data
{
    public class PidarDbContext : DbContext
    {
        public PidarDbContext(DbContextOptions<PidarDbContext> options)
            : base(options)
        {
        }

        public DbSet<Metadata> Metadata { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Metadata>(entity =>
            {
                entity.HasKey(e => e.DatasetId); // Explicit primary key
                entity.Property(e => e.DatasetId)
                      .ValueGeneratedNever(); // Disable auto-increment
            });
        }
    }
}