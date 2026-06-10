using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pidar.Migrations
{
    /// <inheritdoc />
    public partial class RenameDatasetInfo_PiOrchid_To_PiOrcid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PiOrchid",
                schema: "public",
                table: "dataset_info",
                newName: "PiOrcid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PiOrcid",
                schema: "public",
                table: "dataset_info",
                newName: "PiOrchid");
        }
    }
}
