using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KingdomDuel.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ClearPendingOrdersForFeast : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Documentul ordinului își schimbă forma: alocarea trece sub o cheie
            // proprie, lângă ospăț. Ordinele scrise în forma veche nu se mai pot citi,
            // așa că le golim — un jucător prins la mijlocul unei runde o reia cu
            // ordinul nedat, în loc să se rupă meciul.
            migrationBuilder.Sql("""UPDATE player_slots SET "PendingOrder" = NULL WHERE "PendingOrder" IS NOT NULL;""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
