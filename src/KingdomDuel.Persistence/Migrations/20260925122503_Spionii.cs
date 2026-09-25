using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KingdomDuel.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Spionii : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Report",
                table: "player_slots",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Spies",
                table: "player_slots",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Report",
                table: "player_slots");

            migrationBuilder.DropColumn(
                name: "Spies",
                table: "player_slots");
        }
    }
}
