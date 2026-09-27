using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dyrepermen.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilKjennetegnOgProfilbilde : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_dokument_kategori",
                table: "dokument");

            // Filnavn for bilder pa disk, fra planen. Aldri fylt ut - bildet
            // ligger na i dokument med kategori P. Se ADR 0018.
            migrationBuilder.DropColumn(
                name: "bilde_filnavn",
                table: "dyr");

            migrationBuilder.AddColumn<string>(
                name: "farge",
                table: "dyr",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "kjennetegn",
                table: "dyr",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_dokument_profilbilde",
                table: "dokument",
                column: "dyr_id",
                unique: true,
                filter: "kategori = 'P'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_dokument_kategori",
                table: "dokument",
                sql: "kategori IN ('V','J','K','A','P')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_dokument_profilbilde_er_bilde",
                table: "dokument",
                sql: "kategori <> 'P' OR innholdstype IN ('image/jpeg','image/png')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_dokument_profilbilde",
                table: "dokument");

            migrationBuilder.DropCheckConstraint(
                name: "ck_dokument_kategori",
                table: "dokument");

            migrationBuilder.DropCheckConstraint(
                name: "ck_dokument_profilbilde_er_bilde",
                table: "dokument");

            migrationBuilder.DropColumn(
                name: "farge",
                table: "dyr");

            migrationBuilder.DropColumn(
                name: "kjennetegn",
                table: "dyr");

            migrationBuilder.AddColumn<string>(
                name: "bilde_filnavn",
                table: "dyr",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_dokument_kategori",
                table: "dokument",
                sql: "kategori IN ('V','J','K','A')");
        }
    }
}
