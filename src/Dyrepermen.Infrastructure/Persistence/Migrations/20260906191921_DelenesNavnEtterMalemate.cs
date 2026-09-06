using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dyrepermen.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Kolonnene navngis etter hvordan mengden MALES, ikke etter hva foret
    /// er. En overgang gar ikke bare fra rafor til torrfor - den kan ga
    /// motsatt vei, og mellom to torrfor.
    ///
    /// rafor_andel_prosent blir vektdel_andel_prosent, fornavn_torr blir
    /// fornavn_alder. Vilkaret ma slippes for kolonnen kan dopes om, og
    /// legges pa igjen etterpa med det nye navnet.
    /// </summary>
    public partial class DelenesNavnEtterMalemate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_forplan_verdi",
                table: "forplan");

            migrationBuilder.RenameColumn(
                name: "rafor_andel_prosent",
                table: "forplan",
                newName: "vektdel_andel_prosent");

            migrationBuilder.RenameColumn(
                name: "fornavn_torr",
                table: "forplan",
                newName: "fornavn_alder");

            migrationBuilder.AddCheckConstraint(
                name: "ck_forplan_verdi",
                table: "forplan",
                sql: "   (metode = 'P' AND prosent_tidels IS NOT NULL\n                 AND prosent_tidels BETWEEN 1 AND 300\n                 AND gram_per_dag IS NULL\n                 AND vektdel_andel_prosent IS NULL)\nOR (metode = 'G' AND gram_per_dag IS NOT NULL\n                 AND gram_per_dag > 0\n                 AND prosent_tidels IS NULL\n                 AND vektdel_andel_prosent IS NULL)\nOR (metode = 'T' AND gram_per_dag IS NULL\n                 AND vektdel_andel_prosent IS NOT NULL\n                 AND vektdel_andel_prosent BETWEEN 0 AND 100\n                 AND ((vektdel_andel_prosent = 0\n                       AND prosent_tidels IS NULL)\n                   OR (vektdel_andel_prosent > 0\n                       AND prosent_tidels IS NOT NULL\n                       AND prosent_tidels BETWEEN 1 AND 300)))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_forplan_verdi",
                table: "forplan");

            migrationBuilder.RenameColumn(
                name: "vektdel_andel_prosent",
                table: "forplan",
                newName: "rafor_andel_prosent");

            migrationBuilder.RenameColumn(
                name: "fornavn_alder",
                table: "forplan",
                newName: "fornavn_torr");

            migrationBuilder.AddCheckConstraint(
                name: "ck_forplan_verdi",
                table: "forplan",
                sql: "   (metode = 'P' AND prosent_tidels IS NOT NULL\n                 AND prosent_tidels BETWEEN 1 AND 300\n                 AND gram_per_dag IS NULL\n                 AND rafor_andel_prosent IS NULL)\nOR (metode = 'G' AND gram_per_dag IS NOT NULL\n                 AND gram_per_dag > 0\n                 AND prosent_tidels IS NULL\n                 AND rafor_andel_prosent IS NULL)\nOR (metode = 'T' AND gram_per_dag IS NULL\n                 AND rafor_andel_prosent IS NOT NULL\n                 AND rafor_andel_prosent BETWEEN 0 AND 100\n                 AND ((rafor_andel_prosent = 0\n                       AND prosent_tidels IS NULL)\n                   OR (rafor_andel_prosent > 0\n                       AND prosent_tidels IS NOT NULL\n                       AND prosent_tidels BETWEEN 1 AND 300)))");
        }
    }
}
