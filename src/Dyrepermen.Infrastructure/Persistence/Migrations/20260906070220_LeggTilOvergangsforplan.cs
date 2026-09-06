using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Dyrepermen.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilOvergangsforplan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_forplan_metode",
                table: "forplan");

            migrationBuilder.DropCheckConstraint(
                name: "ck_forplan_verdi",
                table: "forplan");

            migrationBuilder.AddColumn<string>(
                name: "fornavn_torr",
                table: "forplan",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "rafor_andel_prosent",
                table: "forplan",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "forplantrinn",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    forplan_id = table.Column<int>(type: "integer", nullable: false),
                    alder_mnd = table.Column<int>(type: "integer", nullable: false),
                    gram_per_dag = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_forplantrinn", x => x.id);
                    table.CheckConstraint("ck_forplantrinn_alder", "alder_mnd BETWEEN 0 AND 240");
                    table.CheckConstraint("ck_forplantrinn_gram", "gram_per_dag BETWEEN 1 AND 20000");
                    table.ForeignKey(
                        name: "fk_forplantrinn_forplan_forplan_id",
                        column: x => x.forplan_id,
                        principalTable: "forplan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_forplan_metode",
                table: "forplan",
                sql: "metode IN ('P','G','B')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_forplan_verdi",
                table: "forplan",
                sql: "   (metode = 'P' AND prosent_tidels IS NOT NULL\n                 AND prosent_tidels BETWEEN 1 AND 300\n                 AND gram_per_dag IS NULL\n                 AND rafor_andel_prosent IS NULL)\nOR (metode = 'G' AND gram_per_dag IS NOT NULL\n                 AND gram_per_dag > 0\n                 AND prosent_tidels IS NULL\n                 AND rafor_andel_prosent IS NULL)\nOR (metode = 'B' AND prosent_tidels IS NOT NULL\n                 AND prosent_tidels BETWEEN 1 AND 300\n                 AND gram_per_dag IS NULL\n                 AND rafor_andel_prosent IS NOT NULL\n                 AND rafor_andel_prosent BETWEEN 0 AND 100)");

            migrationBuilder.CreateIndex(
                name: "ux_forplantrinn_alder",
                table: "forplantrinn",
                columns: new[] { "forplan_id", "alder_mnd" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "forplantrinn");

            migrationBuilder.DropCheckConstraint(
                name: "ck_forplan_metode",
                table: "forplan");

            migrationBuilder.DropCheckConstraint(
                name: "ck_forplan_verdi",
                table: "forplan");

            migrationBuilder.DropColumn(
                name: "fornavn_torr",
                table: "forplan");

            migrationBuilder.DropColumn(
                name: "rafor_andel_prosent",
                table: "forplan");

            migrationBuilder.AddCheckConstraint(
                name: "ck_forplan_metode",
                table: "forplan",
                sql: "metode IN ('P','G')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_forplan_verdi",
                table: "forplan",
                sql: "   (metode = 'P' AND prosent_tidels IS NOT NULL\n                 AND prosent_tidels BETWEEN 1 AND 300\n                 AND gram_per_dag IS NULL)\nOR (metode = 'G' AND gram_per_dag IS NOT NULL\n                 AND gram_per_dag > 0\n                 AND prosent_tidels IS NULL)");
        }
    }
}
