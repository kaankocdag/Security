using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kaan.SecurityPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthenticatedScanning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TestIdentityProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfileName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    TargetDomain = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ProgramName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ProgramUrl = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: true),
                    AccountPurpose = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    OwnershipConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TestingPermissionConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestIdentityProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestIdentityProfiles_DomainAssets_TargetId",
                        column: x => x.TargetId,
                        principalTable: "DomainAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TargetTestAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetDomain = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Username = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    EncryptedSecretReference = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    AccountStatus = table.Column<int>(type: "int", nullable: false),
                    VerificationStatus = table.Column<int>(type: "int", nullable: false),
                    RegistrationUrl = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    LoginUrl = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    LastSuccessfulLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastAuthenticatedScanAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OwnershipConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TestingPermissionConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Role = table.Column<int>(type: "int", nullable: false),
                    IdentityProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TargetTestAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TargetTestAccounts_DomainAssets_TargetId",
                        column: x => x.TargetId,
                        principalTable: "DomainAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TargetTestAccounts_TestIdentityProfiles_IdentityProfileId",
                        column: x => x.IdentityProfileId,
                        principalTable: "TestIdentityProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "AuthenticatedScanRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TestAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TakeoverReason = table.Column<int>(type: "int", nullable: false),
                    TakeoverMessage = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequestedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MaxRequestCount = table.Column<int>(type: "int", nullable: false),
                    ActualRequestCount = table.Column<int>(type: "int", nullable: false),
                    StopReason = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ErrorCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    HeadedBrowser = table.Column<bool>(type: "bit", nullable: false),
                    LoginUrlUsed = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    AuthenticationConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthenticatedScanRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuthenticatedScanRuns_DomainAssets_TargetId",
                        column: x => x.TargetId,
                        principalTable: "DomainAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuthenticatedScanRuns_TargetTestAccounts_TestAccountId",
                        column: x => x.TestAccountId,
                        principalTable: "TargetTestAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "ScanModeObservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthenticatedScanRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsAuthenticatedMode = table.Column<bool>(type: "bit", nullable: false),
                    TestAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MaskedAccountLabel = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Url = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    StatusCode = table.Column<int>(type: "int", nullable: false),
                    FinalUrl = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    RedirectChain = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ContentType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ResponseHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    LoginDetected = table.Column<bool>(type: "bit", nullable: false),
                    AccessDeniedDetected = table.Column<bool>(type: "bit", nullable: false),
                    AuthenticationConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    RedactedEvidence = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ComparisonResult = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScanModeObservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScanModeObservations_AuthenticatedScanRuns_AuthenticatedScanRunId",
                        column: x => x.AuthenticatedScanRunId,
                        principalTable: "AuthenticatedScanRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticatedScanRuns_TargetId",
                table: "AuthenticatedScanRuns",
                column: "TargetId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticatedScanRuns_TestAccountId",
                table: "AuthenticatedScanRuns",
                column: "TestAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ScanModeObservations_AuthenticatedScanRunId",
                table: "ScanModeObservations",
                column: "AuthenticatedScanRunId");

            migrationBuilder.CreateIndex(
                name: "IX_TargetTestAccounts_IdentityProfileId",
                table: "TargetTestAccounts",
                column: "IdentityProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_TargetTestAccounts_TargetId_Role",
                table: "TargetTestAccounts",
                columns: new[] { "TargetId", "Role" });

            migrationBuilder.CreateIndex(
                name: "IX_TestIdentityProfiles_TargetId",
                table: "TestIdentityProfiles",
                column: "TargetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScanModeObservations");

            migrationBuilder.DropTable(
                name: "AuthenticatedScanRuns");

            migrationBuilder.DropTable(
                name: "TargetTestAccounts");

            migrationBuilder.DropTable(
                name: "TestIdentityProfiles");
        }
    }
}
