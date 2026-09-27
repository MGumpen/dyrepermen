using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dyrepermen.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilVedleggPaBesok : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Tabellen har aldri fatt rader: ingen kode la inn dokumenter for
            // denne migrasjonen. Skulle det likevel finnes noen, pekte de pa
            // filer pa disk som Render har slettet for lengst - og de ville
            // brutt CHECK-vilkarene under, siden de nye kolonnene far tomme
            // verdier. Se ADR 0018.
            migrationBuilder.Sql("DELETE FROM dokument;");

            migrationBuilder.DropColumn(
                name: "filnavn",
                table: "dokument");

            migrationBuilder.AddColumn<string>(
                name: "innholdstype",
                table: "dokument",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "storrelse_byte",
                table: "dokument",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "vetbesok_id",
                table: "dokument",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "dokument_innhold",
                columns: table => new
                {
                    dokument_id = table.Column<int>(type: "integer", nullable: false),
                    data = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dokument_innhold", x => x.dokument_id);
                    table.ForeignKey(
                        name: "fk_dokument_innhold_dokument_dokument_id",
                        column: x => x.dokument_id,
                        principalTable: "dokument",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_dokument_vetbesok_id",
                table: "dokument",
                column: "vetbesok_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_dokument_innholdstype",
                table: "dokument",
                sql: "innholdstype IN ('image/jpeg','image/png','application/pdf')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_dokument_storrelse",
                table: "dokument",
                sql: "storrelse_byte > 0");

            migrationBuilder.AddForeignKey(
                name: "fk_dokument_vetbesok_vetbesok_id",
                table: "dokument",
                column: "vetbesok_id",
                principalTable: "vetbesok",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_dokument_vetbesok_vetbesok_id",
                table: "dokument");

            migrationBuilder.DropTable(
                name: "dokument_innhold");

            migrationBuilder.DropIndex(
                name: "ix_dokument_vetbesok_id",
                table: "dokument");

            migrationBuilder.DropCheckConstraint(
                name: "ck_dokument_innholdstype",
                table: "dokument");

            migrationBuilder.DropCheckConstraint(
                name: "ck_dokument_storrelse",
                table: "dokument");

            migrationBuilder.DropColumn(
                name: "innholdstype",
                table: "dokument");

            migrationBuilder.DropColumn(
                name: "storrelse_byte",
                table: "dokument");

            migrationBuilder.DropColumn(
                name: "vetbesok_id",
                table: "dokument");

            migrationBuilder.AddColumn<string>(
                name: "filnavn",
                table: "dokument",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }
    }
}
