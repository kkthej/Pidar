using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Pidar.Migrations
{
    /// <inheritdoc />
    /// Adds the history of contributions and the contribution assistant:
    ///   - submission_change: who changed what in a submission, field by field (with the source of the
    ///     new value: typed, suggested by the assistant, Excel import) plus status changes.
    ///   - assistant_message: the conversation with the assistant per submission (and token usage).
    /// Both only add tables; nothing existing is changed.
    public partial class AddSubmissionHistoryAndAssistant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "submission_change",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SubmissionId = table.Column<int>(type: "integer", nullable: false),
                    At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    UserEmail = table.Column<string>(type: "text", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    FieldPath = table.Column<string>(type: "text", nullable: true),
                    OldValue = table.Column<string>(type: "text", nullable: true),
                    NewValue = table.Column<string>(type: "text", nullable: true),
                    Source = table.Column<string>(type: "text", nullable: true),
                    Note = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_submission_change", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_submission_change_submission_id",
                schema: "public",
                table: "submission_change",
                column: "SubmissionId");

            migrationBuilder.CreateTable(
                name: "assistant_message",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SubmissionId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    ProposalsJson = table.Column<string>(type: "jsonb", nullable: true),
                    Model = table.Column<string>(type: "text", nullable: true),
                    InputTokens = table.Column<int>(type: "integer", nullable: true),
                    OutputTokens = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assistant_message", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_assistant_message_submission_id",
                schema: "public",
                table: "assistant_message",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "ix_assistant_message_user_created",
                schema: "public",
                table: "assistant_message",
                columns: new[] { "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assistant_message",
                schema: "public");

            migrationBuilder.DropTable(
                name: "submission_change",
                schema: "public");
        }
    }
}
