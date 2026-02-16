using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Pidar.Migrations
{
    /// <inheritdoc />
    public partial class AddOntologySearchTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "dataset_ontology_term",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DatasetId = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dataset_ontology_term", x => x.Id);
                    table.ForeignKey(
                        name: "FK_dataset_ontology_term_dataset_DatasetId",
                        column: x => x.DatasetId,
                        principalSchema: "public",
                        principalTable: "dataset",
                        principalColumn: "dataset_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ontology_synonym",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Synonym = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ontology_synonym", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_dataset_ontology_term_code",
                schema: "public",
                table: "dataset_ontology_term",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "ux_dataset_ontology_term_dataset_category_code",
                schema: "public",
                table: "dataset_ontology_term",
                columns: new[] { "DatasetId", "Category", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ontology_synonym_code",
                schema: "public",
                table: "ontology_synonym",
                column: "Code");

            migrationBuilder.Sql(@"CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            migrationBuilder.Sql(@"
CREATE INDEX IF NOT EXISTS ix_ontology_synonym_synonym_trgm
ON public.ontology_synonym
USING gin (""Synonym"" gin_trgm_ops);
");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ix_ontology_synonym_synonym_trgm;");

            migrationBuilder.DropTable(
                name: "dataset_ontology_term",
                schema: "public");

            migrationBuilder.DropTable(
                name: "ontology_synonym",
                schema: "public");

           

        }
    }
}
