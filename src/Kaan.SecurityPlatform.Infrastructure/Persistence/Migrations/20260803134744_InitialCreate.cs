using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kaan.SecurityPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    LegalName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    TaxNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ContactName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    ContactPhone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    WebsiteUrl = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Industry = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    City = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SuspensionReason = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    IconName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ParentCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeCategories_KnowledgeCategories_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalTable: "KnowledgeCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsSystemRole = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    MonthlyScanLimit = table.Column<int>(type: "int", nullable: false),
                    MaxProjects = table.Column<int>(type: "int", nullable: false),
                    MaxDomains = table.Column<int>(type: "int", nullable: false),
                    ScheduledScanningEnabled = table.Column<bool>(type: "bit", nullable: false),
                    SourceCodeScanningEnabled = table.Column<bool>(type: "bit", nullable: false),
                    ReportExportEnabled = table.Column<bool>(type: "bit", nullable: false),
                    PriorityQueueEnabled = table.Column<bool>(type: "bit", nullable: false),
                    MonthlyPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    MembershipStatus = table.Column<int>(type: "int", nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    SuspensionReason = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PreferredLanguage = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    AvatarPath = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    JobTitle = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    PhoneCountryCode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    PrimaryCompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Details = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    IsSensitive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CompanyUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyRole = table.Column<int>(type: "int", nullable: false),
                    IsPrimaryContact = table.Column<bool>(type: "bit", nullable: false),
                    LastAccessAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InvitationEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    InvitedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyUsers_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActionUrl = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RelatedEntityType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RelatedEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SecurityProjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    EnvironmentType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PrimaryContactEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityProjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecurityProjects_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleClaims_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompanySubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubscriptionPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    UsedScansThisPeriod = table.Column<int>(type: "int", nullable: false),
                    CurrentPeriodStartsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CurrentPeriodEndsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanySubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanySubscriptions_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompanySubscriptions_SubscriptionPlans_SubscriptionPlanId",
                        column: x => x.SubscriptionPlanId,
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    JwtId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedByIp = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RevocationReason = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ReplacedByTokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByIp = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserClaims_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_UserLogins_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_UserTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DomainAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SecurityProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HostName = table.Column<string>(type: "nvarchar(253)", maxLength: 253, nullable: false),
                    NormalizedHostName = table.Column<string>(type: "nvarchar(253)", maxLength: 253, nullable: false),
                    Scheme = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Port = table.Column<int>(type: "int", nullable: true),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    VerificationMethod = table.Column<int>(type: "int", nullable: true),
                    VerificationToken = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    VerificationTokenCreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastVerificationError = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DomainAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DomainAssets_SecurityProjects_SecurityProjectId",
                        column: x => x.SecurityProjectId,
                        principalTable: "SecurityProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuthorizationRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SecurityProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DomainAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorizedByName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AuthorizedByEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    AuthorizedByTitle = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    AuthorizationScope = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    AllowedTestTypes = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    ForbiddenActions = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    StartsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaximumRequestsPerSecond = table.Column<int>(type: "int", nullable: false),
                    ApprovalEvidencePath = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RevocationReason = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthorizationRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuthorizationRecords_DomainAssets_DomainAssetId",
                        column: x => x.DomainAssetId,
                        principalTable: "DomainAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuthorizationRecords_SecurityProjects_SecurityProjectId",
                        column: x => x.SecurityProjectId,
                        principalTable: "SecurityProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ScanJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SecurityProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DomainAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScanType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProgressPercentage = table.Column<int>(type: "int", nullable: false),
                    CurrentStep = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    TotalSteps = table.Column<int>(type: "int", nullable: false),
                    CompletedSteps = table.Column<int>(type: "int", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    ScannerVersion = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    HangfireJobId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    PreviousScanJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsRetest = table.Column<bool>(type: "bit", nullable: false),
                    RetestForFindingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScanJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScanJobs_DomainAssets_DomainAssetId",
                        column: x => x.DomainAssetId,
                        principalTable: "DomainAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScanJobs_ScanJobs_PreviousScanJobId",
                        column: x => x.PreviousScanJobId,
                        principalTable: "ScanJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScanJobs_SecurityProjects_SecurityProjectId",
                        column: x => x.SecurityProjectId,
                        principalTable: "SecurityProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ScanResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScanJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SecurityScore = table.Column<int>(type: "int", nullable: false),
                    PreviousSecurityScore = table.Column<int>(type: "int", nullable: false),
                    CriticalCount = table.Column<int>(type: "int", nullable: false),
                    HighCount = table.Column<int>(type: "int", nullable: false),
                    MediumCount = table.Column<int>(type: "int", nullable: false),
                    LowCount = table.Column<int>(type: "int", nullable: false),
                    InfoCount = table.Column<int>(type: "int", nullable: false),
                    ConfirmedCount = table.Column<int>(type: "int", nullable: false),
                    StrongIndicationCount = table.Column<int>(type: "int", nullable: false),
                    RecommendationCount = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ExecutiveSummary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    TechnicalSummary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ChecksTotal = table.Column<int>(type: "int", nullable: false),
                    ChecksPassed = table.Column<int>(type: "int", nullable: false),
                    ChecksFailed = table.Column<int>(type: "int", nullable: false),
                    ChecksSkipped = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScanResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScanResults_ScanJobs_ScanJobId",
                        column: x => x.ScanJobId,
                        principalTable: "ScanJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Findings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScanResultId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    TechnicalDescription = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    BusinessImpact = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    ConfidenceLevel = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CweCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    OwaspCategory = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    AffectedUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    AffectedParameter = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Evidence = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Remediation = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    RemediationExampleConfig = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    TurkishExecutiveSummary = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    FirstSeenAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsFalsePositive = table.Column<bool>(type: "bit", nullable: false),
                    VerifiedByAnalystUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CheckCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Fingerprint = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ScoreImpact = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Findings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Findings_ScanResults_ScanResultId",
                        column: x => x.ScanResultId,
                        principalTable: "ScanResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FindingStatusHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FindingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousStatus = table.Column<int>(type: "int", nullable: false),
                    NewStatus = table.Column<int>(type: "int", nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FindingStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FindingStatusHistories_Findings_FindingId",
                        column: x => x.FindingId,
                        principalTable: "Findings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RemediationRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FindingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AssignedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EstimatedPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    InternalNotes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ContactPreference = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletionNote = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemediationRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RemediationRequests_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RemediationRequests_Findings_FindingId",
                        column: x => x.FindingId,
                        principalTable: "Findings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RetestComparisons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalFindingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousScanResultId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentScanResultId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousSeverity = table.Column<int>(type: "int", nullable: false),
                    CurrentSeverity = table.Column<int>(type: "int", nullable: true),
                    PreviousConfidence = table.Column<int>(type: "int", nullable: false),
                    CurrentConfidence = table.Column<int>(type: "int", nullable: true),
                    Result = table.Column<int>(type: "int", nullable: false),
                    ComparisonSummary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetestComparisons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RetestComparisons_Findings_OriginalFindingId",
                        column: x => x.OriginalFindingId,
                        principalTable: "Findings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RetestComparisons_ScanResults_CurrentScanResultId",
                        column: x => x.CurrentScanResultId,
                        principalTable: "ScanResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RetestComparisons_ScanResults_PreviousScanResultId",
                        column: x => x.PreviousScanResultId,
                        principalTable: "ScanResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FindingKnowledgeLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FindingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ArticleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RelevanceScore = table.Column<int>(type: "int", nullable: false),
                    IsAutoLinked = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FindingKnowledgeLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FindingKnowledgeLinks_Findings_FindingId",
                        column: x => x.FindingId,
                        principalTable: "Findings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeArticleReferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ArticleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferenceType = table.Column<int>(type: "int", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeArticleReferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeArticles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    BodyMarkdown = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CweCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    OwaspCategory = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CveCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    DifficultyLevel = table.Column<int>(type: "int", nullable: false),
                    EstimatedReadMinutes = table.Column<int>(type: "int", nullable: false),
                    Tags = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AuthorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceAttribution = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SourceUrl = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    ViewCount = table.Column<int>(type: "int", nullable: false),
                    CoverMediaAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeArticles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeArticles_KnowledgeCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "KnowledgeCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeMediaAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ArticleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StoragePath = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    PublicUrl = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    MimeType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Width = table.Column<int>(type: "int", nullable: true),
                    Height = table.Column<int>(type: "int", nullable: true),
                    Caption = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    AltText = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Sha256Hash = table.Column<string>(type: "nvarchar(96)", maxLength: 96, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeMediaAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeMediaAssets_KnowledgeArticles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "KnowledgeArticles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CompanyId_CreatedAt",
                table: "AuditLogs",
                columns: new[] { "CompanyId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CreatedAt",
                table: "AuditLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_EntityId",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId_CreatedAt",
                table: "AuditLogs",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizationRecords_DomainAssetId",
                table: "AuthorizationRecords",
                column: "DomainAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizationRecords_SecurityProjectId_DomainAssetId_Status",
                table: "AuthorizationRecords",
                columns: new[] { "SecurityProjectId", "DomainAssetId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Companies_ContactEmail",
                table: "Companies",
                column: "ContactEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Name",
                table: "Companies",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Status",
                table: "Companies",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CompanySubscriptions_CompanyId_Status",
                table: "CompanySubscriptions",
                columns: new[] { "CompanyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CompanySubscriptions_SubscriptionPlanId",
                table: "CompanySubscriptions",
                column: "SubscriptionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyUsers_CompanyId_UserId",
                table: "CompanyUsers",
                columns: new[] { "CompanyId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyUsers_UserId",
                table: "CompanyUsers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DomainAssets_IsVerified",
                table: "DomainAssets",
                column: "IsVerified");

            migrationBuilder.CreateIndex(
                name: "IX_DomainAssets_NormalizedHostName",
                table: "DomainAssets",
                column: "NormalizedHostName");

            migrationBuilder.CreateIndex(
                name: "IX_DomainAssets_SecurityProjectId_NormalizedHostName",
                table: "DomainAssets",
                columns: new[] { "SecurityProjectId", "NormalizedHostName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DomainAssets_Status",
                table: "DomainAssets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_FindingKnowledgeLinks_ArticleId",
                table: "FindingKnowledgeLinks",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_FindingKnowledgeLinks_FindingId_ArticleId",
                table: "FindingKnowledgeLinks",
                columns: new[] { "FindingId", "ArticleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Findings_CheckCode",
                table: "Findings",
                column: "CheckCode");

            migrationBuilder.CreateIndex(
                name: "IX_Findings_CompanyId_Severity_Status",
                table: "Findings",
                columns: new[] { "CompanyId", "Severity", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Findings_Fingerprint",
                table: "Findings",
                column: "Fingerprint");

            migrationBuilder.CreateIndex(
                name: "IX_Findings_ScanResultId_Status",
                table: "Findings",
                columns: new[] { "ScanResultId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FindingStatusHistories_FindingId",
                table: "FindingStatusHistories",
                column: "FindingId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeArticleReferences_ArticleId",
                table: "KnowledgeArticleReferences",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeArticles_CategoryId_IsPublished",
                table: "KnowledgeArticles",
                columns: new[] { "CategoryId", "IsPublished" });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeArticles_CoverMediaAssetId",
                table: "KnowledgeArticles",
                column: "CoverMediaAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeArticles_CweCode",
                table: "KnowledgeArticles",
                column: "CweCode");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeArticles_IsFeatured",
                table: "KnowledgeArticles",
                column: "IsFeatured");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeArticles_Slug",
                table: "KnowledgeArticles",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeCategories_ParentCategoryId_DisplayOrder",
                table: "KnowledgeCategories",
                columns: new[] { "ParentCategoryId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeCategories_Slug",
                table: "KnowledgeCategories",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeMediaAssets_ArticleId",
                table: "KnowledgeMediaAssets",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeMediaAssets_Sha256Hash",
                table: "KnowledgeMediaAssets",
                column: "Sha256Hash");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CompanyId_CreatedAt",
                table: "Notifications",
                columns: new[] { "CompanyId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead_CreatedAt",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ExpiresAt",
                table: "RefreshTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RemediationRequests_CompanyId_Status",
                table: "RemediationRequests",
                columns: new[] { "CompanyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RemediationRequests_FindingId",
                table: "RemediationRequests",
                column: "FindingId");

            migrationBuilder.CreateIndex(
                name: "IX_RetestComparisons_CurrentScanResultId",
                table: "RetestComparisons",
                column: "CurrentScanResultId");

            migrationBuilder.CreateIndex(
                name: "IX_RetestComparisons_OriginalFindingId",
                table: "RetestComparisons",
                column: "OriginalFindingId");

            migrationBuilder.CreateIndex(
                name: "IX_RetestComparisons_PreviousScanResultId_CurrentScanResultId",
                table: "RetestComparisons",
                columns: new[] { "PreviousScanResultId", "CurrentScanResultId" });

            migrationBuilder.CreateIndex(
                name: "IX_RoleClaims_RoleId",
                table: "RoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "Roles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ScanJobs_DomainAssetId_CreatedAt",
                table: "ScanJobs",
                columns: new[] { "DomainAssetId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ScanJobs_HangfireJobId",
                table: "ScanJobs",
                column: "HangfireJobId");

            migrationBuilder.CreateIndex(
                name: "IX_ScanJobs_PreviousScanJobId",
                table: "ScanJobs",
                column: "PreviousScanJobId");

            migrationBuilder.CreateIndex(
                name: "IX_ScanJobs_SecurityProjectId_Status",
                table: "ScanJobs",
                columns: new[] { "SecurityProjectId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ScanJobs_Status",
                table: "ScanJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ScanResults_ScanJobId",
                table: "ScanResults",
                column: "ScanJobId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecurityProjects_CompanyId_Name",
                table: "SecurityProjects",
                columns: new[] { "CompanyId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityProjects_Status",
                table: "SecurityProjects",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_Name",
                table: "SubscriptionPlans",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserClaims_UserId",
                table: "UserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLogins_UserId",
                table: "UserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "Users",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_Users_MembershipStatus",
                table: "Users",
                column: "MembershipStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Users_PrimaryCompanyId",
                table: "Users",
                column: "PrimaryCompanyId");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "Users",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_FindingKnowledgeLinks_KnowledgeArticles_ArticleId",
                table: "FindingKnowledgeLinks",
                column: "ArticleId",
                principalTable: "KnowledgeArticles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_KnowledgeArticleReferences_KnowledgeArticles_ArticleId",
                table: "KnowledgeArticleReferences",
                column: "ArticleId",
                principalTable: "KnowledgeArticles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_KnowledgeArticles_KnowledgeMediaAssets_CoverMediaAssetId",
                table: "KnowledgeArticles",
                column: "CoverMediaAssetId",
                principalTable: "KnowledgeMediaAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KnowledgeMediaAssets_KnowledgeArticles_ArticleId",
                table: "KnowledgeMediaAssets");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "AuthorizationRecords");

            migrationBuilder.DropTable(
                name: "CompanySubscriptions");

            migrationBuilder.DropTable(
                name: "CompanyUsers");

            migrationBuilder.DropTable(
                name: "FindingKnowledgeLinks");

            migrationBuilder.DropTable(
                name: "FindingStatusHistories");

            migrationBuilder.DropTable(
                name: "KnowledgeArticleReferences");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "RemediationRequests");

            migrationBuilder.DropTable(
                name: "RetestComparisons");

            migrationBuilder.DropTable(
                name: "RoleClaims");

            migrationBuilder.DropTable(
                name: "UserClaims");

            migrationBuilder.DropTable(
                name: "UserLogins");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "UserTokens");

            migrationBuilder.DropTable(
                name: "SubscriptionPlans");

            migrationBuilder.DropTable(
                name: "Findings");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "ScanResults");

            migrationBuilder.DropTable(
                name: "ScanJobs");

            migrationBuilder.DropTable(
                name: "DomainAssets");

            migrationBuilder.DropTable(
                name: "SecurityProjects");

            migrationBuilder.DropTable(
                name: "Companies");

            migrationBuilder.DropTable(
                name: "KnowledgeArticles");

            migrationBuilder.DropTable(
                name: "KnowledgeCategories");

            migrationBuilder.DropTable(
                name: "KnowledgeMediaAssets");
        }
    }
}
