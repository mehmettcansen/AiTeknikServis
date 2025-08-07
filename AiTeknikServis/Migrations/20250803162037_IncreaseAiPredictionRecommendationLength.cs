using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiTeknikServis.Migrations
{
    /// <inheritdoc />
    public partial class IncreaseAiPredictionRecommendationLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContactPreference",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "EstimatedCost",
                table: "ServiceRequests");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContactPreference",
                table: "ServiceRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedCost",
                table: "ServiceRequests",
                type: "decimal(18,2)",
                nullable: true);
        }
    }
}
