using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dyrepermen.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilDemoUtloper : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "demo_utloper",
                table: "asp_net_users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_users_demo_utloper",
                table: "asp_net_users",
                column: "demo_utloper");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_asp_net_users_demo_utloper",
                table: "asp_net_users");

            migrationBuilder.DropColumn(
                name: "demo_utloper",
                table: "asp_net_users");
        }
    }
}
