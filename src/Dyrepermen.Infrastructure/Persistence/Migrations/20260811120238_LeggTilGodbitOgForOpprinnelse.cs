using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dyrepermen.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilGodbitOgForOpprinnelse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "varsler_aktiv",
                table: "husstand_innstilling",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            // EF foreslo false her, fordi det er CLR-standarden for bool.
            // Det ville slatt godbitknappen AV for alle husstander som
            // allerede finnes - en funksjon de aldri har tatt stilling til.
            // Standardverdien i Domain er true, og migrasjonen skal si det
            // samme.
            migrationBuilder.AddColumn<bool>(
                name: "godbitlogg_aktiv",
                table: "husstand_innstilling",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "fornavn",
                table: "foring",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            // EF foreslo '\0' - NUL-tegnet. Da ville hver eneste eksisterende
            // foringsrad fatt en ugyldig type, og CHECK-vilkaret rett under
            // ville avvist migrasjonen pa enhver database med data i.
            //
            // Alt som er logget for dette var et maltid. Godbiter fantes ikke
            // som begrep enna, sa 'M' er ikke en gjetning - det er det eneste
            // de kan ha vaert.
            migrationBuilder.AddColumn<char>(
                name: "type",
                table: "foring",
                type: "char(1)",
                nullable: false,
                defaultValue: 'M');

            // Ma komme ETTER at kolonnen er fylt. Motsatt rekkefolge ville
            // avvist sine egne rader.
            migrationBuilder.AddCheckConstraint(
                name: "ck_foring_type",
                table: "foring",
                sql: "type IN ('M','G')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_foring_type",
                table: "foring");

            migrationBuilder.DropColumn(
                name: "godbitlogg_aktiv",
                table: "husstand_innstilling");

            migrationBuilder.DropColumn(
                name: "fornavn",
                table: "foring");

            migrationBuilder.DropColumn(
                name: "type",
                table: "foring");

            migrationBuilder.AlterColumn<bool>(
                name: "varsler_aktiv",
                table: "husstand_innstilling",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");
        }
    }
}
