using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pidar.data.pidar
{
    /// <inheritdoc />
    public partial class RemoveLinkToDataset1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LinkToDataset1",
                schema: "public",
                table: "dataset");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LinkToDataset1",
                schema: "public",
                table: "dataset",
                type: "text",
                nullable: true);
        }
    }
}
