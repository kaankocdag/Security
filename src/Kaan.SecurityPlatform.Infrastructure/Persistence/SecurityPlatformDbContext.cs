using System.Reflection;
using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Domain.Common;
using Kaan.SecurityPlatform.Domain.Entities.Audit;
using Kaan.SecurityPlatform.Domain.Entities.AuthenticatedScanning;
using Kaan.SecurityPlatform.Domain.Entities.BugBounty;
using Kaan.SecurityPlatform.Domain.Entities.Companies;
using Kaan.SecurityPlatform.Domain.Entities.Findings;
using Kaan.SecurityPlatform.Domain.Entities.Knowledge;
using Kaan.SecurityPlatform.Domain.Entities.Lab;
using Kaan.SecurityPlatform.Domain.Entities.Notifications;
using Kaan.SecurityPlatform.Domain.Entities.Projects;
using Kaan.SecurityPlatform.Domain.Entities.Scans;
using Kaan.SecurityPlatform.Domain.Entities.Subscriptions;
using Kaan.SecurityPlatform.Domain.Entities.Validation;
using Kaan.SecurityPlatform.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Kaan.SecurityPlatform.Infrastructure.Persistence;

public class SecurityPlatformDbContext
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>,
      IApplicationDbContext
{
    private readonly ICurrentUser? _currentUser;
    private readonly IDateTimeProvider? _dateTimeProvider;

    public SecurityPlatformDbContext(
        DbContextOptions<SecurityPlatformDbContext> options,
        ICurrentUser? currentUser = null,
        IDateTimeProvider? dateTimeProvider = null)
        : base(options)
    {
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<CompanyUser> CompanyUsers => Set<CompanyUser>();
    public DbSet<SecurityProject> SecurityProjects => Set<SecurityProject>();
    public DbSet<DomainAsset> DomainAssets => Set<DomainAsset>();
    public DbSet<AuthorizationRecord> AuthorizationRecords => Set<AuthorizationRecord>();
    public DbSet<ScanJob> ScanJobs => Set<ScanJob>();
    public DbSet<ScanResult> ScanResults => Set<ScanResult>();
    public DbSet<Finding> Findings => Set<Finding>();
    public DbSet<FindingStatusHistory> FindingStatusHistories => Set<FindingStatusHistory>();
    public DbSet<RemediationRequest> RemediationRequests => Set<RemediationRequest>();
    public DbSet<RetestComparison> RetestComparisons => Set<RetestComparison>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<CompanySubscription> CompanySubscriptions => Set<CompanySubscription>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<KnowledgeCategory> KnowledgeCategories => Set<KnowledgeCategory>();
    public DbSet<KnowledgeArticle> KnowledgeArticles => Set<KnowledgeArticle>();
    public DbSet<KnowledgeMediaAsset> KnowledgeMediaAssets => Set<KnowledgeMediaAsset>();
    public DbSet<KnowledgeArticleReference> KnowledgeArticleReferences => Set<KnowledgeArticleReference>();
    public DbSet<FindingKnowledgeLink> FindingKnowledgeLinks => Set<FindingKnowledgeLink>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<LabScenario> LabScenarios => Set<LabScenario>();
    public DbSet<LabTargetSite> LabTargetSites => Set<LabTargetSite>();
    public DbSet<LabExecution> LabExecutions => Set<LabExecution>();
    public DbSet<LabEnvironment> LabEnvironments => Set<LabEnvironment>();
    public DbSet<LabExecutionStep> LabExecutionSteps => Set<LabExecutionStep>();
    public DbSet<LabExecutionLog> LabExecutionLogs => Set<LabExecutionLog>();
    public DbSet<LabComparisonResult> LabComparisonResults => Set<LabComparisonResult>();
    public DbSet<LabAuthorizationApproval> LabAuthorizationApprovals => Set<LabAuthorizationApproval>();
    public DbSet<LabElevationTicket> LabElevationTickets => Set<LabElevationTicket>();
    public DbSet<BugBountyProgram> BugBountyPrograms => Set<BugBountyProgram>();
    public DbSet<BugBountyPolicyRule> BugBountyPolicyRules => Set<BugBountyPolicyRule>();
    public DbSet<RootCauseGroup> RootCauseGroups => Set<RootCauseGroup>();
    public DbSet<HackerOneReportDraft> HackerOneReportDrafts => Set<HackerOneReportDraft>();
    public DbSet<HackerOneSubmissionRecord> HackerOneSubmissionRecords => Set<HackerOneSubmissionRecord>();
    public DbSet<BugBountyAuditLog> BugBountyAuditLogs => Set<BugBountyAuditLog>();
    public DbSet<ScanProfile> ScanProfiles => Set<ScanProfile>();
    public DbSet<HackerOneWorkspaceSettings> HackerOneWorkspaceSettings => Set<HackerOneWorkspaceSettings>();
    public DbSet<HackerOneApiCredential> HackerOneApiCredentials => Set<HackerOneApiCredential>();
    public DbSet<FindingValidationRun> FindingValidationRuns => Set<FindingValidationRun>();
    public DbSet<FindingValidationResult> FindingValidationResults => Set<FindingValidationResult>();
    public DbSet<ValidationEvidence> ValidationEvidence => Set<ValidationEvidence>();
    public DbSet<ScopePolicy> ScopePolicies => Set<ScopePolicy>();
    public DbSet<ValidationAuthorizationEvidence> ValidationAuthorizationEvidence => Set<ValidationAuthorizationEvidence>();
    public DbSet<TestAccountSession> TestAccountSessions => Set<TestAccountSession>();
    public DbSet<TargetTestAccount> TargetTestAccounts => Set<TargetTestAccount>();
    public DbSet<TestIdentityProfile> TestIdentityProfiles => Set<TestIdentityProfile>();
    public DbSet<AuthenticatedScanRun> AuthenticatedScanRuns => Set<AuthenticatedScanRun>();
    public DbSet<ScanModeObservation> ScanModeObservations => Set<ScanModeObservation>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        builder.Entity<ApplicationUser>().ToTable("Users");
        builder.Entity<ApplicationRole>().ToTable("Roles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<Guid>>().ToTable("UserTokens");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<Guid>>().ToTable("RoleClaims");

        ApplyTenantQueryFilters(builder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ApplyAuditFields();
        return base.SaveChanges();
    }

    private void ApplyAuditFields()
    {
        var now = _dateTimeProvider?.UtcNow ?? DateTime.UtcNow;
        var userId = _currentUser?.UserId;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is BaseEntity baseEntity)
            {
                if (entry.State == EntityState.Added)
                {
                    if (baseEntity.CreatedAt == default)
                    {
                        baseEntity.CreatedAt = now;
                    }
                }
                else if (entry.State == EntityState.Modified)
                {
                    baseEntity.UpdatedAt = now;
                }
            }

            if (entry.Entity is IAuditableEntity auditable)
            {
                if (entry.State == EntityState.Added && auditable.CreatedByUserId is null)
                {
                    auditable.CreatedByUserId = userId;
                }
                else if (entry.State == EntityState.Modified)
                {
                    auditable.UpdatedByUserId = userId;
                }
            }
        }
    }

    private void ApplyTenantQueryFilters(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(ITenantOwnedEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(SecurityPlatformDbContext)
                    .GetMethod(nameof(SetTenantQueryFilter), BindingFlags.NonPublic | BindingFlags.Instance)!
                    .MakeGenericMethod(entityType.ClrType);
                method.Invoke(this, [builder]);
            }
        }
    }

    private void SetTenantQueryFilter<TEntity>(ModelBuilder builder) where TEntity : class, ITenantOwnedEntity
    {
        builder.Entity<TEntity>().HasQueryFilter(entity =>
            _currentUser == null ||
            _currentUser.IsSystemAdmin ||
            _currentUser.CompanyId == null ||
            entity.CompanyId == _currentUser.CompanyId);
    }
}
