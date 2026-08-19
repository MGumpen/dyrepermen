using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dyrepermen.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Apner ck_behandling_type for 'A' - behandlingstypen Annet.
    ///
    /// Ingen kolonne endres, og ingen eksisterende rad brytes: V, O, F, K og T
    /// er fortsatt gyldige. Down feiler hvis noen allerede har registrert en
    /// Annet-behandling, og det er riktig - da finnes det data vilkaret ville
    /// avvist.
    /// </summary>
    public partial class LeggTilBehandlingstypeAnnet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_behandling_type",
                table: "behandling");

            migrationBuilder.AddCheckConstraint(
                name: "ck_behandling_type",
                table: "behandling",
                sql: "type IN ('V','O','F','K','T','A')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_behandling_type",
                table: "behandling");

            migrationBuilder.AddCheckConstraint(
                name: "ck_behandling_type",
                table: "behandling",
                sql: "type IN ('V','O','F','K','T')");
        }
    }
}
