using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dyrepermen.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UtvidForsikring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "egenandel",
                table: "forsikring",
                newName: "forsikringsbelop_kr");

            migrationBuilder.AlterColumn<string>(
                name: "polise_nr",
                table: "forsikring",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40);

            migrationBuilder.AddColumn<int>(
                name: "egenandel_fast_kr",
                table: "forsikring",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "egenandel_variabel_tidels",
                table: "forsikring",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_forsikring_fornyes",
                table: "forsikring",
                column: "fornyes_dato",
                filter: "fornyes_dato IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_forsikring_variabel",
                table: "forsikring",
                sql: "egenandel_variabel_tidels BETWEEN 0 AND 1000");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_forsikring_fornyes",
                table: "forsikring");

            migrationBuilder.DropCheckConstraint(
                name: "ck_forsikring_variabel",
                table: "forsikring");

            migrationBuilder.DropColumn(
                name: "egenandel_fast_kr",
                table: "forsikring");

            migrationBuilder.DropColumn(
                name: "egenandel_variabel_tidels",
                table: "forsikring");

            migrationBuilder.RenameColumn(
                name: "forsikringsbelop_kr",
                table: "forsikring",
                newName: "egenandel");

            migrationBuilder.AlterColumn<string>(
                name: "polise_nr",
                table: "forsikring",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldNullable: true);
        }
    }
}
