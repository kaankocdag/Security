using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kaan.SecurityPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFindingValidationRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ConfirmedVulnerability",
                table: "Findings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "LatestValidationRunId",
                table: "Findings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LatestValidationStatus",
                table: "Findings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PotentialRewardEligible",
                table: "Findings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SubmissionEligible",
                table: "Findings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ScopePolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProgramName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ProgramUrl = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ScopeStatus = table.Column<int>(type: "int", nullable: false),
                    AllowedTestMethods = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    ProhibitedTestMethods = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    RateLimit = table.Column<int>(type: "int", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastVerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PolicyEvidence = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    TargetInBountyScope = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScopePolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScopePolicies_DomainAssets_TargetId",
                        column: x => x.TargetId,
                        principalTable: "DomainAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestAccountSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    OwnershipConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TestingPermissionConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    EncryptedSecretReference = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    OwnedTestResourceHint = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestAccountSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestAccountSessions_DomainAssets_TargetId",
                        column: x => x.TargetId,
                        principalTable: "DomainAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ValidationAuthorizationEvidence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorizationRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AuthorizedByName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AuthorizedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ScopeSummary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    AllowedTestTypes = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EvidenceNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidationAuthorizationEvidence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ValidationAuthorizationEvidence_AuthorizationRecords_AuthorizationRecordId",
                        column: x => x.AuthorizationRecordId,
                        principalTable: "AuthorizationRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ValidationAuthorizationEvidence_DomainAssets_TargetId",
                        column: x => x.TargetId,
                        principalTable: "DomainAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FindingValidationRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FindingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ValidatorType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ValidationMode = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RiskLevel = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequestedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AuthorizationEvidenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ScopePolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MaxRequestCount = table.Column<int>(type: "int", nullable: false),
                    ActualRequestCount = table.Column<int>(type: "int", nullable: false),
                    StopReason = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ErrorCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    StopRequested = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FindingValidationRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FindingValidationRuns_DomainAssets_TargetId",
                        column: x => x.TargetId,
                        principalTable: "DomainAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FindingValidationRuns_Findings_FindingId",
                        column: x => x.FindingId,
                        principalTable: "Findings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FindingValidationRuns_ScopePolicies_ScopePolicyId",
                        column: x => x.ScopePolicyId,
                        principalTable: "ScopePolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_FindingValidationRuns_ValidationAuthorizationEvidence_AuthorizationEvidenceId",
                        column: x => x.AuthorizationEvidenceId,
                        principalTable: "ValidationAuthorizationEvidence",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "FindingValidationResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ValidationRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConfirmedVulnerability = table.Column<bool>(type: "bit", nullable: false),
                    DemonstratedImpact = table.Column<bool>(type: "bit", nullable: false),
                    ImpactType = table.Column<int>(type: "int", nullable: false),
                    Confidence = table.Column<int>(type: "int", nullable: false),
                    SubmissionRecommendation = table.Column<int>(type: "int", nullable: false),
                    SubmissionEligible = table.Column<bool>(type: "bit", nullable: false),
                    PotentialRewardEligible = table.Column<bool>(type: "bit", nullable: false),
                    EligibilityReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ManualReviewReasons = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ExpectedResult = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ActualResult = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ValidatorVersion = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ReproductionCount = table.Column<int>(type: "int", nullable: false),
                    TestAccountRolesUsed = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FindingValidationResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FindingValidationResults_FindingValidationRuns_ValidationRunId",
                        column: x => x.ValidationRunId,
                        principalTable: "FindingValidationRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ValidationEvidence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ValidationRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvidenceType = table.Column<int>(type: "int", nullable: false),
                    RequestMethod = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    RedactedRequestUrl = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    ResponseStatusCode = table.Column<int>(type: "int", nullable: true),
                    FinalUrl = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    RedirectChain = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ResponseContentType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ResponseHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RedactedResponseExcerpt = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SessionRole = table.Column<int>(type: "int", nullable: false),
                    CapturedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidationEvidence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ValidationEvidence_FindingValidationRuns_ValidationRunId",
                        column: x => x.ValidationRunId,
                        principalTable: "FindingValidationRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FindingValidationResults_ValidationRunId",
                table: "FindingValidationResults",
                column: "ValidationRunId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FindingValidationRuns_AuthorizationEvidenceId",
                table: "FindingValidationRuns",
                column: "AuthorizationEvidenceId");

            migrationBuilder.CreateIndex(
                name: "IX_FindingValidationRuns_FindingId",
                table: "FindingValidationRuns",
                column: "FindingId");

            migrationBuilder.CreateIndex(
                name: "IX_FindingValidationRuns_ScopePolicyId",
                table: "FindingValidationRuns",
                column: "ScopePolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_FindingValidationRuns_TargetId_Status",
                table: "FindingValidationRuns",
                columns: new[] { "TargetId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ScopePolicies_TargetId",
                table: "ScopePolicies",
                column: "TargetId");

            migrationBuilder.CreateIndex(
                name: "IX_TestAccountSessions_TargetId_Role",
                table: "TestAccountSessions",
                columns: new[] { "TargetId", "Role" });

            migrationBuilder.CreateIndex(
                name: "IX_ValidationAuthorizationEvidence_AuthorizationRecordId",
                table: "ValidationAuthorizationEvidence",
                column: "AuthorizationRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationAuthorizationEvidence_TargetId",
                table: "ValidationAuthorizationEvidence",
                column: "TargetId");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationEvidence_ValidationRunId",
                table: "ValidationEvidence",
                column: "ValidationRunId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FindingValidationResults");

            migrationBuilder.DropTable(
                name: "TestAccountSessions");

            migrationBuilder.DropTable(
                name: "ValidationEvidence");

            migrationBuilder.DropTable(
                name: "FindingValidationRuns");

            migrationBuilder.DropTable(
                name: "ScopePolicies");

            migrationBuilder.DropTable(
                name: "ValidationAuthorizationEvidence");

            migrationBuilder.DropColumn(
                name: "ConfirmedVulnerability",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "LatestValidationRunId",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "LatestValidationStatus",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "PotentialRewardEligible",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "SubmissionEligible",
                table: "Findings");
        }
    }
}
