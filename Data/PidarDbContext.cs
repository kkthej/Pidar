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

            // Set the default schema for PostgreSQL
            modelBuilder.HasDefaultSchema("public");

            modelBuilder.Entity<Metadata>(entity =>
            {
                entity.ToTable("metadata"); // Explicit table name in lowercase

                entity.HasKey(e => e.DatasetId)
                    .HasName("pk_metadata");

                entity.Property(e => e.DatasetId)
                    .HasColumnName("dataset_id")
                    .ValueGeneratedOnAdd(); // Auto-generate DatasetId

                entity.Property(e => e.DisplayId)
                    .HasColumnName("display_id")
                    .IsRequired()
                    .ValueGeneratedNever(); // Manual input for DisplayId

                // Configure all string properties to use 'text' type in PostgreSQL
                var stringProperties = entity.Metadata.GetProperties()
                    .Where(p => p.ClrType == typeof(string));

                foreach (var property in stringProperties)
                {
                    entity.Property(property.Name)
                        .HasColumnType("text");
                }

                // Add index for DisplayId
                entity.HasIndex(e => e.DisplayId)
                    .HasDatabaseName("ix_metadata_display_id");
            });
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // This is just for design-time migrations
                optionsBuilder.UseNpgsql("Host=localhost;Database=pidar_db;Username=postgres;Password=yourpassword");
            }
        }
    }
}