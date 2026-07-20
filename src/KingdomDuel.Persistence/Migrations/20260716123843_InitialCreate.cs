using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KingdomDuel.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "matches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Round = table.Column<int>(type: "integer", nullable: false),
                    CurrentEvent = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    GameOverReason = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PendingActions = table.Column<string>(type: "jsonb", nullable: false),
                    History = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Player1 = table.Column<string>(type: "jsonb", nullable: true),
                    Player2 = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_matches", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_matches_Code",
                table: "matches",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_matches_Status",
                table: "matches",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "matches");
        }
    }
}
