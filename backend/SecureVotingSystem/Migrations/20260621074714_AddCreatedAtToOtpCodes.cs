using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureVotingSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedAtToOtpCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "OtpCodes",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_OtpCodes_UserId",
                table: "OtpCodes",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_OtpCodes_Users_UserId",
                table: "OtpCodes",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OtpCodes_Users_UserId",
                table: "OtpCodes");

            migrationBuilder.DropIndex(
                name: "IX_OtpCodes_UserId",
                table: "OtpCodes");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "OtpCodes");
        }
    }
}
