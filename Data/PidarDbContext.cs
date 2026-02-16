using Microsoft.EntityFrameworkCore;
using Pidar.Models;
using Pidar.Models.Ontology;
using System.Linq;


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
        public DbSet<DatasetOntologyTerm> DatasetOntologyTerms { get; set; } = null!;
        public DbSet<OntologySynonym> OntologySynonyms { get; set; } = null!;


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
            // --------------------
            // ONTOLOGY SEARCH TABLES
            // --------------------
            modelBuilder.Entity<DatasetOntologyTerm>(entity =>
            {
                entity.ToTable("dataset_ontology_term");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Category).HasColumnType("text").IsRequired();
                entity.Property(e => e.Code).HasColumnType("text").IsRequired();

                entity.HasOne(e => e.Dataset)
                    .WithMany() // no navigation collection required
                    .HasForeignKey(e => e.DatasetId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.Code)
                    .HasDatabaseName("ix_dataset_ontology_term_code");

                entity.HasIndex(e => new { e.DatasetId, e.Category, e.Code })
                    .IsUnique()
                    .HasDatabaseName("ux_dataset_ontology_term_dataset_category_code");
            });

            modelBuilder.Entity<OntologySynonym>(entity =>
            {
                entity.ToTable("ontology_synonym");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Code).HasColumnType("text").IsRequired();
                entity.Property(e => e.Synonym).HasColumnType("text").IsRequired();

                entity.HasIndex(e => e.Code)
                    .HasDatabaseName("ix_ontology_synonym_code");
            });

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

            // Set all string columns to TEXT for child tables too
            foreach (var p in child.Metadata.GetProperties().Where(p => p.ClrType == typeof(string)))
            {
                child.Property(p.Name).HasColumnType("text");
            }


            // Child PK = DatasetId
            child.HasKey("DatasetId");

            // 1:1 relation
            modelBuilder.Entity<Dataset>()
                .HasOne(navigation)
                .WithOne("Dataset")
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
