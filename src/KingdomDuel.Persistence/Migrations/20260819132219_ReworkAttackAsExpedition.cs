using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KingdomDuel.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReworkAttackAsExpedition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Player1AttackTroops",
                table: "round_results",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Player2AttackTroops",
                table: "round_results",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Ordinele în așteptare își schimbă forma: dintr-un simplu nume de acțiune
            // devin obiecte cu numărul de trupe trimise. Cele scrise în formatul vechi
            // nu se mai pot citi, așa că le golim — jucătorii prinși la mijlocul unei
            // runde o reiau cu ordinele nedate, în loc să se rupă meciul.
            migrationBuilder.Sql(
                """UPDATE matches SET "PendingActions" = '{}'::jsonb WHERE "PendingActions" <> '{}'::jsonb;""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Player1AttackTroops",
                table: "round_results");

            migrationBuilder.DropColumn(
                name: "Player2AttackTroops",
                table: "round_results");
        }
    }
}
