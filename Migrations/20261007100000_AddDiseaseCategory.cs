using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pidar.Migrations
{
    /// <inheritdoc />
    /// Adds "Disease Category" (broad class, chosen from a list) above "Disease Model",
    /// plus its DOID code in the ontology table.
    public partial class AddDiseaseCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DiseaseCategory",
                schema: "public",
                table: "in_vivo",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DoidDiseaseCategory",
                schema: "public",
                table: "ontology",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiseaseCategory",
                schema: "public",
                table: "in_vivo");

            migrationBuilder.DropColumn(
                name: "DoidDiseaseCategory",
                schema: "public",
                table: "ontology");
        }
    }
}
