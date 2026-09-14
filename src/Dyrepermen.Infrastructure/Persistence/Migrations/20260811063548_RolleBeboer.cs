using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dyrepermen.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RolleBeboer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_medlemskap_rolle",
                table: "husstandsmedlemskap");

            // Rollen Eier er dopt om til Beboer, og bokstaven fra 'E' til 'B'.
            //
            // REKKEFOLGEN ER IKKE VALGFRI. Kjores denne for constrainten er
            // sluppet, bryter den den gamle regelen rolle IN ('E','G').
            // Kjores den etter at den NYE er lagt pa, bryter de gamle radene
            // rolle IN ('B','G'). Den ma sta noyaktig her, imellom.
            migrationBuilder.Sql("UPDATE husstandsmedlemskap SET rolle = 'B' WHERE rolle = 'E';");
            migrationBuilder.Sql("UPDATE husstand_invitasjon SET rolle = 'B' WHERE rolle = 'E';");

            migrationBuilder.AlterColumn<char>(
                name: "rolle",
                table: "husstand_invitasjon",
                type: "char(1)",
                nullable: false,
                oldClrType: typeof(char),
                oldType: "char(1)",
                oldDefaultValue: 'G');

            migrationBuilder.AddCheckConstraint(
                name: "ck_medlemskap_rolle",
                table: "husstandsmedlemskap",
                sql: "rolle IN ('B','G')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_medlemskap_rolle",
                table: "husstandsmedlemskap");

            // Tilbake til 'E'. Speilvendt av Up.
            migrationBuilder.Sql("UPDATE husstandsmedlemskap SET rolle = 'E' WHERE rolle = 'B';");
            migrationBuilder.Sql("UPDATE husstand_invitasjon SET rolle = 'E' WHERE rolle = 'B';");

            migrationBuilder.AlterColumn<char>(
                name: "rolle",
                table: "husstand_invitasjon",
                type: "char(1)",
                nullable: false,
                defaultValue: 'G',
                oldClrType: typeof(char),
                oldType: "char(1)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_medlemskap_rolle",
                table: "husstandsmedlemskap",
                sql: "rolle IN ('E','G')");
        }
    }
}
