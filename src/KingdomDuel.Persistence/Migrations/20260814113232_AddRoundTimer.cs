using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KingdomDuel.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRoundTimer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MissedRounds",
                table: "player_slots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RoundDeadline",
                table: "matches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RoundSeconds",
                table: "matches",
                type: "integer",
                nullable: false,
                defaultValue: 45);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MissedRounds",
                table: "player_slots");

            migrationBuilder.DropColumn(
                name: "RoundDeadline",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "RoundSeconds",
                table: "matches");
        }
    }
}
