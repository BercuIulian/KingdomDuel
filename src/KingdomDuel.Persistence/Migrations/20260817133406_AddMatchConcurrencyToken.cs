using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KingdomDuel.Persistence.Migrations
{
    /// <summary>
    /// Migrare fără schimbare de schemă, păstrată doar ca snapshot-ul modelului să
    /// rămână sincronizat.
    ///
    /// `xmin` e coloană de sistem în Postgres, prezentă pe orice tabel; maparea ei
    /// ca token de concurență trăiește exclusiv în model. Codul generat automat
    /// aici era un `AddColumn`, care ar fi eșuat la aplicare cu
    /// „column name xmin conflicts with a system column name”.
    /// </summary>
    public partial class AddMatchConcurrencyToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
