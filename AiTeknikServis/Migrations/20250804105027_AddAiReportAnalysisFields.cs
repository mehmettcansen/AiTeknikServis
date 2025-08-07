using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiTeknikServis.Migrations
{
    /// <inheritdoc />
    public partial class AddAiReportAnalysisFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AiAnalysisDate",
                table: "ServiceRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiReportAnalysis",
                table: "ServiceRequests",
                type: "nvarchar(max)",
                maxLength: 5000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiAnalysisDate",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "AiReportAnalysis",
                table: "ServiceRequests");
        }
    }
}
