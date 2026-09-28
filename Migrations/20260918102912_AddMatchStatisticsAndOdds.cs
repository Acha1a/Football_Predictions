using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootballPredictor.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchStatisticsAndOdds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AwayCorners",
                table: "Matches",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AwayOdds",
                table: "Matches",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AwayShots",
                table: "Matches",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AwayShotsOnTarget",
                table: "Matches",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DrawOdds",
                table: "Matches",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HomeCorners",
                table: "Matches",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HomeOdds",
                table: "Matches",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HomeShots",
                table: "Matches",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HomeShotsOnTarget",
                table: "Matches",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AwayCorners",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "AwayOdds",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "AwayShots",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "AwayShotsOnTarget",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "DrawOdds",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "HomeCorners",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "HomeOdds",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "HomeShots",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "HomeShotsOnTarget",
                table: "Matches");
        }
    }
}
