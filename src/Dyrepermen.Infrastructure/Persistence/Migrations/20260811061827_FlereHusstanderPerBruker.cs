using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Dyrepermen.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FlereHusstanderPerBruker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // MERK REKKEFOLGEN. EF genererte denne migrasjonen med
            // DropColumn husstand_id FORST, altsa for tabellen som skal
            // overta dataene i det hele tatt fantes. Da ville hver eneste
            // eksisterende brukers husstandstilknytning forsvunnet.
            //
            // Rekkefolgen her er derfor: opprett tabellen, flytt dataene,
            // og slipp kolonnen til slutt.

            migrationBuilder.AddColumn<char>(
                name: "rolle",
                table: "husstand_invitasjon",
                type: "char(1)",
                nullable: false,
                defaultValue: 'G');

            migrationBuilder.CreateTable(
                name: "husstandsmedlemskap",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    husstand_id = table.Column<int>(type: "integer", nullable: false),
                    bruker_id = table.Column<int>(type: "integer", nullable: false),
                    rolle = table.Column<char>(type: "char(1)", nullable: false),
                    opprettet_dato = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "CURRENT_DATE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_husstandsmedlemskap", x => x.id);
                    table.CheckConstraint("ck_medlemskap_rolle", "rolle IN ('E','G')");
                    table.ForeignKey(
                        name: "fk_husstandsmedlemskap_husstand_husstand_id",
                        column: x => x.husstand_id,
                        principalTable: "husstand",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_husstandsmedlemskap_users_bruker_id",
                        column: x => x.bruker_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_medlemskap_bruker",
                table: "husstandsmedlemskap",
                column: "bruker_id");

            migrationBuilder.CreateIndex(
                name: "ux_medlemskap_husstand_bruker",
                table: "husstandsmedlemskap",
                columns: new[] { "husstand_id", "bruker_id" },
                unique: true);

            // Flytt eksisterende tilknytninger over. Alle som hadde en
            // husstand fra for blir eiere av den - de var de eneste
            // medlemmene, og noen ma kunne endre innstillingene.
            migrationBuilder.Sql("""
                INSERT INTO husstandsmedlemskap (husstand_id, bruker_id, rolle)
                SELECT husstand_id, id, 'E'
                FROM asp_net_users
                WHERE husstand_id IS NOT NULL;
                """);

            // Forst na er kolonnen trygg a fjerne.
            migrationBuilder.DropForeignKey(
                name: "fk_asp_net_users_husstand_husstand_id",
                table: "asp_net_users");

            migrationBuilder.DropIndex(
                name: "ix_bruker_husstand",
                table: "asp_net_users");

            migrationBuilder.DropColumn(
                name: "husstand_id",
                table: "asp_net_users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "husstandsmedlemskap");

            migrationBuilder.DropColumn(
                name: "rolle",
                table: "husstand_invitasjon");

            migrationBuilder.AddColumn<int>(
                name: "husstand_id",
                table: "asp_net_users",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_bruker_husstand",
                table: "asp_net_users",
                column: "husstand_id");

            migrationBuilder.AddForeignKey(
                name: "fk_asp_net_users_husstand_husstand_id",
                table: "asp_net_users",
                column: "husstand_id",
                principalTable: "husstand",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
