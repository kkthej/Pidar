using Microsoft.EntityFrameworkCore;
using Pidar.Models;
using Pidar.Models.Ontology;
using Pidar.Models.Submissions;
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

        // Contributions from data providers (Contribute page). Separate from the dataset tables:
        // a dataset is only created from one when an Admin/Curator approves it.
        public DbSet<DatasetSubmission> DatasetSubmissions { get; set; } = null!;

        // Who changed what in a submission (field edits with their source, status changes)
        public DbSet<SubmissionChange> SubmissionChanges { get; set; } = null!;

        // Conversation with the contribution assistant, per submission
        public DbSet<AssistantMessage> AssistantMessages { get; set; } = null!;


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

            // --------------------
            // CONTRIBUTIONS (data provider submissions)
            // --------------------
            modelBuilder.Entity<DatasetSubmission>(entity =>
            {
                entity.ToTable("dataset_submission");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.OwnerUserId).HasColumnType("text").IsRequired();
                entity.Property(e => e.OwnerEmail).HasColumnType("text").IsRequired();
                entity.Property(e => e.Status).HasColumnType("text").IsRequired();
                entity.Property(e => e.PayloadJson).HasColumnType("jsonb").IsRequired();
                entity.Property(e => e.ReviewerComment).HasColumnType("text");
                entity.Property(e => e.ReviewedBy).HasColumnType("text");

                entity.HasIndex(e => e.OwnerUserId)
                    .HasDatabaseName("ix_dataset_submission_owner_user_id");

                entity.HasIndex(e => e.Status)
                    .HasDatabaseName("ix_dataset_submission_status");
            });

            modelBuilder.Entity<SubmissionChange>(entity =>
            {
                entity.ToTable("submission_change");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.UserId).HasColumnType("text").IsRequired();
                entity.Property(e => e.UserEmail).HasColumnType("text").IsRequired();
                entity.Property(e => e.Kind).HasColumnType("text").IsRequired();
                entity.Property(e => e.FieldPath).HasColumnType("text");
                entity.Property(e => e.OldValue).HasColumnType("text");
                entity.Property(e => e.NewValue).HasColumnType("text");
                entity.Property(e => e.Source).HasColumnType("text");
                entity.Property(e => e.Note).HasColumnType("text");

                entity.HasIndex(e => e.SubmissionId)
                    .HasDatabaseName("ix_submission_change_submission_id");
            });

            modelBuilder.Entity<AssistantMessage>(entity =>
            {
                entity.ToTable("assistant_message");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.UserId).HasColumnType("text").IsRequired();
                entity.Property(e => e.Role).HasColumnType("text").IsRequired();
                entity.Property(e => e.Text).HasColumnType("text").IsRequired();
                entity.Property(e => e.ProposalsJson).HasColumnType("jsonb");
                entity.Property(e => e.Model).HasColumnType("text");

                entity.HasIndex(e => e.SubmissionId)
                    .HasDatabaseName("ix_assistant_message_submission_id");

                // daily limit per user: count of the user's messages in the last 24 h
                entity.HasIndex(e => new { e.UserId, e.CreatedAt })
                    .HasDatabaseName("ix_assistant_message_user_created");
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
