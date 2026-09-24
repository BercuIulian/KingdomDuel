using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KingdomDuel.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ZidulRamaneSpart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Kingdom_RuinedFrom",
                table: "player_slots",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Kingdom_RuinedFrom",
                table: "player_slots");
        }
    }
}
