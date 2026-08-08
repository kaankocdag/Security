using Kaan.SecurityPlatform.Application.Authorization;
using Kaan.SecurityPlatform.Application.Features.BugBounty;
using Kaan.SecurityPlatform.Domain.Entities.BugBounty;
using Kaan.SecurityPlatform.Domain.Entities.Companies;
using Kaan.SecurityPlatform.Domain.Entities.Findings;
using Kaan.SecurityPlatform.Domain.Entities.Knowledge;
using Kaan.SecurityPlatform.Domain.Entities.Projects;
using Kaan.SecurityPlatform.Domain.Entities.Scans;
using Kaan.SecurityPlatform.Domain.Enums;
using Kaan.SecurityPlatform.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kaan.SecurityPlatform.Infrastructure.Persistence.Seed;

public static class DatabaseSeeder
{
    public const string AdminEmail = "admin@kaansecurity.local";
    public const string AdminPassword = "Kaan!Admin2026#";
    public const string DemoCompanyName = "Demo Teknoloji A.Ş.";

    public static async Task SeedAsync(
        SecurityPlatformDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await SeedRolesAsync(roleManager);
        var admin = await SeedAdminAsync(userManager, logger);
        var demoCompany = await SeedDemoCompanyAsync(db, admin, cancellationToken);
        await EnsureAdminLinkedToCompanyAsync(db, userManager, admin, demoCompany, logger, cancellationToken);
        await SeedDemoProjectAsync(db, demoCompany, admin, cancellationToken);
        await SeedKnowledgeAsync(db, admin, cancellationToken);
        await SeedBugBountyAsync(db, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedBugBountyAsync(SecurityPlatformDbContext db, CancellationToken cancellationToken)
    {
        var amazon = await db.BugBountyPrograms.FirstOrDefaultAsync(
            p => p.PolicyKey == BugBountyProgramKeys.AmazonVrp, cancellationToken);
        if (amazon is null)
        {
            amazon = new BugBountyProgram
            {
                PolicyKey = BugBountyProgramKeys.AmazonVrp,
                Name = "Amazon Vulnerability Research Program",
                Handle = "amazonvrp",
                Platform = BugBountyPlatform.HackerOne,
                OpenReportUrl = "https://hackerone.com/amazonvrp",
                IsEnabled = true
            };
            db.BugBountyPrograms.Add(amazon);
            await db.SaveChangesAsync(cancellationToken);

            void Rule(BugBountyPolicyCategory cat, SubmissionRecommendation whenYes, SubmissionRecommendation whenNo, string? notes = null)
            {
                db.BugBountyPolicyRules.Add(new BugBountyPolicyRule
                {
                    BugBountyProgramId = amazon.Id,
                    PolicyCategory = cat,
                    RecommendationWhenDemonstrated = whenYes,
                    RecommendationWhenNotDemonstrated = whenNo,
                    Notes = notes
                });
            }

            Rule(BugBountyPolicyCategory.MissingSecurityHeaders, SubmissionRecommendation.DoNotSubmit, SubmissionRecommendation.DoNotSubmit);
            Rule(BugBountyPolicyCategory.MissingCookieFlags, SubmissionRecommendation.DoNotSubmit, SubmissionRecommendation.DoNotSubmit);
            Rule(BugBountyPolicyCategory.Clickjacking, SubmissionRecommendation.ManualReview, SubmissionRecommendation.DoNotSubmit);
            Rule(BugBountyPolicyCategory.ScannerOutputOnly, SubmissionRecommendation.DoNotSubmit, SubmissionRecommendation.DoNotSubmit);
            Rule(BugBountyPolicyCategory.InformationDisclosure, SubmissionRecommendation.ManualReview, SubmissionRecommendation.ManualReview);
            Rule(BugBountyPolicyCategory.MisconfigurationWithDemonstratedImpact, SubmissionRecommendation.ManualReview, SubmissionRecommendation.DoNotSubmit);
            Rule(BugBountyPolicyCategory.Xss, SubmissionRecommendation.Submit, SubmissionRecommendation.ManualReview);
            Rule(BugBountyPolicyCategory.SqlInjection, SubmissionRecommendation.Submit, SubmissionRecommendation.ManualReview);
            Rule(BugBountyPolicyCategory.Idor, SubmissionRecommendation.Submit, SubmissionRecommendation.ManualReview);
            Rule(BugBountyPolicyCategory.AuthenticationBypass, SubmissionRecommendation.Submit, SubmissionRecommendation.ManualReview);
            Rule(BugBountyPolicyCategory.PrivilegeEscalation, SubmissionRecommendation.Submit, SubmissionRecommendation.ManualReview);
        }

        if (!await db.ScanProfiles.AnyAsync(p => p.ProfileKey == "AmazonVRP", cancellationToken))
        {
            db.ScanProfiles.Add(new ScanProfile
            {
                ProfileKey = "AmazonVRP",
                DisplayName = "Amazon VRP Candidate Profile",
                UserAgentConfigKey = "HackerOne:AmazonVrp:UserAgent",
                RateLimitPerMinuteConfigKey = "HackerOne:AmazonVrp:RateLimitPerMinute",
                Notes = "UA ve rate-limit appsettings HackerOne:AmazonVrp altından okunur.",
                IsEnabled = true
            });
        }

        if (!await db.HackerOneWorkspaceSettings.AnyAsync(cancellationToken))
        {
            db.HackerOneWorkspaceSettings.Add(new HackerOneWorkspaceSettings
            {
                DefaultBugBountyProgramId = amazon.Id,
                OpenReportUrlTemplate = "https://hackerone.com/{handle}",
                MinReadinessScoreForSubmit = 70,
                PreferEnglishReports = true
            });
        }
    }

    private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager)
    {
        string[] roles =
        {
            Roles.SystemAdmin,
            Roles.CompanyAdmin,
            Roles.Developer,
            Roles.SecurityAnalyst,
            Roles.Viewer
        };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = role, NormalizedName = role.ToUpperInvariant() });
            }
        }
    }

    private static async Task<ApplicationUser> SeedAdminAsync(UserManager<ApplicationUser> userManager, ILogger logger)
    {
        var admin = await userManager.FindByEmailAsync(AdminEmail);
        if (admin is not null)
        {
            return admin;
        }

        admin = new ApplicationUser
        {
            UserName = AdminEmail,
            Email = AdminEmail,
            EmailConfirmed = true,
            FirstName = "Sistem",
            LastName = "Yöneticisi",
            MembershipStatus = MembershipStatus.Approved,
            ApprovedAt = DateTime.UtcNow
        };
        var createResult = await userManager.CreateAsync(admin, AdminPassword);
        if (!createResult.Succeeded)
        {
            logger.LogError("Admin kullanıcısı oluşturulamadı: {Errors}",
                string.Join(", ", createResult.Errors.Select(e => e.Description)));
            throw new InvalidOperationException("Admin oluşturulamadı.");
        }
        await userManager.AddToRoleAsync(admin, Roles.SystemAdmin);
        logger.LogInformation("Admin kullanıcısı oluşturuldu: {Email}", AdminEmail);
        return admin;
    }

    private static async Task<Company> SeedDemoCompanyAsync(SecurityPlatformDbContext db, ApplicationUser admin, CancellationToken cancellationToken)
    {
        var existing = await db.Companies.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Name == DemoCompanyName, cancellationToken);
        if (existing is not null) return existing;

        var company = new Company
        {
            Name = DemoCompanyName,
            ContactName = "Demo Yönetici",
            ContactEmail = "iletisim@demoteknoloji.com",
            Industry = "Teknoloji",
            Status = CompanyStatus.Active,
            ApprovedAt = DateTime.UtcNow,
            ApprovedByUserId = admin.Id
        };
        db.Companies.Add(company);
        await db.SaveChangesAsync(cancellationToken);
        return company;
    }

    /// <summary>
    /// SystemAdmin JWT'de CompanyId claim'i olsun diye demo firmaya bağlanır.
    /// Mevcut kurulumlarda da PrimaryCompanyId boşsa tamamlar.
    /// </summary>
    private static async Task EnsureAdminLinkedToCompanyAsync(
        SecurityPlatformDbContext db,
        UserManager<ApplicationUser> userManager,
        ApplicationUser admin,
        Company company,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (admin.PrimaryCompanyId != company.Id)
        {
            admin.PrimaryCompanyId = company.Id;
            await userManager.UpdateAsync(admin);
            logger.LogInformation("Admin {Email} firmaya bağlandı: {Company}", admin.Email, company.Name);
        }

        var membership = await db.CompanyUsers.IgnoreQueryFilters()
            .FirstOrDefaultAsync(cu => cu.UserId == admin.Id && cu.CompanyId == company.Id, cancellationToken);
        if (membership is null)
        {
            db.CompanyUsers.Add(new CompanyUser
            {
                CompanyId = company.Id,
                UserId = admin.Id,
                CompanyRole = CompanyRole.CompanyAdmin,
                IsPrimaryContact = true,
                IsActive = true
            });
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task SeedDemoProjectAsync(SecurityPlatformDbContext db, Company company, ApplicationUser admin, CancellationToken cancellationToken)
    {
        var existing = await db.SecurityProjects
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.CompanyId == company.Id, cancellationToken);
        if (existing is not null) return;

        var project = new SecurityProject
        {
            CompanyId = company.Id,
            Name = "Demo E-Ticaret Web",
            Description = "Demo firmanın üretim web uygulaması.",
            EnvironmentType = EnvironmentType.Production,
            Status = ProjectStatus.Active,
            PrimaryContactEmail = "guvenlik@demoteknoloji.com"
        };
        db.SecurityProjects.Add(project);
        await db.SaveChangesAsync(cancellationToken);

        var domain = new DomainAsset
        {
            CompanyId = company.Id,
            SecurityProjectId = project.Id,
            HostName = "demoteknoloji.com",
            NormalizedHostName = "demoteknoloji.com",
            Scheme = "https",
            Status = DomainAssetStatus.Verified,
            IsVerified = true,
            VerifiedAt = DateTime.UtcNow,
            VerificationMethod = VerificationMethod.Mock,
            VerificationToken = "demo-token"
        };
        db.DomainAssets.Add(domain);
        await db.SaveChangesAsync(cancellationToken);

        var scanJob = new ScanJob
        {
            CompanyId = company.Id,
            SecurityProjectId = project.Id,
            DomainAssetId = domain.Id,
            ScanType = ScanType.FullPassive,
            Status = ScanStatus.Completed,
            StartedAt = DateTime.UtcNow.AddMinutes(-8),
            CompletedAt = DateTime.UtcNow.AddMinutes(-2),
            ProgressPercentage = 100,
            TotalSteps = 9,
            CompletedSteps = 9,
            CurrentStep = "Tamamlandı",
            RequestedByUserId = admin.Id,
            ScannerVersion = "1.0.0"
        };
        db.ScanJobs.Add(scanJob);
        await db.SaveChangesAsync(cancellationToken);

        var scanResult = new ScanResult
        {
            CompanyId = company.Id,
            ScanJobId = scanJob.Id,
            SecurityScore = 68,
            StartedAt = scanJob.StartedAt!.Value,
            CompletedAt = scanJob.CompletedAt!.Value,
            Summary = "4 bulgu tespit edildi. Puan: 68/100 (C).",
            ExecutiveSummary = "Sitenizin güvenlik başlıkları eksik ve TLS yönlendirmesinde iyileştirme gerekiyor. Kritik bulgu yok, hızlıca ele alınabilir.",
            HighCount = 1,
            MediumCount = 2,
            LowCount = 1,
            ConfirmedCount = 4,
            ChecksTotal = 9,
            ChecksPassed = 5,
            ChecksFailed = 4
        };
        db.ScanResults.Add(scanResult);
        await db.SaveChangesAsync(cancellationToken);

        var findings = new List<Finding>
        {
            new Finding
            {
                CompanyId = company.Id,
                ScanResultId = scanResult.Id,
                Title = "HSTS başlığı yok",
                Description = "Sunucu Strict-Transport-Security başlığı göndermiyor. Kullanıcılar SSL stripping saldırılarına açık kalıyor.",
                Severity = Severity.High,
                ConfidenceLevel = ConfidenceLevel.Confirmed,
                Category = "Security Headers",
                CweCode = "CWE-319",
                OwaspCategory = "A05:2021 - Security Misconfiguration",
                AffectedUrl = "https://demoteknoloji.com/",
                Remediation = "Tüm HTTPS yanıtlara 'Strict-Transport-Security: max-age=63072000; includeSubDomains; preload' başlığını ekleyin.",
                RemediationExampleConfig = "add_header Strict-Transport-Security \"max-age=63072000; includeSubDomains; preload\" always;",
                TurkishExecutiveSummary = "Kullanıcı tarayıcısı sitenin sürekli HTTPS'de kalmasını öğrenemiyor. HSTS eklenmeli.",
                CheckCode = "http.security-headers",
                Fingerprint = "sh.hsts.missing"
            },
            new Finding
            {
                CompanyId = company.Id,
                ScanResultId = scanResult.Id,
                Title = "Content-Security-Policy başlığı yok",
                Description = "CSP başlığı bulunmuyor. XSS ve veri sızıntısı gibi tarayıcı tabanlı saldırılara karşı ek koruma katmanı devre dışı.",
                Severity = Severity.Medium,
                ConfidenceLevel = ConfidenceLevel.Confirmed,
                Category = "Security Headers",
                CweCode = "CWE-1021",
                AffectedUrl = "https://demoteknoloji.com/",
                Remediation = "İçeriğinize uygun bir CSP tanımlayın. 'default-src \\'self\\'' ile başlayıp gereken kaynakları ekleyin.",
                TurkishExecutiveSummary = "Tarayıcıya hangi kaynakların yükleneceği söylenmiyor.",
                CheckCode = "http.security-headers",
                Fingerprint = "sh.csp.missing"
            },
            new Finding
            {
                CompanyId = company.Id,
                ScanResultId = scanResult.Id,
                Title = "Clickjacking koruması yok",
                Description = "X-Frame-Options veya CSP frame-ancestors yok. Site iframe içine gömülebilir.",
                Severity = Severity.Medium,
                ConfidenceLevel = ConfidenceLevel.Confirmed,
                Category = "Security Headers",
                AffectedUrl = "https://demoteknoloji.com/",
                Remediation = "'X-Frame-Options: DENY' veya CSP 'frame-ancestors \\'none\\'' ekleyin.",
                CheckCode = "http.security-headers",
                Fingerprint = "sh.clickjacking.missing"
            },
            new Finding
            {
                CompanyId = company.Id,
                ScanResultId = scanResult.Id,
                Title = "security.txt eksik",
                Description = "'/.well-known/security.txt' bulunamadı. Güvenlik araştırmacılarının size ulaşabilmesi için önerilir.",
                Severity = Severity.Low,
                ConfidenceLevel = ConfidenceLevel.Confirmed,
                Category = "Discovery",
                AffectedUrl = "https://demoteknoloji.com/.well-known/security.txt",
                Remediation = "'/.well-known/security.txt' dosyasını RFC 9116 formatında yayınlayın.",
                CheckCode = "http.well-known",
                Fingerprint = "wellknown.security-txt.missing"
            }
        };
        db.Findings.AddRange(findings);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedKnowledgeAsync(SecurityPlatformDbContext db, ApplicationUser admin, CancellationToken cancellationToken)
    {
        if (await db.KnowledgeCategories.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            return;
        }

        var headers = new KnowledgeCategory
        {
            Slug = "security-headers",
            Name = "Güvenlik Başlıkları",
            Description = "HSTS, CSP, X-Frame-Options gibi HTTP güvenlik başlıkları rehberi.",
            IconName = "shield",
            DisplayOrder = 1
        };
        var injection = new KnowledgeCategory
        {
            Slug = "injection-attacks",
            Name = "Injection Saldırıları",
            Description = "SQL Injection, Command Injection, XSS gibi enjeksiyon saldırıları.",
            IconName = "bug",
            DisplayOrder = 2
        };
        var authentication = new KnowledgeCategory
        {
            Slug = "authentication",
            Name = "Kimlik Doğrulama ve Oturum",
            Description = "Şifre yönetimi, MFA, oturum güvenliği, JWT en iyi pratikleri.",
            IconName = "key",
            DisplayOrder = 3
        };
        var infra = new KnowledgeCategory
        {
            Slug = "infrastructure",
            Name = "Altyapı Sertleştirme",
            Description = "TLS, sertifika yönetimi, container güvenliği, konfigürasyon.",
            IconName = "server",
            DisplayOrder = 4
        };
        db.KnowledgeCategories.AddRange(headers, injection, authentication, infra);
        await db.SaveChangesAsync(cancellationToken);

        var articles = new[]
        {
            new KnowledgeArticle
            {
                CategoryId = headers.Id,
                Slug = "hsts-nedir",
                Title = "HSTS Nedir ve Neden Kritiktir?",
                Summary = "Strict-Transport-Security başlığının çalışma mantığı, doğru yapılandırma ve preload süreci.",
                BodyMarkdown = "## HSTS Nedir?\n\nHTTP Strict Transport Security (HSTS), tarayıcılara 'bu siteye yalnızca HTTPS ile bağlan' talimatı vermenizi sağlar. Doğru yapılandırılmadığında SSL stripping saldırılarına karşı savunmasız kalırsınız.\n\n### Örnek Nginx yapılandırması\n\n```\nadd_header Strict-Transport-Security \"max-age=63072000; includeSubDomains; preload\" always;\n```\n\n### Preload listesi\n\nSitenizi https://hstspreload.org üzerinden preload listesine kaydettirdiğinizde tarayıcılar ilk ziyaretten önce bile HTTPS'e zorlanır.",
                CweCode = "CWE-319",
                OwaspCategory = "A05:2021 - Security Misconfiguration",
                DifficultyLevel = DifficultyLevel.Beginner,
                EstimatedReadMinutes = 4,
                Tags = "hsts,tls,https,güvenlik başlıkları",
                IsPublished = true,
                PublishedAt = DateTime.UtcNow,
                AuthorUserId = admin.Id,
                IsFeatured = true
            },
            new KnowledgeArticle
            {
                CategoryId = headers.Id,
                Slug = "csp-uygulama-rehberi",
                Title = "Content Security Policy (CSP) Uygulama Rehberi",
                Summary = "Aşamalı CSP tanımlama, nonce/hash kullanımı, rapor toplama ve yaygın hatalar.",
                BodyMarkdown = "## CSP Nedir?\n\nCSP, tarayıcının hangi kaynaklardan içerik yükleyebileceğini kontrol etmenizi sağlayan güçlü bir güvenlik başlığıdır. XSS ve veri sızıntısı gibi saldırıların etkisini büyük ölçüde azaltır.",
                CweCode = "CWE-1021",
                OwaspCategory = "A05:2021 - Security Misconfiguration",
                DifficultyLevel = DifficultyLevel.Intermediate,
                EstimatedReadMinutes = 8,
                Tags = "csp,xss,güvenlik başlıkları",
                IsPublished = true,
                PublishedAt = DateTime.UtcNow,
                AuthorUserId = admin.Id
            },
            new KnowledgeArticle
            {
                CategoryId = injection.Id,
                Slug = "sql-injection-101",
                Title = "SQL Injection 101: Neden Hâlâ Konuşuyoruz?",
                Summary = "Parametreli sorgular, ORM en iyi pratikleri ve gerçek dünya örnekleri.",
                BodyMarkdown = "## SQL Injection nedir?\n\nSaldırganların kullanıcı girdisi üzerinden SQL komutlarını değiştirebilmesidir. En kolay çözüm: parametreli sorgular ve ORM kullanımı.",
                CweCode = "CWE-89",
                OwaspCategory = "A03:2021 - Injection",
                DifficultyLevel = DifficultyLevel.Beginner,
                EstimatedReadMinutes = 6,
                Tags = "sql,injection,orm",
                IsPublished = true,
                PublishedAt = DateTime.UtcNow,
                AuthorUserId = admin.Id
            },
            new KnowledgeArticle
            {
                CategoryId = authentication.Id,
                Slug = "jwt-guvenli-kullanim",
                Title = "JWT'yi Güvenli Kullanmak",
                Summary = "İmzalama algoritmaları, refresh token rotasyonu, revocation stratejileri ve saklama.",
                BodyMarkdown = "## JWT nedir?\n\nJSON Web Token, taşıyıcı bir token formatıdır. Doğru kullanılmadığında oturum çalma saldırılarına açık kalır.",
                DifficultyLevel = DifficultyLevel.Intermediate,
                EstimatedReadMinutes = 10,
                Tags = "jwt,auth,session",
                IsPublished = true,
                PublishedAt = DateTime.UtcNow,
                AuthorUserId = admin.Id,
                IsFeatured = true
            }
        };
        db.KnowledgeArticles.AddRange(articles);
        await db.SaveChangesAsync(cancellationToken);
    }
}
