using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhostHoldings.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameKycImagesToUrls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SelfieBase64",
                table: "KycApplications",
                newName: "SelfieImageUrl");

            migrationBuilder.RenameColumn(
                name: "FrontImageBase64",
                table: "KycApplications",
                newName: "FrontImageUrl");

            migrationBuilder.RenameColumn(
                name: "BackImageBase64",
                table: "KycApplications",
                newName: "BackImageUrl");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SelfieImageUrl",
                table: "KycApplications",
                newName: "SelfieBase64");

            migrationBuilder.RenameColumn(
                name: "FrontImageUrl",
                table: "KycApplications",
                newName: "FrontImageBase64");

            migrationBuilder.RenameColumn(
                name: "BackImageUrl",
                table: "KycApplications",
                newName: "BackImageBase64");
        }
    }
}
