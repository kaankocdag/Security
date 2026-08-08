using DnsClient;
using Hangfire;
using Hangfire.MemoryStorage;
using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Application.Features.Auth;
using Kaan.SecurityPlatform.Application.Features.Admin;
using Kaan.SecurityPlatform.Application.Features.Domains;
using Kaan.SecurityPlatform.Application.Features.Findings;
using Kaan.SecurityPlatform.Application.Features.Knowledge;
using Kaan.SecurityPlatform.Application.Features.Projects;
using Kaan.SecurityPlatform.Application.Features.Reports;
using Kaan.SecurityPlatform.Application.Features.Scans;
using Kaan.SecurityPlatform.Application.Common.Interfaces.Lab;
using Kaan.SecurityPlatform.Application.Features.BugBounty;
using Kaan.SecurityPlatform.Application.Features.HackerOne;
using Kaan.SecurityPlatform.Application.Features.AuthenticatedScanning;
using Kaan.SecurityPlatform.Application.Features.Validation;
using Kaan.SecurityPlatform.Infrastructure.Admin;
using Kaan.SecurityPlatform.Infrastructure.AuthenticatedScanning;
using Kaan.SecurityPlatform.Infrastructure.BugBounty;
using Kaan.SecurityPlatform.Infrastructure.HackerOne;
using Kaan.SecurityPlatform.Infrastructure.HackerOne.Engines;
using Kaan.SecurityPlatform.Infrastructure.Validation;
using Kaan.SecurityPlatform.Infrastructure.Validation.Validators;
using Microsoft.AspNetCore.DataProtection;
using Kaan.SecurityPlatform.Infrastructure.Lab;
using Kaan.SecurityPlatform.Infrastructure.Lab.Runtime;
using Kaan.SecurityPlatform.Infrastructure.Lab.Scenarios;
using Kaan.SecurityPlatform.Infrastructure.Authentication;
using Kaan.SecurityPlatform.Infrastructure.Domains;
using Kaan.SecurityPlatform.Infrastructure.Findings;
using Kaan.SecurityPlatform.Infrastructure.Knowledge;
using Kaan.SecurityPlatform.Infrastructure.Projects;
using Kaan.SecurityPlatform.Infrastructure.Reporting;
using Kaan.SecurityPlatform.Infrastructure.DomainVerification;
using Kaan.SecurityPlatform.Infrastructure.DomainVerification.Strategies;
using Kaan.SecurityPlatform.Infrastructure.Identity;
using Kaan.SecurityPlatform.Infrastructure.Persistence;
using Kaan.SecurityPlatform.Infrastructure.Scanning;
using Kaan.SecurityPlatform.Infrastructure.Scanning.Checks;
using Kaan.SecurityPlatform.Infrastructure.Scanning.Executor;
using Kaan.SecurityPlatform.Infrastructure.Scanning.Http;
using Kaan.SecurityPlatform.Infrastructure.Scanning.Queue;
using Kaan.SecurityPlatform.Infrastructure.Scanning.Safety;
using Kaan.SecurityPlatform.Infrastructure.Scanning.Suggestions;
using Kaan.SecurityPlatform.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Kaan.SecurityPlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        bool registerHangfireServer = false,
        bool registerHangfireStorage = true,
        string[]? hangfireQueues = null,
        bool registerLabCleanupHostedService = false)
    {
        services.AddDbContext<SecurityPlatformDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("Default")
                ?? "Server=(localdb)\\MSSQLLocalDB;Database=KaanSecurityPlatform;Trusted_Connection=True;Encrypt=False;MultipleActiveResultSets=true";
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(SecurityPlatformDbContext).Assembly.FullName);
                sql.EnableRetryOnFailure(3);
            });
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<SecurityPlatformDbContext>());

        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 10;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.SignIn.RequireConfirmedEmail = false;
        })
        .AddRoles<ApplicationRole>()
        .AddEntityFrameworkStores<SecurityPlatformDbContext>()
        .AddDefaultTokenProviders();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IMembershipApprovalService, MembershipApprovalService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IDomainAssetService, DomainAssetService>();
        services.AddScoped<IScanService, ScanService>();
        services.AddScoped<IFindingService, FindingService>();
        services.AddScoped<IKnowledgeService, KnowledgeService>();
        services.AddScoped<IReportService, ReportService>();

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<IAuditLogWriter, AuditLogWriter>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddSingleton<ISecurityScoreCalculator, SecurityScoreCalculator>();
        services.AddSingleton<IReportExporter, HtmlReportExporter>();
        services.AddSingleton<IReportExporter, TextReportExporter>();
        services.AddScoped<IRemediationSuggestionService, RuleBasedRemediationSuggestionService>();

        services.AddSingleton<ILookupClient>(_ => new LookupClient(new LookupClientOptions
        {
            UseCache = true,
            Timeout = TimeSpan.FromSeconds(3),
            Retries = 1
        }));
        services.AddSingleton<ITargetSafetyValidator, TargetSafetyValidator>();
        services.AddSingleton<SecureHttpClientFactory>();

        services.Configure<DomainVerificationOptions>(configuration.GetSection(DomainVerificationOptions.SectionName));
        services.AddScoped<IVerificationStrategy, DnsTxtVerificationStrategy>();
        services.AddScoped<IVerificationStrategy, HtmlFileVerificationStrategy>();
        services.AddScoped<IVerificationStrategy, MetaTagVerificationStrategy>();
        services.AddScoped<IVerificationStrategy, MockDomainVerificationStrategy>();
        services.AddScoped<IDomainVerificationService, CompositeDomainVerificationService>();

        services.AddScoped<IPassiveSecurityCheck, HttpsCheck>();
        services.AddScoped<IPassiveSecurityCheck, HttpsRedirectCheck>();
        services.AddScoped<IPassiveSecurityCheck, CertificateCheck>();
        services.AddScoped<IPassiveSecurityCheck, SecurityHeadersCheck>();
        services.AddScoped<IPassiveSecurityCheck, CookieSecurityCheck>();
        services.AddScoped<IPassiveSecurityCheck, CorsConfigurationCheck>();
        services.AddScoped<IPassiveSecurityCheck, MixedContentIndicatorCheck>();
        services.AddScoped<IPassiveSecurityCheck, ErrorMessageLeakCheck>();
        services.AddScoped<IPassiveSecurityCheck, WellKnownFileCheck>();

        services.AddScoped<PassiveScanExecutor>();
        services.AddScoped<ApplicationSecurityCandidateExecutor>();
        services.AddScoped<IScanExecutor, RoutingScanExecutor>();
        services.AddSingleton<IAssessmentModeGuard, AssessmentModeGuard>();
        services.AddSingleton<IBugBountyProgramPolicy, AmazonVrpPolicy>();
        services.AddSingleton<IFindingValidationClassifier, FindingValidationClassifier>();

        services.Configure<HackerOneOptions>(configuration.GetSection(HackerOneOptions.SectionName));
        services.AddDataProtection();
        services.AddHttpClient("HackerOne");
        services.AddSingleton<IHackerOneMarkdownBuilder, HackerOneMarkdownBuilder>();
        services.AddSingleton<IHackerOneSecretProtector, HackerOneSecretProtector>();
        services.AddScoped<IBugBountyAuditWriter, BugBountyAuditWriter>();
        services.AddScoped<IRootCauseGroupService, RootCauseGroupService>();
        services.AddScoped<NullHackerOneApiClient>();
        services.AddScoped<HttpHackerOneApiClient>();
        services.AddScoped<IHackerOneApiClient, FeatureFlagHackerOneApiClient>();
        services.AddScoped<IHackerOneWorkspaceService, HackerOneWorkspaceService>();
        services.AddScoped<IHackerOneScopeSyncJob, HackerOneScopeSyncJob>();
        services.AddSingleton<ISensitiveSurfaceAnalyzer, SensitiveSurfaceAnalyzer>();
        services.AddScoped<IApplicationSecurityCandidateEngine, AccessControlCandidateEngine>();
        services.AddScoped<IApplicationSecurityCandidateEngine, XssReflectionCandidateEngine>();
        services.AddScoped<IApplicationSecurityCandidateEngine, CorsMisconfigCandidateEngine>();
        services.AddScoped<IApplicationSecurityCandidateEngine, InfoDisclosureCandidateEngine>();
        services.AddScoped<IApplicationSecurityCandidateEngine, SubdomainTakeoverCandidateEngine>();
        services.AddScoped<IApplicationSecurityCandidateEngine, JsSecretExposureCandidateEngine>();
        services.AddScoped<IApplicationSecurityCandidateEngine, ApiSurfaceCandidateEngine>();
        services.AddScoped<IApplicationSecurityCandidateEngine, OpenRedirectCandidateEngine>();

        services.Configure<ValidationOptions>(configuration.GetSection(ValidationOptions.SectionName));
        services.AddSingleton<IEvidenceRedactor, EvidenceRedactor>();
        services.AddSingleton<ITestAccountSecretProtector, TestAccountSecretProtector>();
        services.AddScoped<IEvidenceCollector, EvidenceCollector>();
        services.AddScoped<IScopePolicyValidator, ScopePolicyValidator>();
        services.AddScoped<IAuthorizationEvidenceService, AuthorizationEvidenceService>();
        services.AddScoped<IValidationPolicyEngine, ValidationPolicyEngine>();
        services.AddScoped<IImpactAssessmentService, ImpactAssessmentService>();
        services.AddScoped<ISubmissionEligibilityEvaluator, SubmissionEligibilityEvaluator>();
        services.AddScoped<IValidationAuditService, ValidationAuditService>();
        services.AddScoped<IValidationRunService, ValidationRunService>();
        services.AddScoped<ValidationHttpGate>();
        services.AddScoped<IValidationHttpGate>(sp => sp.GetRequiredService<ValidationHttpGate>());
        services.AddScoped<AccessControlCandidateValidator>();
        services.AddScoped<SecurityHeadersValidator>();
        services.AddScoped<CorsConfigurationValidator>();
        services.AddScoped<OpenRedirectValidator>();
        services.AddScoped<CookieConfigurationValidator>();
        services.AddScoped<TlsConfigurationValidator>();
        services.AddScoped<ManualOnlyValidator>();
        services.AddScoped<IFindingValidator>(sp => sp.GetRequiredService<AccessControlCandidateValidator>());
        services.AddScoped<IFindingValidator>(sp => sp.GetRequiredService<SecurityHeadersValidator>());
        services.AddScoped<IFindingValidator>(sp => sp.GetRequiredService<CorsConfigurationValidator>());
        services.AddScoped<IFindingValidator>(sp => sp.GetRequiredService<OpenRedirectValidator>());
        services.AddScoped<IFindingValidator>(sp => sp.GetRequiredService<CookieConfigurationValidator>());
        services.AddScoped<IFindingValidator>(sp => sp.GetRequiredService<TlsConfigurationValidator>());
        services.AddScoped<IFindingValidator>(sp => sp.GetRequiredService<ManualOnlyValidator>());
        services.AddScoped<IValidatorRegistry, ValidatorRegistry>();
        services.AddScoped<IFindingValidationOrchestrator, FindingValidationOrchestrator>();
        services.AddScoped<IValidationCatalogService, ValidationCatalogService>();

        services.Configure<AuthenticatedScanOptions>(configuration.GetSection(AuthenticatedScanOptions.SectionName));
        services.AddSingleton<ILoginPageDetector, LoginPageDetector>();
        services.AddSingleton<IRegistrationPageDetector, RegistrationPageDetector>();
        services.AddSingleton<IRegistrationFormAnalyzer, RegistrationFormAnalyzer>();
        services.AddSingleton<ILoginFormAnalyzer, LoginFormAnalyzer>();
        services.AddSingleton<IAuthenticationStateDetector, AuthenticationStateDetector>();
        services.AddSingleton<IAuthenticatedEvidenceRedactor, AuthenticatedEvidenceRedactor>();
        services.AddSingleton<IManualTakeoverService, ManualTakeoverService>();
        services.AddSingleton<ILoginPageDiscoveryService, LoginPageDiscoveryService>();
        services.AddSingleton<ITestAccountVault, TestAccountVault>();
        services.AddSingleton<IRegistrationFormFiller, RegistrationFormFiller>();
        services.AddSingleton<IAutomatedLoginService, AutomatedLoginService>();
        services.AddSingleton<IAuthenticatedCrawlService, AuthenticatedCrawlService>();
        services.AddSingleton<IScanSessionCleanupService, ScanSessionCleanupService>();
        services.AddSingleton<ITestIdentityGenerator, TestIdentityGenerator>();
        services.AddScoped<IBrowserSessionService, BrowserSessionService>();
        services.AddSingleton<BrowserSessionHoldService>();
        services.AddSingleton<IBrowserSessionHoldService>(sp => sp.GetRequiredService<BrowserSessionHoldService>());
        services.AddScoped<ITestAccountManagementService, TestAccountManagementService>();
        services.AddScoped<IAuthenticatedScanOrchestrator, AuthenticatedScanOrchestrator>();

        services.Configure<LabOptions>(configuration.GetSection(LabOptions.SectionName));
        services.AddSingleton<ILabScenario, InputValidationFailureScenario>();
        services.AddSingleton<ILabScenario, OutputEncodingFailureScenario>();
        services.AddSingleton<ILabScenario, InsecureSessionConfigScenario>();
        services.AddSingleton<ILabScenario, BrokenAccessControlScenario>();
        services.AddSingleton<ILabScenario, InsecureFileValidationScenario>();
        services.AddSingleton<ILabScenario, InsecureJwtScenario>();
        services.AddSingleton<ILabScenario, MissingSecurityHeadersScenario>();
        services.AddSingleton<ILabScenario, UnsafeQueryConstructionScenario>();
        services.AddSingleton<ILabScenarioRegistry, LabScenarioRegistry>();
        services.AddSingleton<ILabStartRequestGuard, LabStartRequestGuard>();
        services.AddSingleton<ILabNetworkPolicyValidator, LabNetworkPolicyValidator>();
        services.AddScoped<ILabAuditService, LabAuditService>();
        services.AddScoped<MockLabRuntime>();
        services.AddScoped<DockerLabRuntime>();
        services.AddScoped<ILabEnvironmentService, LabEnvironmentService>();
        services.AddScoped<ILabCleanupService, LabCleanupService>();
        services.AddScoped<ILabExecutionRunner, LabExecutionRunner>();
        services.AddScoped<ILabExecutionService, LabExecutionService>();

        if (registerLabCleanupHostedService)
        {
            services.AddHostedService<LabCleanupHostedService>();
        }

        if (registerHangfireStorage)
        {
            services.AddHangfire(cfg =>
            {
                cfg.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                   .UseSimpleAssemblyNameTypeSerializer()
                   .UseRecommendedSerializerSettings();

                var hangfireConn = configuration.GetConnectionString("Hangfire");
                if (string.IsNullOrWhiteSpace(hangfireConn))
                {
                    cfg.UseMemoryStorage();
                }
                else
                {
                    cfg.UseSqlServerStorage(hangfireConn);
                }
            });
            services.AddScoped<IScanQueue, HangfireScanQueue>();
            services.AddScoped<ILabQueue, HangfireLabQueue>();
        }

        if (registerHangfireServer)
        {
            var queues = hangfireQueues is { Length: > 0 }
                ? hangfireQueues
                : new[] { "default", "scans" };
            var serverName = queues.Contains("labs") && !queues.Contains("default")
                ? "kaan-lab-worker"
                : queues.Contains("labs")
                    ? "kaan-api-dev-worker"
                    : "kaan-scanner-worker";

            services.AddHangfireServer(options =>
            {
                options.ServerName = serverName;
                options.WorkerCount = Math.Max(1, Environment.ProcessorCount / 2);
                options.Queues = queues;
            });
        }

        return services;
    }

    /// <summary>
    /// Development + boş Hangfire bağlantısı: Api içinde tarama/lab işleyicisi.
    /// MemoryStorage paylaşılamadığı için ScannerWorker/LabWorker job görmez.
    /// </summary>
    public static IServiceCollection AddDevelopmentInProcessWorkers(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
        {
            return services;
        }

        if (!string.IsNullOrWhiteSpace(configuration.GetConnectionString("Hangfire")))
        {
            return services;
        }

        services.AddHangfireServer(options =>
        {
            options.ServerName = "kaan-api-dev-worker";
            options.WorkerCount = Math.Max(1, Environment.ProcessorCount / 2);
            options.Queues = ["default", "scans", "labs"];
        });
        services.AddHostedService<QueuedScanRecoveryHostedService>();
        return services;
    }
}
