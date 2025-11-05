using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pidar.data.pidar
{
    /// <inheritdoc />
    public partial class AddMissingDatasetFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CellCultureMedium",
                schema: "public",
                table: "dataset",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CellInjectionProcedure",
                schema: "public",
                table: "dataset",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegistrationAlgorithms",
                schema: "public",
                table: "dataset",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VirusLabelledOrModified",
                schema: "public",
                table: "dataset",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CellCultureMedium",
                schema: "public",
                table: "dataset");

            migrationBuilder.DropColumn(
                name: "CellInjectionProcedure",
                schema: "public",
                table: "dataset");

            migrationBuilder.DropColumn(
                name: "RegistrationAlgorithms",
                schema: "public",
                table: "dataset");

            migrationBuilder.DropColumn(
                name: "VirusLabelledOrModified",
                schema: "public",
                table: "dataset");
        }
    }
}
