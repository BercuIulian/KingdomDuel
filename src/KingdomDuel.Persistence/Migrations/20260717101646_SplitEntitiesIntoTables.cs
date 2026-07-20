using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KingdomDuel.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SplitEntitiesIntoTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "History",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "Player1",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "Player2",
                table: "matches");

            migrationBuilder.AddColumn<Guid>(
                name: "Player1Id",
                table: "matches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Player2Id",
                table: "matches",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "player_slots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_slots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "round_results",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoundNumber = table.Column<int>(type: "integer", nullable: false),
                    Event = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Player1Action = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Player2Action = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Log = table.Column<List<string>>(type: "text[]", nullable: false),
                    GameOverReason = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_round_results", x => x.Id);
                    table.ForeignKey(
                        name: "FK_round_results_matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "kingdoms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Gold = table.Column<int>(type: "integer", nullable: false),
                    Troops = table.Column<int>(type: "integer", nullable: false),
                    Morale = table.Column<int>(type: "integer", nullable: false),
                    DefenseStreak = table.Column<int>(type: "integer", nullable: false),
                    PlayerSlotId = table.Column<Guid>(type: "uuid", nullable: true)
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
                name: "IX_matches_Player1Id",
                table: "matches",
                column: "Player1Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_matches_Player2Id",
                table: "matches",
                column: "Player2Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_kingdoms_PlayerSlotId",
                table: "kingdoms",
                column: "PlayerSlotId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_round_results_MatchId",
                table: "round_results",
                column: "MatchId");

            migrationBuilder.AddForeignKey(
                name: "FK_matches_player_slots_Player1Id",
                table: "matches",
                column: "Player1Id",
                principalTable: "player_slots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_matches_player_slots_Player2Id",
                table: "matches",
                column: "Player2Id",
                principalTable: "player_slots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_matches_player_slots_Player1Id",
                table: "matches");

            migrationBuilder.DropForeignKey(
                name: "FK_matches_player_slots_Player2Id",
                table: "matches");

            migrationBuilder.DropTable(
                name: "kingdoms");

            migrationBuilder.DropTable(
                name: "round_results");

            migrationBuilder.DropTable(
                name: "player_slots");

            migrationBuilder.DropIndex(
                name: "IX_matches_Player1Id",
                table: "matches");

            migrationBuilder.DropIndex(
                name: "IX_matches_Player2Id",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "Player1Id",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "Player2Id",
                table: "matches");

            migrationBuilder.AddColumn<string>(
                name: "History",
                table: "matches",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Player1",
                table: "matches",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Player2",
                table: "matches",
                type: "jsonb",
                nullable: true);
        }
    }
}
