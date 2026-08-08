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
using Microsoft.EntityFrameworkCore;

namespace Kaan.SecurityPlatform.Application.Common.Interfaces;

/// <summary>
/// Application katmanının DbContext'e bağımlılığını Interface üzerinden yönetmesi
/// için tanımlanmış sözleşme. Infrastructure implementasyonu <c>SecurityPlatformDbContext</c>.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Company> Companies { get; }
    DbSet<CompanyUser> CompanyUsers { get; }
    DbSet<SecurityProject> SecurityProjects { get; }
    DbSet<DomainAsset> DomainAssets { get; }
    DbSet<AuthorizationRecord> AuthorizationRecords { get; }
    DbSet<ScanJob> ScanJobs { get; }
    DbSet<ScanResult> ScanResults { get; }
    DbSet<Finding> Findings { get; }
    DbSet<FindingStatusHistory> FindingStatusHistories { get; }
    DbSet<RemediationRequest> RemediationRequests { get; }
    DbSet<RetestComparison> RetestComparisons { get; }
    DbSet<SubscriptionPlan> SubscriptionPlans { get; }
    DbSet<CompanySubscription> CompanySubscriptions { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<KnowledgeCategory> KnowledgeCategories { get; }
    DbSet<KnowledgeArticle> KnowledgeArticles { get; }
    DbSet<KnowledgeMediaAsset> KnowledgeMediaAssets { get; }
    DbSet<KnowledgeArticleReference> KnowledgeArticleReferences { get; }
    DbSet<FindingKnowledgeLink> FindingKnowledgeLinks { get; }

    DbSet<LabScenario> LabScenarios { get; }
    DbSet<LabTargetSite> LabTargetSites { get; }
    DbSet<LabExecution> LabExecutions { get; }
    DbSet<LabEnvironment> LabEnvironments { get; }
    DbSet<LabExecutionStep> LabExecutionSteps { get; }
    DbSet<LabExecutionLog> LabExecutionLogs { get; }
    DbSet<LabComparisonResult> LabComparisonResults { get; }
    DbSet<LabAuthorizationApproval> LabAuthorizationApprovals { get; }
    DbSet<LabElevationTicket> LabElevationTickets { get; }

    DbSet<BugBountyProgram> BugBountyPrograms { get; }
    DbSet<BugBountyPolicyRule> BugBountyPolicyRules { get; }
    DbSet<RootCauseGroup> RootCauseGroups { get; }
    DbSet<HackerOneReportDraft> HackerOneReportDrafts { get; }
    DbSet<HackerOneSubmissionRecord> HackerOneSubmissionRecords { get; }
    DbSet<BugBountyAuditLog> BugBountyAuditLogs { get; }
    DbSet<ScanProfile> ScanProfiles { get; }
    DbSet<HackerOneWorkspaceSettings> HackerOneWorkspaceSettings { get; }
    DbSet<HackerOneApiCredential> HackerOneApiCredentials { get; }

    DbSet<FindingValidationRun> FindingValidationRuns { get; }
    DbSet<FindingValidationResult> FindingValidationResults { get; }
    DbSet<ValidationEvidence> ValidationEvidence { get; }
    DbSet<ScopePolicy> ScopePolicies { get; }
    DbSet<ValidationAuthorizationEvidence> ValidationAuthorizationEvidence { get; }
    DbSet<TestAccountSession> TestAccountSessions { get; }

    DbSet<TargetTestAccount> TargetTestAccounts { get; }
    DbSet<TestIdentityProfile> TestIdentityProfiles { get; }
    DbSet<AuthenticatedScanRun> AuthenticatedScanRuns { get; }
    DbSet<ScanModeObservation> ScanModeObservations { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
