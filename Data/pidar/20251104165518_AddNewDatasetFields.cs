using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pidar.data.pidar
{
    /// <inheritdoc />
    public partial class AddNewDatasetFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AiEnhanced",
                schema: "public",
                table: "dataset",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AnimalCondition",
                schema: "public",
                table: "dataset",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FunderId",
                schema: "public",
                table: "dataset",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageAttenuationCorrection",
                schema: "public",
                table: "dataset",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PerfusionMethod",
                schema: "public",
                table: "dataset",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PiOrchid",
                schema: "public",
                table: "dataset",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RorCodeOwner",
                schema: "public",
                table: "dataset",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpecimenThickness",
                schema: "public",
                table: "dataset",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TissueDescription",
                schema: "public",
                table: "dataset",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TissuePerfused",
                schema: "public",
                table: "dataset",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiEnhanced",
                schema: "public",
                table: "dataset");

            migrationBuilder.DropColumn(
                name: "AnimalCondition",
                schema: "public",
                table: "dataset");

            migrationBuilder.DropColumn(
                name: "FunderId",
                schema: "public",
                table: "dataset");

            migrationBuilder.DropColumn(
                name: "ImageAttenuationCorrection",
                schema: "public",
                table: "dataset");

            migrationBuilder.DropColumn(
                name: "PerfusionMethod",
                schema: "public",
                table: "dataset");

            migrationBuilder.DropColumn(
                name: "PiOrchid",
                schema: "public",
                table: "dataset");

            migrationBuilder.DropColumn(
                name: "RorCodeOwner",
                schema: "public",
                table: "dataset");

            migrationBuilder.DropColumn(
                name: "SpecimenThickness",
                schema: "public",
                table: "dataset");

            migrationBuilder.DropColumn(
                name: "TissueDescription",
                schema: "public",
                table: "dataset");

            migrationBuilder.DropColumn(
                name: "TissuePerfused",
                schema: "public",
                table: "dataset");
        }
    }
}
