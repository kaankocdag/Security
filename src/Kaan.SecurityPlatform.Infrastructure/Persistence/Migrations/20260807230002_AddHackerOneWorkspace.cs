using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kaan.SecurityPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHackerOneWorkspace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RootCauseGroupId",
                table: "Findings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BugBountyAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActorEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Action = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    DetailsJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BugBountyAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BugBountyPrograms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Handle = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Platform = table.Column<int>(type: "int", nullable: false),
                    OpenReportUrl = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    ExternalProgramId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    LastSyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BugBountyPrograms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HackerOneApiCredentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Identifier = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProtectedApiToken = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ApiUsername = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastValidatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HackerOneApiCredentials", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RootCauseGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FingerprintKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    FindingCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RootCauseGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScanProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfileKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UserAgentConfigKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RateLimitPerMinuteConfigKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScanProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BugBountyPolicyRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BugBountyProgramId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyCategory = table.Column<int>(type: "int", nullable: false),
                    RecommendationWhenDemonstrated = table.Column<int>(type: "int", nullable: false),
                    RecommendationWhenNotDemonstrated = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BugBountyPolicyRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BugBountyPolicyRules_BugBountyPrograms_BugBountyProgramId",
                        column: x => x.BugBountyProgramId,
                        principalTable: "BugBountyPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HackerOneReportDrafts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FindingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BugBountyProgramId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Asset = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Weakness = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Impact = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    StepsToReproduce = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    ProofOfConcept = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    MarkdownBody = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: true),
                    ReportReadinessScore = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HackerOneReportDrafts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HackerOneReportDrafts_BugBountyPrograms_BugBountyProgramId",
                        column: x => x.BugBountyProgramId,
                        principalTable: "BugBountyPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HackerOneReportDrafts_Findings_FindingId",
                        column: x => x.FindingId,
                        principalTable: "Findings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HackerOneWorkspaceSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefaultBugBountyProgramId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OpenReportUrlTemplate = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    MinReadinessScoreForSubmit = table.Column<int>(type: "int", nullable: false),
                    PreferEnglishReports = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HackerOneWorkspaceSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HackerOneWorkspaceSettings_BugBountyPrograms_DefaultBugBountyProgramId",
                        column: x => x.DefaultBugBountyProgramId,
                        principalTable: "BugBountyPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "HackerOneSubmissionRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HackerOneReportDraftId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalReportId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ExternalReportUrl = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HackerOneSubmissionRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HackerOneSubmissionRecords_HackerOneReportDrafts_HackerOneReportDraftId",
                        column: x => x.HackerOneReportDraftId,
                        principalTable: "HackerOneReportDrafts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Findings_RootCauseGroupId",
                table: "Findings",
                column: "RootCauseGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_BugBountyAuditLogs_Action",
                table: "BugBountyAuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_BugBountyAuditLogs_CreatedAt",
                table: "BugBountyAuditLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BugBountyPolicyRules_BugBountyProgramId_PolicyCategory",
                table: "BugBountyPolicyRules",
                columns: new[] { "BugBountyProgramId", "PolicyCategory" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BugBountyPrograms_Handle",
                table: "BugBountyPrograms",
                column: "Handle",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BugBountyPrograms_PolicyKey",
                table: "BugBountyPrograms",
                column: "PolicyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HackerOneApiCredentials_Identifier",
                table: "HackerOneApiCredentials",
                column: "Identifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HackerOneReportDrafts_BugBountyProgramId",
                table: "HackerOneReportDrafts",
                column: "BugBountyProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_HackerOneReportDrafts_FindingId",
                table: "HackerOneReportDrafts",
                column: "FindingId");

            migrationBuilder.CreateIndex(
                name: "IX_HackerOneReportDrafts_Status",
                table: "HackerOneReportDrafts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_HackerOneSubmissionRecords_HackerOneReportDraftId",
                table: "HackerOneSubmissionRecords",
                column: "HackerOneReportDraftId");

            migrationBuilder.CreateIndex(
                name: "IX_HackerOneWorkspaceSettings_DefaultBugBountyProgramId",
                table: "HackerOneWorkspaceSettings",
                column: "DefaultBugBountyProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_RootCauseGroups_FingerprintKey",
                table: "RootCauseGroups",
                column: "FingerprintKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScanProfiles_ProfileKey",
                table: "ScanProfiles",
                column: "ProfileKey",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Findings_RootCauseGroups_RootCauseGroupId",
                table: "Findings",
                column: "RootCauseGroupId",
                principalTable: "RootCauseGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Findings_RootCauseGroups_RootCauseGroupId",
                table: "Findings");

            migrationBuilder.DropTable(
                name: "BugBountyAuditLogs");

            migrationBuilder.DropTable(
                name: "BugBountyPolicyRules");

            migrationBuilder.DropTable(
                name: "HackerOneApiCredentials");

            migrationBuilder.DropTable(
                name: "HackerOneSubmissionRecords");

            migrationBuilder.DropTable(
                name: "HackerOneWorkspaceSettings");

            migrationBuilder.DropTable(
                name: "RootCauseGroups");

            migrationBuilder.DropTable(
                name: "ScanProfiles");

            migrationBuilder.DropTable(
                name: "HackerOneReportDrafts");

            migrationBuilder.DropTable(
                name: "BugBountyPrograms");

            migrationBuilder.DropIndex(
                name: "IX_Findings_RootCauseGroupId",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "RootCauseGroupId",
                table: "Findings");
        }
    }
}
