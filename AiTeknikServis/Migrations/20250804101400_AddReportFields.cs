using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiTeknikServis.Migrations
{
    /// <inheritdoc />
    public partial class AddReportFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedCost",
                table: "ServiceRequests",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuggestedTechnician",
                table: "AiPredictions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstimatedCost",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "SuggestedTechnician",
                table: "AiPredictions");
        }
    }
}
