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
                entity.HasKey(e => e.DatasetId);
                entity.Property(e => e.DatasetId)
                      .ValueGeneratedOnAdd(); // Auto-generate DatasetId

                entity.Property(e => e.DisplayId)
                      .IsRequired()
                      .ValueGeneratedNever(); // Manual input for DisplayId
            });
        }
    }
}