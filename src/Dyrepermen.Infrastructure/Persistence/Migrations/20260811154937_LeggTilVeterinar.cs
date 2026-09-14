using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Dyrepermen.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilVeterinar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_vetbesok_dyr_id",
                table: "vetbesok");

            migrationBuilder.AlterColumn<int>(
                name: "kostnad_kr",
                table: "vetbesok",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "klokkeslett",
                table: "vetbesok",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "neste_kontroll_dato",
                table: "vetbesok",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "notat",
                table: "vetbesok",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "refundert_kr",
                table: "vetbesok",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "veterinar_id",
                table: "vetbesok",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "veterinar",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    husstand_id = table.Column<int>(type: "integer", nullable: false),
                    navn = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    type = table.Column<char>(type: "char(1)", nullable: false),
                    telefon = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    adresse = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    nettside = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    epost = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    apningstider = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    notat = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    opprettet_dato = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "CURRENT_DATE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_veterinar", x => x.id);
                    table.CheckConstraint("ck_veterinar_telefon", "telefon IS NULL OR length(btrim(telefon)) BETWEEN 3 AND 20");
                    table.CheckConstraint("ck_veterinar_type", "type IN ('F','V','S','A')");
                    table.ForeignKey(
                        name: "fk_veterinar_husstand_husstand_id",
                        column: x => x.husstand_id,
                        principalTable: "husstand",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_vetbesok_dyr_dato",
                table: "vetbesok",
                columns: new[] { "dyr_id", "dato" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_vetbesok_veterinar_id",
                table: "vetbesok",
                column: "veterinar_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_vetbesok_kostnad",
                table: "vetbesok",
                sql: "kostnad_kr IS NULL OR kostnad_kr >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_vetbesok_refundert",
                table: "vetbesok",
                sql: "refundert_kr IS NULL OR refundert_kr >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_vetbesok_refusjon_krever_krav",
                table: "vetbesok",
                sql: "refundert_kr IS NULL OR forsikring_krevd");

            migrationBuilder.CreateIndex(
                name: "ix_veterinar_husstand_navn",
                table: "veterinar",
                columns: new[] { "husstand_id", "navn" });

            migrationBuilder.AddForeignKey(
                name: "fk_vetbesok_veterinar_veterinar_id",
                table: "vetbesok",
                column: "veterinar_id",
                principalTable: "veterinar",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_vetbesok_veterinar_veterinar_id",
                table: "vetbesok");

            migrationBuilder.DropTable(
                name: "veterinar");

            migrationBuilder.DropIndex(
                name: "ix_vetbesok_dyr_dato",
                table: "vetbesok");

            migrationBuilder.DropIndex(
                name: "ix_vetbesok_veterinar_id",
                table: "vetbesok");

            migrationBuilder.DropCheckConstraint(
                name: "ck_vetbesok_kostnad",
                table: "vetbesok");

            migrationBuilder.DropCheckConstraint(
                name: "ck_vetbesok_refundert",
                table: "vetbesok");

            migrationBuilder.DropCheckConstraint(
                name: "ck_vetbesok_refusjon_krever_krav",
                table: "vetbesok");

            migrationBuilder.DropColumn(
                name: "klokkeslett",
                table: "vetbesok");

            migrationBuilder.DropColumn(
                name: "neste_kontroll_dato",
                table: "vetbesok");

            migrationBuilder.DropColumn(
                name: "notat",
                table: "vetbesok");

            migrationBuilder.DropColumn(
                name: "refundert_kr",
                table: "vetbesok");

            migrationBuilder.DropColumn(
                name: "veterinar_id",
                table: "vetbesok");

            migrationBuilder.AlterColumn<int>(
                name: "kostnad_kr",
                table: "vetbesok",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_vetbesok_dyr_id",
                table: "vetbesok",
                column: "dyr_id");
        }
    }
}
