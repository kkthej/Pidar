using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pidar.Migrations
{
    /// <inheritdoc />
    public partial class AddedReagentProcedures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NameOfReagentS",
                schema: "public",
                table: "procedures",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NameOfReagentS",
                schema: "public",
                table: "procedures");
        }
    }
}
