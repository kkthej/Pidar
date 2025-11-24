using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pidar.Migrations
{
    /// <inheritdoc />
    public partial class AddDisplayIdUniqueConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.CreateIndex(
    name: "IX_Datasets_DisplayId",
    table: "dataset",
    column: "display_id",
    unique: true);


        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
