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

        // MAIN TABLE
        public DbSet<Dataset> Datasets { get; set; } = null!;

        // CHILD TABLES (1:1)
        public DbSet<StudyDesign> StudyDesigns { get; set; } = null!;
        public DbSet<Publication> Publications { get; set; } = null!;
        public DbSet<StudyComponent> StudyComponents { get; set; } = null!;
        public DbSet<DatasetInfo> DatasetInfos { get; set; } = null!;
        public DbSet<InVivo> InVivos { get; set; } = null!;
        public DbSet<Procedures> Procedures { get; set; } = null!;
        public DbSet<ImageAcquisition> ImageAcquisitions { get; set; } = null!;
        public DbSet<ImageData> ImageDatas { get; set; } = null!;
        public DbSet<ImageCorrelation> ImageCorrelations { get; set; } = null!;
        public DbSet<Analyzed> Analyzed { get; set; } = null!;
        public DbSet<Ontology> Ontologies { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasDefaultSchema("public");

            // DATASET TABLE
            modelBuilder.Entity<Dataset>(entity =>
            {
                entity.ToTable("dataset");

                entity.HasKey(e => e.DatasetId)
                      .HasName("pk_dataset");

                entity.Property(e => e.DatasetId)
                      .HasColumnName("dataset_id")
                      .ValueGeneratedOnAdd();

                entity.Property(e => e.DisplayId)
                      .HasColumnName("display_id")
                      .IsRequired()
                      .ValueGeneratedNever();

                // Set all string fields to TEXT
                foreach (var p in entity.Metadata.GetProperties().Where(p => p.ClrType == typeof(string)))
                {
                    entity.Property(p.Name).HasColumnType("text");
                }

                entity.HasIndex(e => e.DisplayId)
                      .HasDatabaseName("ix_dataset_display_id");
            });

            // GENERIC 1:1 RELATIONS
            ConfigureOneToOne<StudyDesign>(modelBuilder, d => d.StudyDesign, "study_design");
            ConfigureOneToOne<Publication>(modelBuilder, d => d.Publication, "publication");
            ConfigureOneToOne<StudyComponent>(modelBuilder, d => d.StudyComponent, "study_component");
            ConfigureOneToOne<DatasetInfo>(modelBuilder, d => d.DatasetInfo, "dataset_info");
            ConfigureOneToOne<InVivo>(modelBuilder, d => d.InVivo, "in_vivo");
            ConfigureOneToOne<Procedures>(modelBuilder, d => d.Procedures, "procedures");
            ConfigureOneToOne<ImageAcquisition>(modelBuilder, d => d.ImageAcquisition, "image_acquisition");
            ConfigureOneToOne<ImageData>(modelBuilder, d => d.ImageData, "image_data");
            ConfigureOneToOne<ImageCorrelation>(modelBuilder, d => d.ImageCorrelation, "image_correlation");
            ConfigureOneToOne<Analyzed>(modelBuilder, d => d.Analyzed, "analyzed");
            ConfigureOneToOne<Ontology>(modelBuilder, d => d.Ontology, "ontology");
        }

        /// <summary>
        /// CLEAN & SAFE 1:1 RELATION MAPPING
        /// </summary>
        private void ConfigureOneToOne<TChild>(
            ModelBuilder modelBuilder,
            System.Linq.Expressions.Expression<Func<Dataset, TChild?>> navigation,
            string tableName
        ) where TChild : class
        {
            // Child table
            var child = modelBuilder.Entity<TChild>();
            child.ToTable(tableName);

            // Child PK = DatasetId
            child.HasKey("DatasetId");

            // 1:1 relation
            modelBuilder.Entity<Dataset>()
                .HasOne(navigation)
                .WithOne()
                .HasForeignKey<TChild>("DatasetId")
                .OnDelete(DeleteBehavior.Cascade);
        }

        // RUNTIME FALLBACK FOR EF TOOLS
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql(
                    "Host=localhost;Database=pidar_db;Username=postgres;Password=password"
                );
            }
        }
    }
}
