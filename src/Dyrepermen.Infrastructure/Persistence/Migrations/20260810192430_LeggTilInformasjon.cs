using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Dyrepermen.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilInformasjon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "informasjon",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    husstand_id = table.Column<int>(type: "integer", nullable: false),
                    dyr_id = table.Column<int>(type: "integer", nullable: true),
                    tittel = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    tekst = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    opprettet_av_bruker_id = table.Column<int>(type: "integer", nullable: true),
                    opprettet_dato = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "CURRENT_DATE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_informasjon", x => x.id);
                    table.ForeignKey(
                        name: "fk_informasjon_asp_net_users_opprettet_av_bruker_id",
                        column: x => x.opprettet_av_bruker_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_informasjon_dyr_dyr_id",
                        column: x => x.dyr_id,
                        principalTable: "dyr",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_informasjon_husstand_husstand_id",
                        column: x => x.husstand_id,
                        principalTable: "husstand",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_informasjon_dyr_id",
                table: "informasjon",
                column: "dyr_id");

            migrationBuilder.CreateIndex(
                name: "ix_informasjon_husstand",
                table: "informasjon",
                column: "husstand_id");

            migrationBuilder.CreateIndex(
                name: "ix_informasjon_opprettet_av_bruker_id",
                table: "informasjon",
                column: "opprettet_av_bruker_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "informasjon");
        }
    }
}
