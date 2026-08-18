using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dyrepermen.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ApningstidPerUkedag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "apningstider",
                table: "veterinar");

            migrationBuilder.AddColumn<string>(
                name: "apent_fredag",
                table: "veterinar",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "apent_lordag",
                table: "veterinar",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "apent_mandag",
                table: "veterinar",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "apent_onsdag",
                table: "veterinar",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "apent_sondag",
                table: "veterinar",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "apent_tirsdag",
                table: "veterinar",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "apent_torsdag",
                table: "veterinar",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "apent_fredag",
                table: "veterinar");

            migrationBuilder.DropColumn(
                name: "apent_lordag",
                table: "veterinar");

            migrationBuilder.DropColumn(
                name: "apent_mandag",
                table: "veterinar");

            migrationBuilder.DropColumn(
                name: "apent_onsdag",
                table: "veterinar");

            migrationBuilder.DropColumn(
                name: "apent_sondag",
                table: "veterinar");

            migrationBuilder.DropColumn(
                name: "apent_tirsdag",
                table: "veterinar");

            migrationBuilder.DropColumn(
                name: "apent_torsdag",
                table: "veterinar");

            migrationBuilder.AddColumn<string>(
                name: "apningstider",
                table: "veterinar",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }
    }
}
