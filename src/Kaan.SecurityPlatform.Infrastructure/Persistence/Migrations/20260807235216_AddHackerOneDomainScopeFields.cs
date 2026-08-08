using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kaan.SecurityPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHackerOneDomainScopeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DomainAssets_SecurityProjectId_NormalizedHostName",
                table: "DomainAssets");

            migrationBuilder.AddColumn<string>(
                name: "HackerOneAssetType",
                table: "DomainAssets",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HackerOneBountySummary",
                table: "DomainAssets",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HackerOneCurrency",
                table: "DomainAssets",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HackerOneEligibleForBounty",
                table: "DomainAssets",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HackerOneEligibleForSubmission",
                table: "DomainAssets",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HackerOneIsWildcard",
                table: "DomainAssets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "HackerOneLastSyncedAt",
                table: "DomainAssets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HackerOneMaxSeverity",
                table: "DomainAssets",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HackerOneOffersBounties",
                table: "DomainAssets",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HackerOneProgramHandle",
                table: "DomainAssets",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HackerOneProgramName",
                table: "DomainAssets",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HackerOneScopeId",
                table: "DomainAssets",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HackerOneSubmissionState",
                table: "DomainAssets",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "DomainAssets",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Manual");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "BugBountyPrograms",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OffersBounties",
                table: "BugBountyPrograms",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OpenScope",
                table: "BugBountyPrograms",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "BugBountyPrograms",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmissionState",
                table: "BugBountyPrograms",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DomainAssets_HackerOneEligibleForBounty",
                table: "DomainAssets",
                column: "HackerOneEligibleForBounty");

            migrationBuilder.CreateIndex(
                name: "IX_DomainAssets_HackerOneProgramHandle",
                table: "DomainAssets",
                column: "HackerOneProgramHandle");

            migrationBuilder.CreateIndex(
                name: "IX_DomainAssets_SecurityProjectId_NormalizedHostName_HackerOneProgramHandle",
                table: "DomainAssets",
                columns: new[] { "SecurityProjectId", "NormalizedHostName", "HackerOneProgramHandle" },
                unique: true,
                filter: "[HackerOneProgramHandle] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DomainAssets_SecurityProjectId_NormalizedHostName_Manual",
                table: "DomainAssets",
                columns: new[] { "SecurityProjectId", "NormalizedHostName" },
                unique: true,
                filter: "[HackerOneProgramHandle] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DomainAssets_Source",
                table: "DomainAssets",
                column: "Source");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DomainAssets_HackerOneEligibleForBounty",
                table: "DomainAssets");

            migrationBuilder.DropIndex(
                name: "IX_DomainAssets_HackerOneProgramHandle",
                table: "DomainAssets");

            migrationBuilder.DropIndex(
                name: "IX_DomainAssets_SecurityProjectId_NormalizedHostName_HackerOneProgramHandle",
                table: "DomainAssets");

            migrationBuilder.DropIndex(
                name: "IX_DomainAssets_SecurityProjectId_NormalizedHostName_Manual",
                table: "DomainAssets");

            migrationBuilder.DropIndex(
                name: "IX_DomainAssets_Source",
                table: "DomainAssets");

            migrationBuilder.DropColumn(
                name: "HackerOneAssetType",
                table: "DomainAssets");

            migrationBuilder.DropColumn(
                name: "HackerOneBountySummary",
                table: "DomainAssets");

            migrationBuilder.DropColumn(
                name: "HackerOneCurrency",
                table: "DomainAssets");

            migrationBuilder.DropColumn(
                name: "HackerOneEligibleForBounty",
                table: "DomainAssets");

            migrationBuilder.DropColumn(
                name: "HackerOneEligibleForSubmission",
                table: "DomainAssets");

            migrationBuilder.DropColumn(
                name: "HackerOneIsWildcard",
                table: "DomainAssets");

            migrationBuilder.DropColumn(
                name: "HackerOneLastSyncedAt",
                table: "DomainAssets");

            migrationBuilder.DropColumn(
                name: "HackerOneMaxSeverity",
                table: "DomainAssets");

            migrationBuilder.DropColumn(
                name: "HackerOneOffersBounties",
                table: "DomainAssets");

            migrationBuilder.DropColumn(
                name: "HackerOneProgramHandle",
                table: "DomainAssets");

            migrationBuilder.DropColumn(
                name: "HackerOneProgramName",
                table: "DomainAssets");

            migrationBuilder.DropColumn(
                name: "HackerOneScopeId",
                table: "DomainAssets");

            migrationBuilder.DropColumn(
                name: "HackerOneSubmissionState",
                table: "DomainAssets");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "DomainAssets");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "BugBountyPrograms");

            migrationBuilder.DropColumn(
                name: "OffersBounties",
                table: "BugBountyPrograms");

            migrationBuilder.DropColumn(
                name: "OpenScope",
                table: "BugBountyPrograms");

            migrationBuilder.DropColumn(
                name: "State",
                table: "BugBountyPrograms");

            migrationBuilder.DropColumn(
                name: "SubmissionState",
                table: "BugBountyPrograms");

            migrationBuilder.CreateIndex(
                name: "IX_DomainAssets_SecurityProjectId_NormalizedHostName",
                table: "DomainAssets",
                columns: new[] { "SecurityProjectId", "NormalizedHostName" },
                unique: true);
        }
    }
}
