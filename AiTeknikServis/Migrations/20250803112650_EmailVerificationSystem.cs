using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiTeknikServis.Migrations
{
    /// <inheritdoc />
    public partial class EmailVerificationSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmailVerifications_User_UserId",
                table: "EmailVerifications");

            migrationBuilder.DropIndex(
                name: "IX_EmailVerifications_Email_Type_IsUsed",
                table: "EmailVerifications");

            migrationBuilder.DropIndex(
                name: "IX_EmailVerifications_UserId",
                table: "EmailVerifications");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "EmailVerifications");

            migrationBuilder.RenameIndex(
                name: "IX_EmailVerifications_ExpiryDate",
                table: "EmailVerifications",
                newName: "IX_EmailVerification_ExpiryDate");

            migrationBuilder.AddColumn<string>(
                name: "AdditionalData",
                table: "EmailVerifications",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxRetries",
                table: "EmailVerifications",
                type: "int",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<string>(
                name: "Purpose",
                table: "EmailVerifications",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                table: "EmailVerifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UsedDate",
                table: "EmailVerifications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailVerification_Active",
                table: "EmailVerifications",
                columns: new[] { "Email", "Type", "IsUsed", "ExpiryDate" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailVerification_CreatedDate",
                table: "EmailVerifications",
                column: "CreatedDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmailVerification_Active",
                table: "EmailVerifications");

            migrationBuilder.DropIndex(
                name: "IX_EmailVerification_CreatedDate",
                table: "EmailVerifications");

            migrationBuilder.DropColumn(
                name: "AdditionalData",
                table: "EmailVerifications");

            migrationBuilder.DropColumn(
                name: "MaxRetries",
                table: "EmailVerifications");

            migrationBuilder.DropColumn(
                name: "Purpose",
                table: "EmailVerifications");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                table: "EmailVerifications");

            migrationBuilder.DropColumn(
                name: "UsedDate",
                table: "EmailVerifications");

            migrationBuilder.RenameIndex(
                name: "IX_EmailVerification_ExpiryDate",
                table: "EmailVerifications",
                newName: "IX_EmailVerifications_ExpiryDate");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "EmailVerifications",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailVerifications_Email_Type_IsUsed",
                table: "EmailVerifications",
                columns: new[] { "Email", "Type", "IsUsed" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailVerifications_UserId",
                table: "EmailVerifications",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_EmailVerifications_User_UserId",
                table: "EmailVerifications",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
