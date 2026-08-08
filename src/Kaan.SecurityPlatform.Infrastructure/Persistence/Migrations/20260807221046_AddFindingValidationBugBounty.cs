using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kaan.SecurityPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFindingValidationBugBounty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "BugBountyEligible",
                table: "Findings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DemonstratedImpact",
                table: "Findings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "EligibilityReason",
                table: "Findings",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Exploitability",
                table: "Findings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FindingClass",
                table: "Findings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PolicyCategory",
                table: "Findings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ProgramPolicyMatch",
                table: "Findings",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresManualValidation",
                table: "Findings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SubmissionRecommendation",
                table: "Findings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TechnicalSeverity",
                table: "Findings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Findings_BugBountyEligible_SubmissionRecommendation",
                table: "Findings",
                columns: new[] { "BugBountyEligible", "SubmissionRecommendation" });

            migrationBuilder.CreateIndex(
                name: "IX_Findings_FindingClass",
                table: "Findings",
                column: "FindingClass");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Findings_BugBountyEligible_SubmissionRecommendation",
                table: "Findings");

            migrationBuilder.DropIndex(
                name: "IX_Findings_FindingClass",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "BugBountyEligible",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "DemonstratedImpact",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "EligibilityReason",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "Exploitability",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "FindingClass",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "PolicyCategory",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "ProgramPolicyMatch",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "RequiresManualValidation",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "SubmissionRecommendation",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "TechnicalSeverity",
                table: "Findings");
        }
    }
}
