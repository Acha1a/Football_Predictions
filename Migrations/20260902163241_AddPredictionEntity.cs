using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootballPredictor.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPredictionEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Predictions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MatchId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    HomeProbability = table.Column<decimal>(type: "TEXT", nullable: false),
                    DrawProbability = table.Column<decimal>(type: "TEXT", nullable: false),
                    AwayProbability = table.Column<decimal>(type: "TEXT", nullable: false),
                    Confidence = table.Column<int>(type: "INTEGER", nullable: false),
                    RiskLevel = table.Column<string>(type: "TEXT", nullable: false),
                    PredictedOutcome = table.Column<string>(type: "TEXT", nullable: false),
                    ActualOutcome = table.Column<string>(type: "TEXT", nullable: true),
                    IsCorrect = table.Column<bool>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Predictions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Predictions_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Predictions_MatchId",
                table: "Predictions",
                column: "MatchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Predictions");
        }
    }
}
