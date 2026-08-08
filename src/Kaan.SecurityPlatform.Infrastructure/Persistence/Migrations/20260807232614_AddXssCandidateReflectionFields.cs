using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kaan.SecurityPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddXssCandidateReflectionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AttributeEncoded",
                table: "Findings",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BugBountySeverity",
                table: "Findings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "HtmlEncoded",
                table: "Findings",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InputSource",
                table: "Findings",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReflectionContentType",
                table: "Findings",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReflectionContext",
                table: "Findings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReflectionCount",
                table: "Findings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReflectionHttpStatus",
                table: "Findings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReflectionLocation",
                table: "Findings",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReflectionMarker",
                table: "Findings",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TechnicalPotentialSeverity",
                table: "Findings",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Findings_BugBountySeverity",
                table: "Findings",
                column: "BugBountySeverity");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Findings_BugBountySeverity",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "AttributeEncoded",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "BugBountySeverity",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "HtmlEncoded",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "InputSource",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "ReflectionContentType",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "ReflectionContext",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "ReflectionCount",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "ReflectionHttpStatus",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "ReflectionLocation",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "ReflectionMarker",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "TechnicalPotentialSeverity",
                table: "Findings");
        }
    }
}
