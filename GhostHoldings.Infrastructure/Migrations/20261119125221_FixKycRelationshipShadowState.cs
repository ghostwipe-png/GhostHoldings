using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhostHoldings.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixKycRelationshipShadowState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KycSubmissions_Users_UserId1",
                table: "KycSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_KycSubmissions_UserId1",
                table: "KycSubmissions");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "KycSubmissions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId1",
                table: "KycSubmissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_KycSubmissions_UserId1",
                table: "KycSubmissions",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_KycSubmissions_Users_UserId1",
                table: "KycSubmissions",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
