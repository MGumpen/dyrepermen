using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dyrepermen.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Metoden 'B' (blanding) blir 'T' (tabell), og tabellmetoden slutter a
    /// kreve en raforregel den ikke bruker.
    ///
    /// Rekkefolgen er ikke tilfeldig: begge vilkarene ma vaere borte for
    /// radene skrives om, ellers avviser det gamle ck_forplan_metode hver
    /// eneste UPDATE - og de nye legges pa etterpa, ellers avviser de nye de
    /// radene som enda star pa 'B'.
    /// </summary>
    public partial class TabellmetodeUtenRafor : Migration
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

            // Metodetegnet skrives om for de nye vilkarene legges pa.
            migrationBuilder.Sql("UPDATE forplan SET metode = 'T' WHERE metode = 'B';");

            // En plan som sto pa 0 % rafor hadde likevel en prosentsats, fordi
            // det gamle vilkaret krevde en. Den er ubrukt, og det nye vilkaret
            // krever at den er null - ellers ville hver eneste slik rad blitt
            // avvist i det vilkaret ble lagt pa.
            migrationBuilder.Sql(
                "UPDATE forplan SET prosent_tidels = NULL "
                + "WHERE metode = 'T' AND rafor_andel_prosent = 0;");

            migrationBuilder.AddCheckConstraint(
                name: "ck_forplan_metode",
                table: "forplan",
                sql: "metode IN ('P','G','T')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_forplan_verdi",
                table: "forplan",
                sql: "   (metode = 'P' AND prosent_tidels IS NOT NULL\n                 AND prosent_tidels BETWEEN 1 AND 300\n                 AND gram_per_dag IS NULL\n                 AND rafor_andel_prosent IS NULL)\nOR (metode = 'G' AND gram_per_dag IS NOT NULL\n                 AND gram_per_dag > 0\n                 AND prosent_tidels IS NULL\n                 AND rafor_andel_prosent IS NULL)\nOR (metode = 'T' AND gram_per_dag IS NULL\n                 AND rafor_andel_prosent IS NOT NULL\n                 AND rafor_andel_prosent BETWEEN 0 AND 100\n                 AND ((rafor_andel_prosent = 0\n                       AND prosent_tidels IS NULL)\n                   OR (rafor_andel_prosent > 0\n                       AND prosent_tidels IS NOT NULL\n                       AND prosent_tidels BETWEEN 1 AND 300)))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_forplan_metode",
                table: "forplan");

            migrationBuilder.DropCheckConstraint(
                name: "ck_forplan_verdi",
                table: "forplan");

            migrationBuilder.Sql("UPDATE forplan SET metode = 'B' WHERE metode = 'T';");

            // Det gamle vilkaret krever en prosentsats ogsa nar andelen er 0.
            // Uten en verdi her ville tilbakerullingen brutt pa egne rader, og
            // 0 tidels prosent er ikke lovlig heller - 1 er laveste verdi
            // vilkaret godtar, og satsen er uansett ubrukt ved 0 % rafor.
            migrationBuilder.Sql(
                "UPDATE forplan SET prosent_tidels = 1 "
                + "WHERE metode = 'B' AND prosent_tidels IS NULL;");

            migrationBuilder.AddCheckConstraint(
                name: "ck_forplan_metode",
                table: "forplan",
                sql: "metode IN ('P','G','B')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_forplan_verdi",
                table: "forplan",
                sql: "   (metode = 'P' AND prosent_tidels IS NOT NULL\n                 AND prosent_tidels BETWEEN 1 AND 300\n                 AND gram_per_dag IS NULL\n                 AND rafor_andel_prosent IS NULL)\nOR (metode = 'G' AND gram_per_dag IS NOT NULL\n                 AND gram_per_dag > 0\n                 AND prosent_tidels IS NULL\n                 AND rafor_andel_prosent IS NULL)\nOR (metode = 'B' AND prosent_tidels IS NOT NULL\n                 AND prosent_tidels BETWEEN 1 AND 300\n                 AND gram_per_dag IS NULL\n                 AND rafor_andel_prosent IS NOT NULL\n                 AND rafor_andel_prosent BETWEEN 0 AND 100)");
        }
    }
}
