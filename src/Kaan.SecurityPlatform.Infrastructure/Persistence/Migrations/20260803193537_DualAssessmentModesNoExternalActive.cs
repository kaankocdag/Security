using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kaan.SecurityPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DualAssessmentModesNoExternalActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssessmentMode",
                table: "ScanJobs",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "AssessmentMode",
                table: "LabExecutions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "LabTargetSiteId",
                table: "LabExecutions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "TargetHostName",
                table: "LabExecutions",
                type: "nvarchar(253)",
                maxLength: 253,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "LabTargetSites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HostName = table.Column<string>(type: "nvarchar(253)", maxLength: 253, nullable: false),
                    NormalizedHostName = table.Column<string>(type: "nvarchar(253)", maxLength: 253, nullable: false),
                    NotesTr = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabTargetSites", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScanJobs_AssessmentMode",
                table: "ScanJobs",
                column: "AssessmentMode");

            migrationBuilder.CreateIndex(
                name: "IX_LabExecutions_LabTargetSiteId",
                table: "LabExecutions",
                column: "LabTargetSiteId");

            migrationBuilder.CreateIndex(
                name: "IX_LabTargetSites_IsEnabled",
                table: "LabTargetSites",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_LabTargetSites_NormalizedHostName",
                table: "LabTargetSites",
                column: "NormalizedHostName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LabTargetSites");

            migrationBuilder.DropIndex(
                name: "IX_ScanJobs_AssessmentMode",
                table: "ScanJobs");

            migrationBuilder.DropIndex(
                name: "IX_LabExecutions_LabTargetSiteId",
                table: "LabExecutions");

            migrationBuilder.DropColumn(
                name: "AssessmentMode",
                table: "ScanJobs");

            migrationBuilder.DropColumn(
                name: "AssessmentMode",
                table: "LabExecutions");

            migrationBuilder.DropColumn(
                name: "LabTargetSiteId",
                table: "LabExecutions");

            migrationBuilder.DropColumn(
                name: "TargetHostName",
                table: "LabExecutions");
        }
    }
}
