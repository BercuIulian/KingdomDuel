using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KingdomDuel.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LinkPlayerSlotsToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Meciurile existente se șterg, nu se migrează: sloturile lor n-au cont cu care
            // să umple noul UserId, iar aplicația n-a fost publicată, deci nu există date de
            // păstrat. Ordinea contează — FK-ul e pe "matches", cu ON DELETE CASCADE, deci
            // ștergerea sloturilor duce și meciurile (plus "kingdoms") cu ea; al doilea DELETE
            // prinde meciurile rămase fără slot și, prin cascadă, "round_results".
            migrationBuilder.Sql("DELETE FROM player_slots;");
            migrationBuilder.Sql("DELETE FROM matches;");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "player_slots");

            migrationBuilder.DropColumn(
                name: "PlayerId",
                table: "player_slots");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "player_slots",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_player_slots_UserId",
                table: "player_slots",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_player_slots_users_UserId",
                table: "player_slots",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_player_slots_users_UserId",
                table: "player_slots");

            migrationBuilder.DropIndex(
                name: "IX_player_slots_UserId",
                table: "player_slots");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "player_slots");

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "player_slots",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PlayerId",
                table: "player_slots",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");
        }
    }
}
