using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kaan.SecurityPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLabModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LabElevationTickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConsumedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClientIp = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabElevationTickets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LabExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScenarioKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RuntimeMode = table.Column<int>(type: "int", nullable: false),
                    ElevatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ElevatedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AuditCorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ElevationTicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailureReasonTr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CancelReasonTr = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabExecutions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LabScenarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScenarioKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TitleTr = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SummaryTr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    RiskCategory = table.Column<int>(type: "int", nullable: false),
                    VulnerableImageTag = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PatchedImageTag = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabScenarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LabAuthorizationApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LabExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConfirmPhrase = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ClientIp = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabAuthorizationApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabAuthorizationApprovals_LabExecutions_LabExecutionId",
                        column: x => x.LabExecutionId,
                        principalTable: "LabExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LabComparisonResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LabExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InitialTestFailed = table.Column<bool>(type: "bit", nullable: false),
                    RetestSucceeded = table.Column<bool>(type: "bit", nullable: false),
                    VulnerableScore = table.Column<int>(type: "int", nullable: false),
                    PatchedScore = table.Column<int>(type: "int", nullable: false),
                    RiskTr = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    WhyTr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    FixTr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SummaryTr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabComparisonResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabComparisonResults_LabExecutions_LabExecutionId",
                        column: x => x.LabExecutionId,
                        principalTable: "LabExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LabEnvironments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LabExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RuntimeMode = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    NetworkId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    NetworkName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    VulnerableContainerId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    PatchedContainerId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    InternalEndpoint = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DestroyedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabEnvironments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabEnvironments_LabExecutions_LabExecutionId",
                        column: x => x.LabExecutionId,
                        principalTable: "LabExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LabExecutionLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LabExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LabExecutionStepId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Level = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    MessageTr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    LoggedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabExecutionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabExecutionLogs_LabExecutions_LabExecutionId",
                        column: x => x.LabExecutionId,
                        principalTable: "LabExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LabExecutionSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LabExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepKind = table.Column<int>(type: "int", nullable: false),
                    StepOrder = table.Column<int>(type: "int", nullable: false),
                    TitleTr = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SummaryTr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabExecutionSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabExecutionSteps_LabExecutions_LabExecutionId",
                        column: x => x.LabExecutionId,
                        principalTable: "LabExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LabAuthorizationApprovals_LabExecutionId",
                table: "LabAuthorizationApprovals",
                column: "LabExecutionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabComparisonResults_LabExecutionId",
                table: "LabComparisonResults",
                column: "LabExecutionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabElevationTickets_TokenHash",
                table: "LabElevationTickets",
                column: "TokenHash");

            migrationBuilder.CreateIndex(
                name: "IX_LabElevationTickets_UserId_ExpiresAt",
                table: "LabElevationTickets",
                columns: new[] { "UserId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LabEnvironments_ExpiresAt",
                table: "LabEnvironments",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_LabEnvironments_LabExecutionId",
                table: "LabEnvironments",
                column: "LabExecutionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabExecutionLogs_LabExecutionId_LoggedAt",
                table: "LabExecutionLogs",
                columns: new[] { "LabExecutionId", "LoggedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LabExecutions_AuditCorrelationId",
                table: "LabExecutions",
                column: "AuditCorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_LabExecutions_CreatedAt",
                table: "LabExecutions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LabExecutions_Status",
                table: "LabExecutions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_LabExecutionSteps_LabExecutionId_StepOrder",
                table: "LabExecutionSteps",
                columns: new[] { "LabExecutionId", "StepOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabScenarios_ScenarioKey",
                table: "LabScenarios",
                column: "ScenarioKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LabAuthorizationApprovals");

            migrationBuilder.DropTable(
                name: "LabComparisonResults");

            migrationBuilder.DropTable(
                name: "LabElevationTickets");

            migrationBuilder.DropTable(
                name: "LabEnvironments");

            migrationBuilder.DropTable(
                name: "LabExecutionLogs");

            migrationBuilder.DropTable(
                name: "LabExecutionSteps");

            migrationBuilder.DropTable(
                name: "LabScenarios");

            migrationBuilder.DropTable(
                name: "LabExecutions");
        }
    }
}
