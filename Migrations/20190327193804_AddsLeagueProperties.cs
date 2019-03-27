using Microsoft.EntityFrameworkCore.Migrations;

namespace MockStockBackend.Migrations
{
    public partial class AddsLeagueProperties : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LeagueCreationDate",
                table: "Leagues",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LeagueHost",
                table: "Leagues",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LeagueName",
                table: "Leagues",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LeagueCreationDate",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "LeagueHost",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "LeagueName",
                table: "Leagues");
        }
    }
}
