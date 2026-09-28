using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootballPredictor.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPredictionFactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PredictionFactors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PredictionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<decimal>(type: "TEXT", nullable: false),
                    Weight = table.Column<decimal>(type: "TEXT", nullable: false),
                    Contribution = table.Column<decimal>(type: "TEXT", nullable: false),
                    Explanation = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PredictionFactors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PredictionFactors_Predictions_PredictionId",
                        column: x => x.PredictionId,
                        principalTable: "Predictions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PredictionFactors_PredictionId",
                table: "PredictionFactors",
                column: "PredictionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PredictionFactors");
        }
    }
}
