using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Dyrepermen.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilBehandlingskur : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "antall_dager",
                table: "behandling",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ganger_per_dag",
                table: "behandling",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "behandling_dose",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    behandling_id = table.Column<int>(type: "integer", nullable: false),
                    gitt_tid = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    gitt_av_bruker_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_behandling_dose", x => x.id);
                    table.ForeignKey(
                        name: "fk_behandling_dose_asp_net_users_gitt_av_bruker_id",
                        column: x => x.gitt_av_bruker_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_behandling_dose_behandling_behandling_id",
                        column: x => x.behandling_id,
                        principalTable: "behandling",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_behandling_antall_dager",
                table: "behandling",
                sql: "antall_dager >= 1");

            migrationBuilder.AddCheckConstraint(
                name: "ck_behandling_ganger_per_dag",
                table: "behandling",
                sql: "ganger_per_dag >= 1");

            migrationBuilder.CreateIndex(
                name: "ix_behandling_dose_behandling_tid",
                table: "behandling_dose",
                columns: new[] { "behandling_id", "gitt_tid" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_behandling_dose_gitt_av_bruker_id",
                table: "behandling_dose",
                column: "gitt_av_bruker_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "behandling_dose");

            migrationBuilder.DropCheckConstraint(
                name: "ck_behandling_antall_dager",
                table: "behandling");

            migrationBuilder.DropCheckConstraint(
                name: "ck_behandling_ganger_per_dag",
                table: "behandling");

            migrationBuilder.DropColumn(
                name: "antall_dager",
                table: "behandling");

            migrationBuilder.DropColumn(
                name: "ganger_per_dag",
                table: "behandling");
        }
    }
}
