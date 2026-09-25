using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KingdomDuel.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReworkRoundsAsAllocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Meciurile de dinainte nu se pot converti: ordinele, cronica și starea
            // regatelor își schimbă toate forma deodată. Le ștergem o singură dată, în
            // loc să scriem cod de compatibilitate pentru o formă care oricum dispare.
            // Istoricul (match_records) nu depinde de meciuri și rămâne întreg.
            migrationBuilder.Sql("DELETE FROM matches;");
            migrationBuilder.Sql("DELETE FROM player_slots;");

            migrationBuilder.DropTable(
                name: "kingdoms");

            migrationBuilder.DropColumn(
                name: "Log",
                table: "round_results");

            migrationBuilder.DropColumn(
                name: "Player1Action",
                table: "round_results");

            migrationBuilder.DropColumn(
                name: "Player2Action",
                table: "round_results");

            migrationBuilder.DropColumn(
                name: "PendingActions",
                table: "matches");

            migrationBuilder.AddColumn<string>(
                name: "Events",
                table: "round_results",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "Player1",
                table: "round_results",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<string>(
                name: "Player2",
                table: "round_results",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<int>(
                name: "Gold",
                table: "player_slots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Morale",
                table: "player_slots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PendingOrder",
                table: "player_slots",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Troops",
                table: "player_slots",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Events",
                table: "round_results");

            migrationBuilder.DropColumn(
                name: "Player1",
                table: "round_results");

            migrationBuilder.DropColumn(
                name: "Player2",
                table: "round_results");

            migrationBuilder.DropColumn(
                name: "Gold",
                table: "player_slots");

            migrationBuilder.DropColumn(
                name: "Morale",
                table: "player_slots");

            migrationBuilder.DropColumn(
                name: "PendingOrder",
                table: "player_slots");

            migrationBuilder.DropColumn(
                name: "Troops",
                table: "player_slots");

            migrationBuilder.AddColumn<List<string>>(
                name: "Log",
                table: "round_results",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "Player1Action",
                table: "round_results",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Player2Action",
                table: "round_results",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PendingActions",
                table: "matches",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.CreateTable(
                name: "kingdoms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DefenseStreak = table.Column<int>(type: "integer", nullable: false),
                    Gold = table.Column<int>(type: "integer", nullable: false),
                    Morale = table.Column<int>(type: "integer", nullable: false),
                    PlayerSlotId = table.Column<Guid>(type: "uuid", nullable: true),
                    Troops = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kingdoms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_kingdoms_player_slots_PlayerSlotId",
                        column: x => x.PlayerSlotId,
                        principalTable: "player_slots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_kingdoms_PlayerSlotId",
                table: "kingdoms",
                column: "PlayerSlotId",
                unique: true);
        }
    }
}
