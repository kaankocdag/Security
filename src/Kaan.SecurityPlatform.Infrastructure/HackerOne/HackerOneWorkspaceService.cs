using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Application.Common.Models;
using Kaan.SecurityPlatform.Application.Features.BugBounty;
using Kaan.SecurityPlatform.Application.Features.HackerOne;
using Kaan.SecurityPlatform.Application.Features.HackerOne.Dtos;
using Kaan.SecurityPlatform.Application.Features.Lab;
using Kaan.SecurityPlatform.Application.Features.Scans;
using Kaan.SecurityPlatform.Application.Features.Scans.Dtos;
using Kaan.SecurityPlatform.Domain.Entities.BugBounty;
using Kaan.SecurityPlatform.Domain.Entities.Findings;
using Kaan.SecurityPlatform.Domain.Entities.Projects;
using Kaan.SecurityPlatform.Domain.Enums;
using Kaan.SecurityPlatform.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kaan.SecurityPlatform.Infrastructure.HackerOne;

public sealed class HackerOneWorkspaceService : IHackerOneWorkspaceService
{
    public const string HackerOneTargetsProjectName = "HackerOne Bug Bounty Targets";

    private static readonly HashSet<string> WebAssetTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "DOMAIN",
        "URL",
        "WILDCARD",
        "OTHER"
    };

    private readonly IApplicationDbContext _db;
    private readonly IHackerOneMarkdownBuilder _markdown;
    private readonly IHackerOneApiClient _apiClient;
    private readonly IHackerOneSecretProtector _protector;
    private readonly IBugBountyAuditWriter _audit;
    private readonly IScanService _scanService;
    private readonly HackerOneOptions _options;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<HackerOneWorkspaceService> _logger;

    public HackerOneWorkspaceService(
        IApplicationDbContext db,
        IHackerOneMarkdownBuilder markdown,
        IHackerOneApiClient apiClient,
        IHackerOneSecretProtector protector,
        IBugBountyAuditWriter audit,
        IScanService scanService,
        IOptions<HackerOneOptions> options,
        ICurrentUser currentUser,
        ILogger<HackerOneWorkspaceService> logger)
    {
        _db = db;
        _markdown = markdown;
        _apiClient = apiClient;
        _protector = protector;
        _audit = audit;
        _scanService = scanService;
        _options = options.Value;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<HackerOneOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var candidates = _db.Findings.Where(f =>
            f.SubmissionRecommendation == SubmissionRecommendation.Submit
            || f.SubmissionRecommendation == SubmissionRecommendation.ManualReview);

        var settings = await EnsureSettingsAsync(cancellationToken);
        string? handle = null;
        if (settings.DefaultBugBountyProgramId is Guid pid)
        {
            handle = await _db.BugBountyPrograms.Where(p => p.Id == pid).Select(p => p.Handle).FirstOrDefaultAsync(cancellationToken);
        }

        return new HackerOneOverviewDto(
            await candidates.CountAsync(cancellationToken),
            await candidates.CountAsync(f => f.SubmissionRecommendation == SubmissionRecommendation.Submit, cancellationToken),
            await candidates.CountAsync(f => f.SubmissionRecommendation == SubmissionRecommendation.ManualReview, cancellationToken),
            await _db.HackerOneReportDrafts.CountAsync(cancellationToken),
            await _db.HackerOneReportDrafts.CountAsync(d => d.Status == HackerOneReportDraftStatus.Ready, cancellationToken),
            await _db.HackerOneSubmissionRecords.CountAsync(cancellationToken),
            await _db.Findings.CountAsync(f => f.BugBountyEligible, cancellationToken),
            _options.ApiEnabled,
            handle);
    }

    public async Task<IReadOnlyList<HackerOneCandidateDto>> ListCandidatesAsync(
        SubmissionRecommendation? recommendation = null,
        string? programPolicyKey = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Findings.AsQueryable()
            .Where(f =>
                f.SubmissionRecommendation == SubmissionRecommendation.Submit
                || f.SubmissionRecommendation == SubmissionRecommendation.ManualReview);

        if (recommendation is SubmissionRecommendation rec
            && rec is SubmissionRecommendation.Submit or SubmissionRecommendation.ManualReview)
        {
            query = query.Where(f => f.SubmissionRecommendation == rec);
        }

        if (!string.IsNullOrWhiteSpace(programPolicyKey))
        {
            query = query.Where(f => f.ProgramPolicyMatch == programPolicyKey);
        }

        return await query
            .OrderByDescending(f => f.BugBountyEligible)
            .ThenByDescending(f => f.TechnicalSeverity)
            .ThenByDescending(f => f.LastSeenAt)
            .Select(f => new HackerOneCandidateDto(
                f.Id,
                f.Title,
                f.TechnicalSeverity,
                f.FindingClass,
                f.SubmissionRecommendation,
                f.BugBountyEligible,
                f.DemonstratedImpact,
                f.ConfirmedVulnerability,
                f.SubmissionEligible,
                f.PotentialRewardEligible,
                f.LatestValidationStatus,
                f.ProgramPolicyMatch,
                f.ScanResult != null && f.ScanResult.ScanJob != null && f.ScanResult.ScanJob.DomainAsset != null
                    ? f.ScanResult.ScanJob.DomainAsset.HostName
                    : null,
                f.AffectedUrl,
                f.Fingerprint,
                f.RootCauseGroupId,
                f.EligibilityReason,
                f.LastSeenAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BugBountyProgramDto>> ListProgramsAsync(CancellationToken cancellationToken = default)
    {
        var programs = await _db.BugBountyPrograms
            .Include(p => p.PolicyRules)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
        return programs.Select(MapProgram).ToList();
    }

    public async Task<Result<BugBountyProgramDto>> GetProgramAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var program = await _db.BugBountyPrograms.Include(p => p.PolicyRules)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        return program is null
            ? Result<BugBountyProgramDto>.Failure("program_not_found", "Program bulunamadı.")
            : Result<BugBountyProgramDto>.Success(MapProgram(program));
    }

    public async Task<Result<BugBountyProgramDto>> UpdateProgramEnabledAsync(
        Guid id,
        bool isEnabled,
        CancellationToken cancellationToken = default)
    {
        var program = await _db.BugBountyPrograms.Include(p => p.PolicyRules)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (program is null)
        {
            return Result<BugBountyProgramDto>.Failure("program_not_found", "Program bulunamadı.");
        }

        program.IsEnabled = isEnabled;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync("program.enabled_changed", "BugBountyProgram", id.ToString(), new { isEnabled }, cancellationToken);
        return Result<BugBountyProgramDto>.Success(MapProgram(program));
    }

    public async Task<HackerOneWorkspaceSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await EnsureSettingsAsync(cancellationToken);
        var cred = await _db.HackerOneApiCredentials.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Identifier == "default", cancellationToken);
        var hasToken = cred is not null && !string.IsNullOrWhiteSpace(cred.ProtectedApiToken);
        var hasIdentifier = !string.IsNullOrWhiteSpace(cred?.ApiUsername);
        var hint = hasIdentifier ? MaskIdentifier(cred!.ApiUsername!) : null;
        return MapSettings(settings, hasToken, hasIdentifier, hint);
    }

    private static string MaskIdentifier(string value)
    {
        if (value.Length <= 2)
        {
            return "**";
        }

        return value[0] + new string('*', Math.Min(6, value.Length - 1));
    }

    public async Task<Result<HackerOneWorkspaceSettingsDto>> UpdateSettingsAsync(
        UpdateHackerOneWorkspaceSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var settings = await EnsureSettingsAsync(cancellationToken);
        if (request.DefaultBugBountyProgramId is not null)
        {
            settings.DefaultBugBountyProgramId = request.DefaultBugBountyProgramId;
        }

        if (!string.IsNullOrWhiteSpace(request.OpenReportUrlTemplate))
        {
            settings.OpenReportUrlTemplate = request.OpenReportUrlTemplate.Trim();
        }

        if (request.MinReadinessScoreForSubmit is int min)
        {
            settings.MinReadinessScoreForSubmit = Math.Clamp(min, 0, 100);
        }

        if (request.PreferEnglishReports is bool prefer)
        {
            settings.PreferEnglishReports = prefer;
        }

        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync("settings.updated", "HackerOneWorkspaceSettings", settings.Id.ToString(), request, cancellationToken);
        var refreshed = await GetSettingsAsync(cancellationToken);
        return Result<HackerOneWorkspaceSettingsDto>.Success(refreshed);
    }

    public async Task<Result> SetApiTokenAsync(SetHackerOneApiTokenRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ApiUsername))
        {
            return Result.Failure(
                "token_identifier_required",
                "HackerOne kullanıcı adın (handle) gerekli = HTTP Basic Auth kullanıcı adı. E-posta değil; kişisel token’da ayrı isim sorulmaz.");
        }

        if (string.IsNullOrWhiteSpace(request.ApiToken))
        {
            return Result.Failure("token_required", "API token değeri gerekli (HTTP Basic şifresi).");
        }

        var cred = await _db.HackerOneApiCredentials.FirstOrDefaultAsync(c => c.Identifier == "default", cancellationToken);
        if (cred is null)
        {
            cred = new HackerOneApiCredential { Identifier = "default" };
            _db.HackerOneApiCredentials.Add(cred);
        }

        cred.ProtectedApiToken = _protector.Protect(request.ApiToken.Trim());
        cred.ApiUsername = request.ApiUsername.Trim();
        cred.UpdatedByUserId = _currentUser.UserId;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync("api_token.set", "HackerOneApiCredential", cred.Id.ToString(),
            new { hasUsername = true }, cancellationToken);
        return Result.Success();
    }

    public async Task<Result> ClearApiTokenAsync(CancellationToken cancellationToken = default)
    {
        var cred = await _db.HackerOneApiCredentials.FirstOrDefaultAsync(c => c.Identifier == "default", cancellationToken);
        if (cred is not null)
        {
            _db.HackerOneApiCredentials.Remove(cred);
            await _db.SaveChangesAsync(cancellationToken);
            await _audit.WriteAsync("api_token.cleared", "HackerOneApiCredential", cred.Id.ToString(), null, cancellationToken);
        }

        return Result.Success();
    }

    public async Task<Result<int>> SyncProgramsAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.ApiEnabled)
        {
            await _audit.WriteAsync("programs.sync_rejected", "BugBountyProgram", null, new { reason = "api_disabled" }, cancellationToken);
            return Result<int>.Failure("hackerone_api_disabled", "HackerOne API kapalı (HackerOne:ApiEnabled=false).");
        }

        var remote = await _apiClient.ListProgramsAsync(cancellationToken);
        if (remote.IsFailure)
        {
            await _audit.WriteAsync("programs.sync_failed", "BugBountyProgram", null, new { remote.ErrorCode }, cancellationToken);
            return Result<int>.Failure(remote.ErrorCode!, remote.ErrorMessage!);
        }

        var updated = 0;
        foreach (var item in remote.Value!)
        {
            var existing = await _db.BugBountyPrograms.FirstOrDefaultAsync(
                p => p.Handle == item.Handle || p.ExternalProgramId == item.ExternalId, cancellationToken);
            if (existing is null)
            {
                _db.BugBountyPrograms.Add(new BugBountyProgram
                {
                    PolicyKey = $"H1_{item.Handle}",
                    Name = item.Name,
                    Handle = item.Handle,
                    Platform = BugBountyPlatform.HackerOne,
                    OpenReportUrl = _options.OpenReportUrlTemplate.Replace("{handle}", item.Handle, StringComparison.OrdinalIgnoreCase),
                    ExternalProgramId = item.ExternalId,
                    LastSyncedAt = DateTime.UtcNow,
                    IsEnabled = false,
                    OffersBounties = item.OffersBounties,
                    Currency = item.Currency,
                    SubmissionState = item.SubmissionState,
                    OpenScope = item.OpenScope,
                    State = item.State
                });
                updated++;
            }
            else
            {
                existing.Name = item.Name;
                existing.ExternalProgramId = item.ExternalId;
                existing.LastSyncedAt = DateTime.UtcNow;
                existing.OffersBounties = item.OffersBounties;
                existing.Currency = item.Currency;
                existing.SubmissionState = item.SubmissionState;
                existing.OpenScope = item.OpenScope;
                existing.State = item.State;
                updated++;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync("programs.synced", "BugBountyProgram", null, new { count = updated }, cancellationToken);
        return Result<int>.Success(updated);
    }

    public async Task<Result<HackerOneScopeSyncResultDto>> SyncScopesToDomainsAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.ApiEnabled)
        {
            await _audit.WriteAsync("scopes.sync_rejected", "DomainAsset", null, new { reason = "api_disabled" }, cancellationToken);
            return Result<HackerOneScopeSyncResultDto>.Failure(
                "hackerone_api_disabled",
                "HackerOne API kapalı (HackerOne:ApiEnabled=false).");
        }

        var programSync = await SyncProgramsAsync(cancellationToken);
        if (programSync.IsFailure)
        {
            return Result<HackerOneScopeSyncResultDto>.Failure(programSync.ErrorCode!, programSync.ErrorMessage!);
        }

        var project = await EnsureHackerOneTargetsProjectAsync(cancellationToken);
        if (project is null)
        {
            return Result<HackerOneScopeSyncResultDto>.Failure(
                "hackerone_project_missing",
                "HackerOne hedef projesi oluşturulamadı (demo firma bulunamadı).");
        }

        var programs = await _db.BugBountyPrograms
            .Where(p => p.Platform == BugBountyPlatform.HackerOne)
            .OrderBy(p => p.Handle)
            .ToListAsync(cancellationToken);

        // Aynı program + host birden fazla scope'ta gelebilir (DOMAIN + URL).
        // Tenant filter Hangfire'da mevcut satırları gizlerse duplicate INSERT olur → IgnoreQueryFilters.
        var domainByKey = new Dictionary<string, DomainAsset>(StringComparer.OrdinalIgnoreCase);
        await ReloadHackerOneDomainCacheAsync(project.Id, domainByKey, cancellationToken);

        var programsProcessed = 0;
        var scopesSeen = 0;
        var upserted = 0;
        var skipped = 0;
        var now = DateTime.UtcNow;

        foreach (var program in programs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var scopesResult = await _apiClient.ListStructuredScopesAsync(program.Handle, cancellationToken);
            programsProcessed++;
            if (scopesResult.IsFailure)
            {
                _logger.LogWarning(
                    "HackerOne scope sync skipped for {Handle}: {Code} {Message}",
                    program.Handle,
                    scopesResult.ErrorCode,
                    scopesResult.ErrorMessage);
                continue;
            }

            foreach (var scope in scopesResult.Value!)
            {
                scopesSeen++;
                if (!IsWebScope(scope.AssetType, scope.AssetIdentifier))
                {
                    skipped++;
                    continue;
                }

                var parsed = TryParseWebAsset(scope.AssetIdentifier, scope.AssetType);
                if (parsed is null)
                {
                    skipped++;
                    continue;
                }

                var key = ScopeDomainKey(program.Handle, parsed.NormalizedHost);
                var summary = BuildBountySummary(program, scope);

                if (!domainByKey.TryGetValue(key, out var existing))
                {
                    existing = new DomainAsset
                    {
                        CompanyId = project.CompanyId,
                        SecurityProjectId = project.Id,
                        HostName = parsed.DisplayHost,
                        NormalizedHostName = parsed.NormalizedHost,
                        Scheme = "https",
                        Source = "HackerOne",
                        Status = DomainAssetStatus.Verified,
                        IsVerified = true,
                        VerifiedAt = now,
                        VerificationMethod = VerificationMethod.Mock,
                        Notes = $"HackerOne in-scope · {program.Name}",
                        HackerOneProgramHandle = program.Handle,
                        HackerOneProgramName = program.Name,
                        HackerOneScopeId = scope.ExternalId,
                        HackerOneAssetType = scope.AssetType,
                        HackerOneEligibleForBounty = scope.EligibleForBounty,
                        HackerOneEligibleForSubmission = scope.EligibleForSubmission,
                        HackerOneMaxSeverity = scope.MaxSeverity,
                        HackerOneOffersBounties = program.OffersBounties,
                        HackerOneCurrency = program.Currency,
                        HackerOneSubmissionState = program.SubmissionState,
                        HackerOneIsWildcard = parsed.IsWildcard,
                        HackerOneBountySummary = summary,
                        HackerOneLastSyncedAt = now,
                        CreatedAt = now
                    };
                    _db.DomainAssets.Add(existing);
                    domainByKey[key] = existing;
                    upserted++;
                }
                else
                {
                    // Aynı host tekrar gelirse: bounty-eligible / daha yüksek severity tercih et.
                    ApplyScopeToDomain(existing, program, scope, parsed, summary, now, preferIncoming: PreferIncomingScope(existing, scope));
                    upserted++;
                }
            }

            if (programsProcessed % 10 == 0)
            {
                var saved = await SaveScopeSyncBatchAsync(project.Id, domainByKey, cancellationToken);
                if (!saved)
                {
                    _logger.LogWarning(
                        "HackerOne scope sync batch recovered after conflict at program {Processed}/{Total}",
                        programsProcessed,
                        programs.Count);
                }

                _logger.LogInformation(
                    "HackerOne scope sync progress: {Processed}/{Total} programs, {Upserted} domains",
                    programsProcessed,
                    programs.Count,
                    upserted);
            }
        }

        await SaveScopeSyncBatchAsync(project.Id, domainByKey, cancellationToken);
        var message =
            $"{programsProcessed} program, {scopesSeen} scope işlendi; {upserted} domain güncellendi, {skipped} atlandı. " +
            "HackerOne API genelde kesin $ tutarı vermez; özet eligible/max severity/currency bilgisidir.";
        await _audit.WriteAsync(
            "scopes.synced",
            "DomainAsset",
            project.Id.ToString(),
            new { programsProcessed, scopesSeen, upserted, skipped },
            cancellationToken);

        return Result<HackerOneScopeSyncResultDto>.Success(new HackerOneScopeSyncResultDto(
            programsProcessed,
            scopesSeen,
            upserted,
            skipped,
            message));
    }

    private static string ScopeDomainKey(string programHandle, string normalizedHost) =>
        $"{programHandle.Trim().ToLowerInvariant()}|{normalizedHost.Trim().ToLowerInvariant()}";

    private static bool PreferIncomingScope(DomainAsset existing, HackerOneRemoteScope incoming)
    {
        if (existing.HackerOneEligibleForBounty != true && incoming.EligibleForBounty)
        {
            return true;
        }

        return SeverityRank(incoming.MaxSeverity) > SeverityRank(existing.HackerOneMaxSeverity);
    }

    private static int SeverityRank(string? severity) =>
        (severity ?? "").Trim().ToLowerInvariant() switch
        {
            "critical" => 4,
            "high" => 3,
            "medium" => 2,
            "low" => 1,
            "none" => 0,
            _ => -1
        };

    private static void ApplyScopeToDomain(
        DomainAsset existing,
        BugBountyProgram program,
        HackerOneRemoteScope scope,
        ParsedWebAsset parsed,
        string summary,
        DateTime now,
        bool preferIncoming)
    {
        if (preferIncoming || string.IsNullOrWhiteSpace(existing.HackerOneScopeId))
        {
            existing.HostName = parsed.DisplayHost;
            existing.HackerOneScopeId = scope.ExternalId;
            existing.HackerOneAssetType = scope.AssetType;
            existing.HackerOneEligibleForBounty = scope.EligibleForBounty;
            existing.HackerOneEligibleForSubmission = scope.EligibleForSubmission;
            existing.HackerOneMaxSeverity = scope.MaxSeverity;
            existing.HackerOneIsWildcard = parsed.IsWildcard;
            existing.HackerOneBountySummary = summary;
        }

        existing.Source = "HackerOne";
        existing.HackerOneProgramHandle = program.Handle;
        existing.HackerOneProgramName = program.Name;
        existing.HackerOneOffersBounties = program.OffersBounties;
        existing.HackerOneCurrency = program.Currency;
        existing.HackerOneSubmissionState = program.SubmissionState;
        existing.HackerOneLastSyncedAt = now;
        existing.UpdatedAt = now;
        if (!existing.IsVerified)
        {
            existing.IsVerified = true;
            existing.Status = DomainAssetStatus.Verified;
            existing.VerifiedAt = now;
            existing.VerificationMethod ??= VerificationMethod.Mock;
        }
    }

    private async Task ReloadHackerOneDomainCacheAsync(
        Guid projectId,
        Dictionary<string, DomainAsset> domainByKey,
        CancellationToken cancellationToken)
    {
        DetachTrackedDomainAssets();
        domainByKey.Clear();

        var existingDomains = await _db.DomainAssets
            .IgnoreQueryFilters()
            .Where(d => d.SecurityProjectId == projectId && d.Source == "HackerOne")
            .ToListAsync(cancellationToken);

        foreach (var d in existingDomains)
        {
            if (string.IsNullOrWhiteSpace(d.HackerOneProgramHandle))
            {
                continue;
            }

            domainByKey[ScopeDomainKey(d.HackerOneProgramHandle, d.NormalizedHostName)] = d;
        }
    }

    private void DetachTrackedDomainAssets()
    {
        if (_db is not DbContext concrete)
        {
            return;
        }

        foreach (var entry in concrete.ChangeTracker.Entries<DomainAsset>().ToList())
        {
            entry.State = EntityState.Detached;
        }
    }

    /// <returns>false if unique conflict recovered (cache reloaded).</returns>
    private async Task<bool> SaveScopeSyncBatchAsync(
        Guid projectId,
        Dictionary<string, DomainAsset> domainByKey,
        CancellationToken cancellationToken)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex)
        {
            // Duplicate key: tenant filter / önceki kısmi sync / eşzamanlı job.
            // Exception fırlatma — VS debugger + Hangfire'ı düşürmesin; cache yenile, devam.
            _logger.LogWarning(ex, "HackerOne scope sync SaveChanges conflict — cache yenileniyor, devam");
            await ReloadHackerOneDomainCacheAsync(projectId, domainByKey, cancellationToken);
            return false;
        }
    }

    public async Task<Result<TargetAssessmentSummaryDto>> GetLatestAssessmentSummaryAsync(
        Guid domainAssetId,
        CancellationToken cancellationToken = default)
    {
        var domain = await _db.DomainAssets.AsNoTracking()
            .Where(d => d.Id == domainAssetId)
            .Select(d => new { d.Id, d.HostName })
            .FirstOrDefaultAsync(cancellationToken);
        if (domain is null)
        {
            return Result<TargetAssessmentSummaryDto>.Failure("not_found", "Hedef bulunamadı.");
        }

        var job = await _db.ScanJobs.AsNoTracking()
            .Include(j => j.Result!)
            .ThenInclude(r => r.Findings)
            .Where(j => j.DomainAssetId == domainAssetId
                        && j.AssessmentMode == AssessmentMode.ApplicationSecurityCandidate)
            .OrderByDescending(j => j.CompletedAt ?? j.StartedAt ?? j.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (job is null)
        {
            return Result<TargetAssessmentSummaryDto>.Failure(
                "no_assessment",
                "Bu hedef için henüz ASC (Candidate Assessment) kaydı yok.");
        }

        var findings = (job.Result?.Findings ?? Array.Empty<Finding>())
            .OrderByDescending(f => f.Severity)
            .ThenBy(f => f.Title)
            .Select(f => new TargetAssessmentFindingDto(
                f.Id,
                f.Title,
                f.Severity.ToString(),
                f.FindingClass.ToString(),
                f.SubmissionRecommendation.ToString(),
                f.AffectedUrl,
                f.CheckCode,
                f.Fingerprint,
                f.Category))
            .ToList();

        var engines = ParseEnginesFromSummary(job.Result?.Summary)
            .DefaultIfEmpty(job.CurrentStep ?? "asc")
            .Where(e => !string.IsNullOrWhiteSpace(e) && e != "Tamamlandı")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Result<TargetAssessmentSummaryDto>.Success(new TargetAssessmentSummaryDto(
            domain.Id,
            domain.HostName,
            job.Id,
            job.Result?.Id,
            job.Status.ToString(),
            job.CompletedAt,
            job.Result?.SecurityScore ?? 0,
            job.Result?.Summary,
            job.Result?.ExecutiveSummary,
            job.Result?.ChecksTotal ?? job.TotalSteps,
            job.Result?.ChecksPassed ?? job.CompletedSteps,
            job.Result?.ChecksFailed ?? 0,
            job.Result?.CriticalCount ?? 0,
            job.Result?.HighCount ?? 0,
            job.Result?.MediumCount ?? 0,
            job.Result?.LowCount ?? 0,
            job.Result?.InfoCount ?? 0,
            engines,
            findings));
    }

    private static IReadOnlyList<string> ParseEnginesFromSummary(string? summary)
    {
        if (string.IsNullOrWhiteSpace(summary))
        {
            return Array.Empty<string>();
        }

        const string marker = "Çalışan motorlar:";
        var idx = summary.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
        {
            return Array.Empty<string>();
        }

        var slice = summary[(idx + marker.Length)..];
        var end = slice.IndexOf('.');
        if (end >= 0)
        {
            slice = slice[..end];
        }

        return slice
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => !string.Equals(s, "(none)", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<Result<Guid>> AddManualTargetAsync(
        string hostName,
        bool authorizedConfirmed,
        CancellationToken cancellationToken = default)
    {
        if (!authorizedConfirmed)
        {
            return Result<Guid>.Failure(
                "authorization_required",
                "Bu hedefi test etmeye yetkili olduğunuzu onaylamalısınız.");
        }

        if (string.IsNullOrWhiteSpace(hostName))
        {
            return Result<Guid>.Failure("host_required", "Alan adı zorunlu.");
        }

        var parsed = TryParseWebAsset(hostName, "DOMAIN");
        if (parsed is null)
        {
            return Result<Guid>.Failure("invalid_host", "Geçerli bir alan adı girin (örn. example.com).");
        }

        if (parsed.IsWildcard)
        {
            return Result<Guid>.Failure(
                "wildcard_not_allowed",
                "Wildcard (*.) hedef doğrudan taranamaz; tam alan adı girin.");
        }

        var project = await EnsureHackerOneTargetsProjectAsync(cancellationToken);
        if (project is null)
        {
            return Result<Guid>.Failure("no_project", "Hedef projesi bulunamadı.");
        }

        var existing = await _db.DomainAssets.IgnoreQueryFilters().FirstOrDefaultAsync(
            d => d.SecurityProjectId == project.Id && d.NormalizedHostName == parsed.NormalizedHost,
            cancellationToken);
        if (existing is not null)
        {
            if (!existing.IsVerified)
            {
                existing.IsVerified = true;
                existing.Status = DomainAssetStatus.Verified;
                existing.VerifiedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
            }

            return Result<Guid>.Success(existing.Id);
        }

        var now = DateTime.UtcNow;
        var entity = new DomainAsset
        {
            CompanyId = project.CompanyId,
            SecurityProjectId = project.Id,
            HostName = parsed.DisplayHost,
            NormalizedHostName = parsed.NormalizedHost,
            Scheme = "https",
            Source = "Manual",
            Status = DomainAssetStatus.Verified,
            IsVerified = true,
            VerifiedAt = now,
            VerificationMethod = VerificationMethod.Mock,
            HackerOneProgramHandle = "manual",
            HackerOneProgramName = "Manuel Hedef",
            HackerOneEligibleForBounty = true,
            HackerOneOffersBounties = false,
            HackerOneIsWildcard = false,
            Notes = "Kullanıcı tarafından manuel eklendi (yetkili test beyanı).",
            CreatedByUserId = _currentUser.UserId,
            CreatedAt = now
        };
        _db.DomainAssets.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync("hackerone.manual_target.added", "DomainAsset", entity.Id.ToString(),
            new { entity.HostName }, cancellationToken);
        return Result<Guid>.Success(entity.Id);
    }

    private async Task<SecurityProject?> EnsureHackerOneTargetsProjectAsync(CancellationToken cancellationToken)
    {
        var company = await _db.Companies
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Name == DatabaseSeeder.DemoCompanyName, cancellationToken);
        if (company is null)
        {
            company = await _db.Companies.IgnoreQueryFilters()
                .OrderBy(c => c.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (company is null)
        {
            return null;
        }

        var project = await _db.SecurityProjects.IgnoreQueryFilters().FirstOrDefaultAsync(
            p => p.CompanyId == company.Id && p.Name == HackerOneTargetsProjectName,
            cancellationToken);
        if (project is not null)
        {
            return project;
        }

        project = new SecurityProject
        {
            CompanyId = company.Id,
            Name = HackerOneTargetsProjectName,
            Description = "HackerOne structured_scopes ile senkronize edilen in-scope web hedefleri.",
            EnvironmentType = EnvironmentType.Production,
            Status = ProjectStatus.Active,
            PrimaryContactEmail = company.ContactEmail,
            CreatedAt = DateTime.UtcNow
        };
        _db.SecurityProjects.Add(project);
        await _db.SaveChangesAsync(cancellationToken);
        return project;
    }

    private static bool IsWebScope(string assetType, string identifier)
    {
        if (WebAssetTypes.Contains(assetType))
        {
            // OTHER only if it looks like a host/URL
            if (assetType.Equals("OTHER", StringComparison.OrdinalIgnoreCase))
            {
                return identifier.Contains('.') && !identifier.Contains(' ');
            }

            return true;
        }

        return false;
    }

    private sealed record ParsedWebAsset(string DisplayHost, string NormalizedHost, bool IsWildcard);

    private static ParsedWebAsset? TryParseWebAsset(string identifier, string assetType)
    {
        var raw = identifier.Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var isWildcard = assetType.Equals("WILDCARD", StringComparison.OrdinalIgnoreCase)
                         || raw.StartsWith("*.", StringComparison.Ordinal);

        try
        {
            if (raw.Contains("://", StringComparison.Ordinal))
            {
                var uri = new Uri(raw);
                var host = uri.IdnHost.Trim().TrimEnd('.').ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(host))
                {
                    return null;
                }

                return new ParsedWebAsset(host, host, isWildcard || host.StartsWith("*.", StringComparison.Ordinal));
            }
        }
        catch (UriFormatException)
        {
            // fall through to host parsing
        }

        var hostPart = raw
            .Replace("*.", "", StringComparison.Ordinal)
            .Split('/', StringSplitOptions.RemoveEmptyEntries)[0]
            .Split('?', StringSplitOptions.RemoveEmptyEntries)[0]
            .Trim()
            .TrimEnd('.')
            .ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(hostPart) || hostPart.Contains(' ') || !hostPart.Contains('.'))
        {
            return null;
        }

        // Reject obvious non-host assets (emails, CIDR-ish)
        if (hostPart.Contains('@') || hostPart.Contains(':') && !hostPart.StartsWith('['))
        {
            // allow IPv6 bracket hosts later; skip plain "host:port" for now by stripping port
            var colon = hostPart.IndexOf(':');
            if (colon > 0 && int.TryParse(hostPart[(colon + 1)..], out _))
            {
                hostPart = hostPart[..colon];
            }
            else if (hostPart.Contains('@'))
            {
                return null;
            }
        }

        var display = isWildcard ? $"*.{hostPart.TrimStart('*').TrimStart('.')}" : hostPart;
        var normalized = display.ToLowerInvariant();
        return new ParsedWebAsset(display, normalized, isWildcard);
    }

    private static string BuildBountySummary(BugBountyProgram program, HackerOneRemoteScope scope)
    {
        var parts = new List<string>();
        if (program.OffersBounties)
        {
            parts.Add(string.IsNullOrWhiteSpace(program.Currency)
                ? "Program bounty ödüyor"
                : $"Program bounty ödüyor ({program.Currency})");
        }
        else
        {
            parts.Add("Bounty yok (VDP/reputation)");
        }

        parts.Add(scope.EligibleForBounty ? "Bu varlık bounty-eligible" : "Bu varlık bounty-eligible değil");

        if (!string.IsNullOrWhiteSpace(scope.MaxSeverity))
        {
            parts.Add($"max severity: {scope.MaxSeverity}");
        }

        if (!string.IsNullOrWhiteSpace(program.SubmissionState))
        {
            parts.Add($"submission: {program.SubmissionState}");
        }

        parts.Add("kesin $ tutarı HackerOne API'de yok — program sayfasına bakın");
        return string.Join(" · ", parts);
    }

    public async Task<Result<HackerOneReportDraftDto>> CreateOrGetDraftAsync(
        CreateHackerOneDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        var finding = await _db.Findings
            .Include(f => f.ScanResult!).ThenInclude(r => r.ScanJob!).ThenInclude(j => j.DomainAsset)
            .FirstOrDefaultAsync(f => f.Id == request.FindingId, cancellationToken);
        if (finding is null)
        {
            return Result<HackerOneReportDraftDto>.Failure("finding_not_found", "Bulgu bulunamadı.");
        }

        var existing = await _db.HackerOneReportDrafts
            .Include(d => d.Program)
            .FirstOrDefaultAsync(d => d.FindingId == request.FindingId
                                      && d.Status != HackerOneReportDraftStatus.Archived, cancellationToken);
        if (existing is not null)
        {
            return Result<HackerOneReportDraftDto>.Success(MapDraft(existing));
        }

        if (!IsEligibleForHackerOneDraft(finding))
        {
            return Result<HackerOneReportDraftDto>.Failure(
                "draft_not_applicable",
                "Anlamlı güvenlik sinyali yok veya DoNotSubmit — HackerOne taslağı üretilmez. Bulgu Security Assessment'ta Informational olarak kalır.");
        }

        var program = await ResolveProgramAsync(request.BugBountyProgramId, cancellationToken);
        if (program is null)
        {
            return Result<HackerOneReportDraftDto>.Failure("program_not_found", "Varsayılan Bug Bounty programı bulunamadı.");
        }

        var settings = await EnsureSettingsAsync(cancellationToken);
        var enFields = BuildEnglishDraftFieldsFromFinding(finding);
        var trFields = BuildTurkishDraftFieldsFromFinding(finding);
        var markdown = _markdown.Build(enFields);
        var trMarkdown = _markdown.BuildTurkish(trFields);
        var score = _markdown.ComputeReadinessScore(enFields);

        var draft = new HackerOneReportDraft
        {
            FindingId = finding.Id,
            BugBountyProgramId = program.Id,
            Title = enFields.Title,
            Severity = enFields.Severity,
            Asset = enFields.Asset,
            Weakness = enFields.Weakness,
            Impact = enFields.Impact,
            StepsToReproduce = enFields.StepsToReproduce,
            ProofOfConcept = enFields.ProofOfConcept,
            Notes = enFields.Notes,
            MarkdownBody = markdown,
            TurkishMarkdownBody = trMarkdown,
            ReportReadinessScore = score,
            Status = score >= settings.MinReadinessScoreForSubmit
                ? HackerOneReportDraftStatus.Ready
                : HackerOneReportDraftStatus.Draft,
            CreatedByUserId = _currentUser.UserId,
            Program = program
        };

        _db.HackerOneReportDrafts.Add(draft);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync("draft.created", "HackerOneReportDraft", draft.Id.ToString(), new { finding.Id }, cancellationToken);
        return Result<HackerOneReportDraftDto>.Success(MapDraft(draft));
    }

    public async Task<Result<HackerOneReportDraftDto>> GetDraftAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var draft = await _db.HackerOneReportDrafts.Include(d => d.Program)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        return draft is null
            ? Result<HackerOneReportDraftDto>.Failure("draft_not_found", "Taslak bulunamadı.")
            : Result<HackerOneReportDraftDto>.Success(MapDraft(draft));
    }

    public async Task<IReadOnlyList<HackerOneReportDraftDto>> ListDraftsAsync(CancellationToken cancellationToken = default)
    {
        var drafts = await _db.HackerOneReportDrafts.Include(d => d.Program)
            .OrderByDescending(d => d.UpdatedAt ?? d.CreatedAt)
            .ToListAsync(cancellationToken);
        return drafts.Select(MapDraft).ToList();
    }

    public async Task<Result<HackerOneReportDraftDto>> UpdateDraftAsync(
        Guid id,
        UpdateHackerOneDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        var draft = await _db.HackerOneReportDrafts
            .Include(d => d.Program)
            .Include(d => d.Finding)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (draft is null)
        {
            return Result<HackerOneReportDraftDto>.Failure("draft_not_found", "Taslak bulunamadı.");
        }

        if (request.Title is not null) draft.Title = request.Title;
        if (request.Severity is not null) draft.Severity = request.Severity;
        if (request.Asset is not null) draft.Asset = request.Asset;
        if (request.Weakness is not null) draft.Weakness = request.Weakness;
        if (request.Impact is not null) draft.Impact = request.Impact;
        if (request.StepsToReproduce is not null) draft.StepsToReproduce = request.StepsToReproduce;
        if (request.ProofOfConcept is not null) draft.ProofOfConcept = request.ProofOfConcept;
        if (request.Notes is not null) draft.Notes = request.Notes;

        var settings = await EnsureSettingsAsync(cancellationToken);
        RefreshDraftMarkdown(draft, settings.MinReadinessScoreForSubmit);
        draft.UpdatedByUserId = _currentUser.UserId;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync("draft.updated", "HackerOneReportDraft", id.ToString(), null, cancellationToken);
        return Result<HackerOneReportDraftDto>.Success(MapDraft(draft));
    }

    public async Task<Result<HackerOneMarkdownDto>> GetMarkdownAsync(
        Guid id,
        string? language = null,
        CancellationToken cancellationToken = default)
    {
        var draft = await _db.HackerOneReportDrafts
            .Include(d => d.Finding!).ThenInclude(f => f.ScanResult!).ThenInclude(r => r.ScanJob!).ThenInclude(j => j.DomainAsset)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (draft is null)
        {
            return Result<HackerOneMarkdownDto>.Failure("draft_not_found", "Taslak bulunamadı.");
        }

        var settings = await EnsureSettingsAsync(cancellationToken);
        RefreshDraftMarkdown(draft, settings.MinReadinessScoreForSubmit);
        await _db.SaveChangesAsync(cancellationToken);

        var lang = (language ?? "en").Trim().ToLowerInvariant();
        var isTr = lang is "tr" or "tr-tr" or "turkish";
        var body = isTr
            ? (draft.TurkishMarkdownBody ?? "")
            : (draft.MarkdownBody ?? "");

        return Result<HackerOneMarkdownDto>.Success(new HackerOneMarkdownDto(
            draft.Id,
            body,
            draft.ReportReadinessScore,
            isTr ? "tr-TR" : HackerOneReportLanguage.Code,
            draft.TurkishMarkdownBody));
    }

    public async Task<Result<HackerOneMarkdownDto>> RecalculateReadinessAsync(Guid id, CancellationToken cancellationToken = default)
        => await GetMarkdownAsync(id, "en", cancellationToken);

    public async Task<IReadOnlyList<HackerOneSubmissionDto>> ListSubmissionsAsync(CancellationToken cancellationToken = default)
    {
        return await _db.HackerOneSubmissionRecords
            .OrderByDescending(s => s.SubmittedAt ?? s.CreatedAt)
            .Select(s => new HackerOneSubmissionDto(
                s.Id,
                s.HackerOneReportDraftId,
                s.ExternalReportId,
                s.ExternalReportUrl,
                s.Status,
                s.ErrorMessage,
                s.SubmittedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<Result<HackerOneSubmissionDto>> SubmitDraftAsync(
        Guid draftId,
        SubmitHackerOneDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        await _audit.WriteAsync("draft.submit_attempt", "HackerOneReportDraft", draftId.ToString(),
            new { request.ExplicitConfirm, apiEnabled = _options.ApiEnabled }, cancellationToken);

        if (!request.ExplicitConfirm)
        {
            return Result<HackerOneSubmissionDto>.Failure(
                "explicit_confirm_required",
                "API gönderimi için açık onay (ExplicitConfirm=true) zorunludur.");
        }

        if (!_options.ApiEnabled)
        {
            return Result<HackerOneSubmissionDto>.Failure(
                "hackerone_api_disabled",
                "HackerOne API kapalı. Copy Full Report / Open HackerOne kullanın.");
        }

        var draft = await _db.HackerOneReportDrafts
            .Include(d => d.Program)
            .Include(d => d.Finding)
            .FirstOrDefaultAsync(d => d.Id == draftId, cancellationToken);
        if (draft is null)
        {
            return Result<HackerOneSubmissionDto>.Failure("draft_not_found", "Taslak bulunamadı.");
        }

        var settings = await EnsureSettingsAsync(cancellationToken);
        RefreshDraftMarkdown(draft, settings.MinReadinessScoreForSubmit);

        if (draft.ReportReadinessScore < settings.MinReadinessScoreForSubmit)
        {
            return Result<HackerOneSubmissionDto>.Failure(
                "readiness_too_low",
                $"ReportReadinessScore ({draft.ReportReadinessScore}) eşiğin altında ({settings.MinReadinessScoreForSubmit}).");
        }

        var finding = draft.Finding;
        if (finding is null)
        {
            return Result<HackerOneSubmissionDto>.Failure("finding_not_found", "Taslak bulgusu bulunamadı.");
        }

        if (finding.SubmissionRecommendation == SubmissionRecommendation.DoNotSubmit)
        {
            return Result<HackerOneSubmissionDto>.Failure(
                "do_not_submit",
                "Finding SubmissionRecommendation=DoNotSubmit; HackerOne'a gönderilemez.");
        }

        if (!finding.BugBountyEligible && finding.SubmissionRecommendation != SubmissionRecommendation.ManualReview)
        {
            return Result<HackerOneSubmissionDto>.Failure(
                "not_eligible",
                "Finding ne BugBountyEligible ne de ManualReview.");
        }

        // Hard gate: never submit unvalidated candidates to HackerOne.
        var confirmed = finding.FindingClass == FindingClass.Vulnerability && finding.DemonstratedImpact;
        if (!confirmed || !finding.DemonstratedImpact)
        {
            return Result<HackerOneSubmissionDto>.Failure(
                "impact_not_demonstrated",
                "HackerOne gönderimi engellendi: ConfirmedVulnerability=false veya DemonstratedImpact=false. " +
                "Önce ayrıcalıklı erişim / XSS etkisini kanıtlayın; Copy EN ile manuel inceleyin.");
        }

        if (finding.FindingClass == FindingClass.VulnerabilityCandidate
            || finding.BugBountySeverity == BugBountySeverity.Unassigned)
        {
            return Result<HackerOneSubmissionDto>.Failure(
                "candidate_not_confirmed",
                "VulnerabilityCandidate / Unassigned severity HackerOne API ile gönderilemez.");
        }

        var submit = await _apiClient.SubmitReportAsync(new HackerOneSubmitPayload(
            draft.Program?.Handle ?? "amazonvrp",
            draft.Title,
            draft.Severity,
            draft.MarkdownBody ?? ""), cancellationToken);

        var record = new HackerOneSubmissionRecord
        {
            HackerOneReportDraftId = draft.Id,
            CreatedByUserId = _currentUser.UserId,
            SubmittedAt = DateTime.UtcNow
        };

        if (submit.IsFailure)
        {
            record.Status = HackerOneSubmissionStatus.Failed;
            record.ErrorMessage = submit.ErrorMessage;
            _db.HackerOneSubmissionRecords.Add(record);
            await _db.SaveChangesAsync(cancellationToken);
            await _audit.WriteAsync("draft.submit_failed", "HackerOneSubmissionRecord", record.Id.ToString(),
                new { submit.ErrorCode }, cancellationToken);
            return Result<HackerOneSubmissionDto>.Failure(submit.ErrorCode!, submit.ErrorMessage!);
        }

        record.Status = HackerOneSubmissionStatus.Submitted;
        record.ExternalReportId = submit.Value!.ExternalReportId;
        record.ExternalReportUrl = submit.Value.ExternalReportUrl;
        draft.Status = HackerOneReportDraftStatus.Submitted;
        _db.HackerOneSubmissionRecords.Add(record);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync("draft.submitted", "HackerOneSubmissionRecord", record.Id.ToString(),
            new { record.ExternalReportId }, cancellationToken);

        return Result<HackerOneSubmissionDto>.Success(new HackerOneSubmissionDto(
            record.Id, draft.Id, record.ExternalReportId, record.ExternalReportUrl,
            record.Status, record.ErrorMessage, record.SubmittedAt));
    }

    public async Task<IReadOnlyList<ScanProfileDto>> ListScanProfilesAsync(CancellationToken cancellationToken = default)
    {
        var profiles = await _db.ScanProfiles.OrderBy(p => p.ProfileKey).ToListAsync(cancellationToken);
        return profiles.Select(p => new ScanProfileDto(
            p.Id,
            p.ProfileKey,
            p.DisplayName,
            p.UserAgentConfigKey,
            p.RateLimitPerMinuteConfigKey,
            p.IsEnabled,
            _options.AmazonVrp.UserAgent,
            _options.AmazonVrp.RateLimitPerMinute)).ToList();
    }

    public async Task<Result<Guid>> StartCandidateAssessmentAsync(
        StartCandidateAssessmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var resolve = await ResolveDomainAssetIdAsync(request, cancellationToken);
        if (resolve.IsFailure)
        {
            return Result<Guid>.Failure(resolve.ErrorCode!, resolve.ErrorMessage!);
        }

        var domainAssetId = resolve.Value!;
        var start = await _scanService.StartAsync(new StartScanRequest(
            domainAssetId,
            ScanType.FullPassive,
            AssessmentMode.ApplicationSecurityCandidate,
            AssessmentModeNames.ApplicationSecurityCandidate), cancellationToken);

        if (start.IsFailure)
        {
            return Result<Guid>.Failure(start.ErrorCode!, start.ErrorMessage!);
        }

        await _audit.WriteAsync("candidate_assessment.started", "ScanJob", start.Value!.ScanJobId.ToString(),
            new { domainAssetId, request.HostName }, cancellationToken);
        return Result<Guid>.Success(start.Value.ScanJobId);
    }

    private async Task<Result<Guid>> ResolveDomainAssetIdAsync(
        StartCandidateAssessmentRequest request,
        CancellationToken cancellationToken)
    {
        if (request.DomainAssetId is Guid id && id != Guid.Empty)
        {
            var exists = await _db.DomainAssets.AnyAsync(d => d.Id == id, cancellationToken);
            return exists
                ? Result<Guid>.Success(id)
                : Result<Guid>.Failure("domain_not_found", "Domain bulunamadı.");
        }

        if (string.IsNullOrWhiteSpace(request.HostName))
        {
            return Result<Guid>.Failure(
                "domain_required",
                "Domain adı veya DomainAssetId gerekli (örn. amazon.com).");
        }

        var host = NormalizeHostInput(request.HostName);
        var wwwHost = host.StartsWith("www.", StringComparison.Ordinal) ? host : "www." + host;
        var bareHost = host.StartsWith("www.", StringComparison.Ordinal) ? host[4..] : host;
        var domain = await _db.DomainAssets
            .Where(d =>
                d.NormalizedHostName == host
                || d.NormalizedHostName == wwwHost
                || d.NormalizedHostName == bareHost
                || d.HostName == host
                || d.HostName == request.HostName.Trim())
            .OrderByDescending(d => d.IsVerified)
            .ThenByDescending(d => d.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (domain is null)
        {
            return Result<Guid>.Failure(
                "domain_not_found",
                $"'{request.HostName}' platformda kayıtlı değil. Önce Domainler / Site Test ile ekleyip doğrulayın.");
        }

        if (!domain.IsVerified)
        {
            return Result<Guid>.Failure(
                "domain_not_verified",
                $"'{domain.HostName}' doğrulanmamış. Application Security Candidate için önce doğrulayın.");
        }

        return Result<Guid>.Success(domain.Id);
    }

    private static string NormalizeHostInput(string raw)
    {
        var host = raw.Trim().ToLowerInvariant();
        if (host.StartsWith("https://", StringComparison.Ordinal))
        {
            host = host["https://".Length..];
        }
        else if (host.StartsWith("http://", StringComparison.Ordinal))
        {
            host = host["http://".Length..];
        }

        var slash = host.IndexOf('/');
        if (slash >= 0)
        {
            host = host[..slash];
        }

        if (host.StartsWith("www.", StringComparison.Ordinal))
        {
            host = host[4..];
        }

        return host.TrimEnd('.');
    }

    private void RefreshDraftMarkdown(HackerOneReportDraft draft, int minScore)
    {
        // EN = HackerOne export; TR = internal review only.
        if (draft.Finding is Finding finding)
        {
            var en = BuildEnglishDraftFieldsFromFinding(finding);
            var tr = BuildTurkishDraftFieldsFromFinding(finding);
            draft.Title = en.Title;
            draft.Severity = en.Severity;
            draft.Asset = en.Asset;
            draft.Weakness = en.Weakness;
            draft.Impact = en.Impact;
            draft.StepsToReproduce = en.StepsToReproduce;
            draft.ProofOfConcept = en.ProofOfConcept;
            draft.Notes = en.Notes;
            draft.MarkdownBody = _markdown.Build(en);
            draft.TurkishMarkdownBody = _markdown.BuildTurkish(tr);
            draft.ReportReadinessScore = _markdown.ComputeReadinessScore(en);
        }
        else
        {
            var fields = new HackerOneReportDraftFields(
                draft.Title,
                draft.Severity,
                draft.Asset,
                draft.Weakness,
                draft.Impact,
                draft.StepsToReproduce,
                draft.ProofOfConcept,
                draft.Notes,
                ConfirmedVulnerability: false,
                DemonstratedImpact: false,
                BugBountySeverityLabel: draft.Severity,
                Language: HackerOneReportLanguage.Code);
            draft.MarkdownBody = _markdown.Build(fields);
            draft.ReportReadinessScore = _markdown.ComputeReadinessScore(fields);
        }

        if (draft.Status is not (HackerOneReportDraftStatus.Submitted or HackerOneReportDraftStatus.Archived))
        {
            draft.Status = draft.ReportReadinessScore >= minScore
                ? HackerOneReportDraftStatus.Ready
                : HackerOneReportDraftStatus.Draft;
        }
    }

    private HackerOneReportDraftFields BuildEnglishDraftFieldsFromFinding(Finding finding)
    {
        if (HackerOneReportLanguage.IsXssCandidate(finding.Fingerprint, finding.PolicyCategory))
        {
            return BuildXssEnglishFields(finding);
        }

        if (HackerOneReportLanguage.IsAccessControlCandidate(finding.Fingerprint, finding.PolicyCategory))
        {
            return BuildAccessControlEnglishFields(finding);
        }

        return BuildGenericEnglishFields(finding);
    }

    private HackerOneReportDraftFields BuildXssEnglishFields(Finding finding)
    {
        var host = finding.ScanResult?.ScanJob?.DomainAsset?.HostName ?? "unknown-asset";
        var safeTarget = _markdown.FormatSafeUrlForSteps(
            string.IsNullOrWhiteSpace(finding.AffectedUrl) ? host : finding.AffectedUrl);
        var param = finding.AffectedParameter ?? "q";
        var findingType = "XSS Candidate";
        var candidateSeverity = HackerOneReportLanguage.FormatCandidateSeverity(
            finding.TechnicalPotentialSeverity, finding.BugBountySeverity);
        const string bbSeverity = "Unassigned";
        var weakness = string.IsNullOrWhiteSpace(finding.CweCode)
            ? "Potential Weakness: CWE-79"
            : $"Potential Weakness: {finding.CweCode}";
        var submission = HackerOneReportLanguage.FormatSubmissionRecommendation(finding.SubmissionRecommendation);
        var exploitability = HackerOneReportLanguage.FormatExploitability(
            finding.Exploitability, finding.RequiresManualValidation);
        var eligibility = BuildXssEnglishEligibility(finding);
        var notes = eligibility;

        return new HackerOneReportDraftFields(
            Title: "Reflected Input / XSS Candidate",
            Severity: bbSeverity,
            Asset: host,
            Weakness: weakness,
            Impact:
                "A unique harmless marker supplied through a query parameter was reflected in the HTTP response body.\n\n" +
                "This confirms input reflection only. No executable JavaScript or browser-side code execution has been demonstrated.\n\n" +
                "Manual review of the output context and encoding behavior is required before this finding can be classified as XSS.",
            StepsToReproduce:
                $"1. Open the target asset: {safeTarget}\n" +
                $"2. Submit a harmless unique marker via the `{param}` query parameter (no executable JavaScript payload).\n" +
                "3. Inspect the HTTP response for reflection of the marker and encoding behavior.\n" +
                "4. Manually validate context before classifying as XSS.",
            ProofOfConcept: BuildXssEnglishProofOfConcept(finding),
            Notes: notes,
            ConfirmedVulnerability: false,
            DemonstratedImpact: false,
            BugBountySeverityLabel: bbSeverity,
            Language: HackerOneReportLanguage.Code,
            FindingType: findingType,
            CandidateSeverity: candidateSeverity,
            ExploitabilityLabel: exploitability,
            SubmissionRecommendationLabel: submission,
            Summary:
                "A unique harmless marker supplied through a query parameter was reflected in the HTTP response. " +
                "This is an XSS candidate only; it is not a confirmed vulnerability.",
            VulnerabilityInformation:
                $"Finding Type: {findingType}\nFinding Class: VulnerabilityCandidate\n" +
                $"Candidate Severity: {candidateSeverity}\nConfirmed Vulnerability: No\n{weakness}\n" +
                $"Exploitability: {exploitability}\nDemonstrated Impact: No\n" +
                $"Bug bounty severity: {bbSeverity}\nSubmission Recommendation: {submission}\n" +
                $"ReflectionContext: {finding.ReflectionContext?.ToString() ?? "Unknown"}\n" +
                $"ReflectionCount: {finding.ReflectionCount?.ToString() ?? "n/a"}\n" +
                $"HtmlEncoded: {finding.HtmlEncoded?.ToString() ?? "n/a"}\n" +
                $"AttributeEncoded: {finding.AttributeEncoded?.ToString() ?? "n/a"}\n" +
                $"ContentType: {finding.ReflectionContentType ?? "n/a"}\n" +
                $"HttpStatus: {finding.ReflectionHttpStatus?.ToString() ?? "n/a"}\n" +
                $"InputSource: {finding.InputSource ?? "query:q"}",
            ExpectedResult:
                "Reflected input should be context-aware encoded so that special characters cannot break out of the output context.",
            ActualResult: finding.HtmlEncoded == true || finding.AttributeEncoded == true
                ? "The marker appears to be reflected with encoding applied (candidate for Do Not Submit)."
                : "The marker was reflected in the response. Encoding/context were not proven safe by automation.",
            SuggestedRemediation:
                "Apply context-aware output encoding and validate with a browser-based XSS proof only after manual review.",
            TestingNotes:
                "Platform testing used a harmless unique marker only. No executable JavaScript payload was automated. " +
                "HackerOne submission is blocked while ConfirmedVulnerability=false / DemonstratedImpact=false.",
            EligibilityReason: eligibility);
    }

    private static bool IsEligibleForHackerOneDraft(Finding finding)
    {
        if (finding.SubmissionRecommendation == SubmissionRecommendation.DoNotSubmit)
        {
            return false;
        }

        if (string.Equals(finding.Fingerprint, "asc.access.surface-donotsubmit", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (finding.FindingClass == FindingClass.Informational
            && finding.Fingerprint?.StartsWith("asc.access.", StringComparison.OrdinalIgnoreCase) == true)
        {
            return false;
        }

        var isAccess = HackerOneReportLanguage.IsAccessControlCandidate(finding.Fingerprint, finding.PolicyCategory)
                       || string.Equals(finding.Fingerprint, "asc.access.confirmed-unauthorized", StringComparison.OrdinalIgnoreCase);
        if (isAccess && !HasUsableSurfaceEvidence(finding.Evidence))
        {
            return false;
        }

        return finding.SubmissionRecommendation is SubmissionRecommendation.ManualReview or SubmissionRecommendation.Submit;
    }

    private HackerOneReportDraftFields BuildAccessControlEnglishFields(Finding finding)
    {
        var host = finding.ScanResult?.ScanJob?.DomainAsset?.HostName ?? "unknown-asset";
        var safeTarget = _markdown.FormatSafeUrlForSteps(
            string.IsNullOrWhiteSpace(finding.AffectedUrl) ? host : finding.AffectedUrl);
        var confirmed = string.Equals(finding.Fingerprint, "asc.access.confirmed-unauthorized", StringComparison.OrdinalIgnoreCase)
                        || (finding.FindingClass == FindingClass.Vulnerability && finding.DemonstratedImpact);
        var highPriority = string.Equals(finding.Fingerprint, "asc.access.surface-manualreview-high", StringComparison.OrdinalIgnoreCase);
        var findingType = confirmed ? "Broken Access Control" : "AccessControlCandidate";
        var findingClass = confirmed ? "Vulnerability" : "VulnerabilityCandidate";
        var bbSeverity = confirmed
            ? (finding.BugBountySeverity == BugBountySeverity.Unassigned ? "High" : finding.BugBountySeverity.ToString())
            : "Unassigned";
        var candidateSeverity = HackerOneReportLanguage.FormatCandidateSeverity(
            finding.TechnicalPotentialSeverity,
            confirmed ? finding.BugBountySeverity : BugBountySeverity.Unassigned);
        var weakness = string.IsNullOrWhiteSpace(finding.CweCode)
            ? "Potential Weakness: CWE-284"
            : $"Potential Weakness: {finding.CweCode}";
        var submission = HackerOneReportLanguage.FormatSubmissionRecommendation(
            confirmed ? SubmissionRecommendation.Submit : SubmissionRecommendation.ManualReview);
        var exploitability = confirmed ? "Demonstrated" : "Requires Manual Validation";
        var eligibility = confirmed
            ? "Confirmed Vulnerability: verified unauthorized privileged access demonstrated."
            : BuildAccessControlEnglishEligibility(finding);
        var surfaceEvidence = ExtractSurfaceEvidence(finding.Evidence);
        var reasons = ExtractManualReviewReasons(finding.Evidence);
        var steps = SensitiveSurfaceAnalyzer.BuildStepsFromSurfaceEvidence(finding.Evidence, safeTarget);
        if (confirmed)
        {
            steps +=
                "\n" +
                "Final: Unauthorized privileged access was verified with authorized validation evidence (see Proof of Concept / Validation Evidence).";
        }

        return new HackerOneReportDraftFields(
            Title: confirmed
                ? "Broken Access Control — verified unauthorized privileged access"
                : highPriority
                    ? "AccessControlCandidate [high priority] — unauthenticated sensitive data indicators"
                    : "AccessControlCandidate — unvalidated sensitive surface",
            Severity: bbSeverity,
            Asset: host,
            Weakness: weakness,
            Impact: confirmed
                ? "Verified unauthorized privileged access was demonstrated. Privileged data or functionality was reachable without proper authorization."
                : "Candidate only: meaningful unauthenticated surface signals were recorded, but " +
                  "no unauthorized access to privileged data or functionality has been demonstrated. " +
                  "Path existence alone is not a vulnerability.",
            StepsToReproduce: steps,
            ProofOfConcept: reasons.Count > 0
                ? "ManualReviewReasons:\n" + string.Join("\n", reasons.Select(r => $"- {r}"))
                : "See Surface Evidence for recorded analyzer observations.",
            Notes: eligibility,
            ConfirmedVulnerability: confirmed,
            DemonstratedImpact: confirmed,
            BugBountySeverityLabel: bbSeverity,
            Language: HackerOneReportLanguage.Code,
            FindingType: findingType,
            CandidateSeverity: candidateSeverity,
            ExploitabilityLabel: exploitability,
            SubmissionRecommendationLabel: submission,
            Summary: confirmed
                ? "Confirmed Broken Access Control. Demonstrated Impact: Yes."
                : highPriority
                    ? "High-priority AccessControlCandidate (sensitive data). Confirmed Vulnerability: No."
                    : "AccessControlCandidate for manual review. Confirmed Vulnerability: No.",
            VulnerabilityInformation:
                $"Finding Type: {findingType}\nFinding Class: {findingClass}\n" +
                $"Candidate Severity: {candidateSeverity}\nConfirmed Vulnerability: {(confirmed ? "Yes" : "No")}\n{weakness}\n" +
                $"Exploitability: {exploitability}\nDemonstrated Impact: {(confirmed ? "Yes" : "No")}\n" +
                $"Bug bounty severity: {bbSeverity}\nSubmission Recommendation: {submission}",
            ExpectedResult:
                "Privileged data and functionality remain inaccessible to unauthenticated or lower-privileged users.",
            ActualResult: confirmed
                ? "Unauthorized privileged access was verified. See Surface Evidence and verification notes."
                : "Meaningful analyzer signals were recorded (see Surface Evidence / ManualReviewReasons). " +
                  "No unauthorized privileged access was demonstrated.",
            SuggestedRemediation:
                "Enforce authentication and authorization on privileged surfaces. Validate with dual-account tests before reporting BAC/IDOR.",
            TestingNotes: confirmed
                ? "Confirmed only after authorized validation demonstrated unauthorized privileged access."
                : "Safe GET/HEAD-style inspection only. No authentication bypass, password guessing, brute force, credential stuffing, " +
                  "privilege escalation, or destructive admin actions were performed. " +
                  "HackerOne submission is blocked while ConfirmedVulnerability=false / DemonstratedImpact=false.",
            EligibilityReason: eligibility,
            SurfaceEvidence: surfaceEvidence,
            ValidationEvidence: BuildValidationEvidenceSection(finding, confirmed, highPriority, reasons));
    }

    private static string BuildValidationEvidenceSection(
        Finding finding,
        bool confirmed,
        bool highPriority,
        IReadOnlyList<string> reasons)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Validation status: {finding.LatestValidationStatus?.ToString() ?? "NotStarted"}");
        sb.AppendLine("Validation method: Finding Validation (safe GET/HEAD differential / passive)");
        sb.AppendLine("Test account roles: (see ValidationRun evidence; secrets never logged)");
        sb.AppendLine($"Authorization and scope status: SubmissionEligible={finding.SubmissionEligible}; Target bounty scope alone is insufficient");
        sb.AppendLine($"Confirmed Vulnerability: {(confirmed || finding.ConfirmedVulnerability ? "Yes" : "No")}");
        sb.AppendLine($"Demonstrated impact: {(confirmed || finding.DemonstratedImpact ? "Yes" : "No")}");
        sb.AppendLine($"Submission recommendation: {finding.SubmissionRecommendation}");
        sb.AppendLine($"Potential reward eligible: {finding.PotentialRewardEligible}");
        sb.AppendLine("Reward eligibility disclaimer: Reward not guaranteed.");
        if (reasons.Count > 0)
        {
            sb.AppendLine("Manual review notes:");
            foreach (var r in reasons)
            {
                sb.AppendLine($"- {r}");
            }
        }

        if (!confirmed && !finding.ConfirmedVulnerability)
        {
            sb.AppendLine();
            sb.AppendLine(
                "This is a vulnerability candidate only. No unauthorized access or demonstrated security impact has been confirmed. " +
                "Do not submit solely on the basis of path existence or analyzer signals.");
            if (highPriority)
            {
                sb.AppendLine("High-priority candidate signals require Finding Validation before any SubmitCandidate decision.");
            }
        }

        return sb.ToString().TrimEnd();
    }

    private static bool HasUsableSurfaceEvidence(string? evidence)
    {
        if (string.IsNullOrWhiteSpace(evidence) || LooksTurkish(evidence))
        {
            return false;
        }

        return evidence.Contains("## Surface Evidence", StringComparison.OrdinalIgnoreCase)
               && evidence.Contains("HTTP Status:", StringComparison.OrdinalIgnoreCase)
               && evidence.Contains("URL:", StringComparison.OrdinalIgnoreCase);
    }

    private static string ExtractSurfaceEvidence(string? evidence)
    {
        if (!HasUsableSurfaceEvidence(evidence))
        {
            return string.Empty;
        }

        var idx = evidence!.IndexOf("## Surface Evidence", StringComparison.OrdinalIgnoreCase);
        var slice = evidence[idx..];
        // Strip leading markdown header so the builder can render ## Surface Evidence once.
        var lines = slice.Split('\n');
        var body = string.Join('\n', lines.Skip(1)).Trim();
        return body;
    }

    private static IReadOnlyList<string> ExtractManualReviewReasons(string? evidence)
    {
        if (string.IsNullOrWhiteSpace(evidence))
        {
            return Array.Empty<string>();
        }

        var reasons = new List<string>();
        var inBlock = false;
        foreach (var raw in evidence.Split('\n'))
        {
            var line = raw.Trim();
            if (line.StartsWith("ManualReviewReasons:", StringComparison.OrdinalIgnoreCase))
            {
                inBlock = true;
                if (line.Contains("(none)", StringComparison.OrdinalIgnoreCase))
                {
                    return Array.Empty<string>();
                }

                continue;
            }

            if (!inBlock)
            {
                continue;
            }

            if (line.StartsWith("- "))
            {
                reasons.Add(line[2..].Trim());
                continue;
            }

            if (line.Length == 0 || line.StartsWith("##", StringComparison.Ordinal) || line.Contains(':'))
            {
                break;
            }
        }

        return reasons;
    }

    private HackerOneReportDraftFields BuildGenericEnglishFields(Finding finding)
    {
        var host = finding.ScanResult?.ScanJob?.DomainAsset?.HostName ?? "unknown-asset";
        var safeTarget = _markdown.FormatSafeUrlForSteps(
            string.IsNullOrWhiteSpace(finding.AffectedUrl) ? host : finding.AffectedUrl);
        var findingType = HackerOneReportLanguage.FormatFindingType(
            finding.Fingerprint, finding.FindingClass, finding.PolicyCategory);
        var candidateSeverity = HackerOneReportLanguage.FormatCandidateSeverity(
            finding.TechnicalPotentialSeverity, finding.BugBountySeverity);
        var bbSeverity = finding.BugBountySeverity == BugBountySeverity.Unassigned
            ? "Unassigned"
            : finding.BugBountySeverity.ToString();
        var weakness = string.IsNullOrWhiteSpace(finding.CweCode)
            ? "Potential Weakness: (unspecified)"
            : $"Potential Weakness: {finding.CweCode}";
        var submission = HackerOneReportLanguage.FormatSubmissionRecommendation(finding.SubmissionRecommendation);
        var exploitability = HackerOneReportLanguage.FormatExploitability(
            finding.Exploitability, finding.RequiresManualValidation);
        var confirmed = finding.FindingClass == FindingClass.Vulnerability && finding.DemonstratedImpact;
        var eligibility = BuildGenericEnglishEligibility(finding);
        var evidence = string.IsNullOrWhiteSpace(finding.Evidence) || LooksTurkish(finding.Evidence)
            ? "See platform finding metadata (redacted). No automated exploitation was performed."
            : finding.Evidence!;

        return new HackerOneReportDraftFields(
            Title: string.IsNullOrWhiteSpace(finding.Title) || LooksTurkish(finding.Title) ? findingType : finding.Title,
            Severity: bbSeverity,
            Asset: host,
            Weakness: weakness,
            Impact: finding.DemonstratedImpact
                ? "Demonstrated impact is recorded on the platform finding; validate before Submit."
                : "No unauthorized access to privileged data or functionality has been demonstrated. " +
                  "Confirmed Vulnerability: No. Demonstrated Impact: No.",
            StepsToReproduce:
                $"1. Target: {safeTarget}\n" +
                "2. Recorded observations:\n" +
                evidence + "\n" +
                "3. Validate impact manually before considering a HackerOne submission.",
            ProofOfConcept: evidence,
            Notes: eligibility,
            ConfirmedVulnerability: confirmed,
            DemonstratedImpact: finding.DemonstratedImpact,
            BugBountySeverityLabel: bbSeverity,
            Language: HackerOneReportLanguage.Code,
            FindingType: findingType,
            CandidateSeverity: candidateSeverity,
            ExploitabilityLabel: exploitability,
            SubmissionRecommendationLabel: submission,
            Summary: $"{findingType} on {host}. Manual review required unless impact is demonstrated.",
            VulnerabilityInformation:
                $"Finding Type: {findingType}\nCandidate Severity: {candidateSeverity}\n" +
                $"Confirmed Vulnerability: {(confirmed ? "Yes" : "No")}\n{weakness}\n" +
                $"Exploitability: {exploitability}\nDemonstrated Impact: {(finding.DemonstratedImpact ? "Yes" : "No")}\n" +
                $"Submission Recommendation: {submission}",
            ExpectedResult: "Secure handling without unauthorized privileged access.",
            ActualResult: finding.DemonstratedImpact
                ? "See recorded observations / evidence."
                : "No unauthorized access to privileged data or functionality has been demonstrated.",
            SuggestedRemediation: "Review and remediate according to the relevant CWE/OWASP guidance after manual validation.",
            TestingNotes:
                $"Report language: {HackerOneReportLanguage.Code}. Submission blocked while impact is not demonstrated.",
            EligibilityReason: eligibility);
    }

    private HackerOneReportDraftFields BuildTurkishDraftFieldsFromFinding(Finding finding)
    {
        if (HackerOneReportLanguage.IsXssCandidate(finding.Fingerprint, finding.PolicyCategory))
        {
            var host = finding.ScanResult?.ScanJob?.DomainAsset?.HostName ?? "bilinmeyen-varlik";
            var safeTarget = _markdown.FormatSafeUrlForSteps(
                string.IsNullOrWhiteSpace(finding.AffectedUrl) ? host : finding.AffectedUrl);
            var param = finding.AffectedParameter ?? "q";
            var eligibilityTr =
                "XSS adayı (VulnerabilityCandidate). Doğrulanmış zafiyet=Hayır, DemonstratedImpact=Hayır. " +
                "Tek marker yansıması XSS exploit kanıtı değildir; encoding/context ManualReview. Otomatik Submit yok.";
            return new HackerOneReportDraftFields(
                Title: "Yansıyan girdi / XSS adayı",
                Severity: "Unassigned",
                Asset: host,
                Weakness: "Olası zayıflık: CWE-79",
                Impact:
                    "Tek benzersiz zararsız marker sorgu parametresinde yansıtıldı. " +
                    "Bu XSS exploit kanıtı değildir; encoding/context için Manual Review gerekir.",
                StepsToReproduce:
                    $"1. Hedef: {safeTarget}\n2. `{param}` ile zararsız marker.\n3. Yansıma/encoding incele.\n4. XSS için tarayıcı PoC şart.",
                ProofOfConcept: finding.Evidence ?? "Yansıma meta verisi platformda.",
                Notes: eligibilityTr,
                ConfirmedVulnerability: false,
                DemonstratedImpact: false,
                BugBountySeverityLabel: "Unassigned",
                Language: "tr-TR",
                FindingType: "XSS Candidate",
                SubmissionRecommendationLabel: "Manuel inceleme",
                Summary: "XSS adayı — HackerOne’a otomatik gönderilmez.",
                EligibilityReason: eligibilityTr);
        }

        if (HackerOneReportLanguage.IsAccessControlCandidate(finding.Fingerprint, finding.PolicyCategory))
        {
            var host = finding.ScanResult?.ScanJob?.DomainAsset?.HostName ?? "bilinmeyen-varlik";
            var safeTarget = _markdown.FormatSafeUrlForSteps(
                string.IsNullOrWhiteSpace(finding.AffectedUrl) ? host : finding.AffectedUrl);
            var surface = ExtractSurfaceEvidence(finding.Evidence);
            var reasons = ExtractManualReviewReasons(finding.Evidence);
            var eligibilityTr =
                "AccessControlCandidate (doğrulanmamış). Yol varlığı zafiyet değildir. " +
                "Ayrıcalıklı veri/işleve yetkisiz erişim kanıtlanmadı. ManualReview — otomatik Submit yok.";
            return new HackerOneReportDraftFields(
                Title: "AccessControlCandidate — doğrulanmamış hassas yüzey",
                Severity: "Unassigned",
                Asset: host,
                Weakness: "Olası zayıflık: CWE-284",
                Impact:
                    "Aday: anlamlı yüzey sinyalleri kaydedildi; yetkisiz ayrıcalıklı erişim kanıtlanmadı. " +
                    "Yol varlığı tek başına Broken Access Control değildir.",
                StepsToReproduce: SensitiveSurfaceAnalyzer.BuildStepsFromSurfaceEvidence(finding.Evidence, safeTarget),
                ProofOfConcept: reasons.Count > 0
                    ? "ManualReviewReasons:\n" + string.Join("\n", reasons.Select(r => $"- {r}"))
                    : "Surface Evidence bölümüne bak.",
                Notes: eligibilityTr,
                ConfirmedVulnerability: false,
                DemonstratedImpact: false,
                BugBountySeverityLabel: "Unassigned",
                Language: "tr-TR",
                FindingType: "AccessControlCandidate",
                SubmissionRecommendationLabel: "Manuel inceleme",
                Summary: "AccessControl adayı — etki kanıtı yok; HackerOne’a gönderme.",
                EligibilityReason: eligibilityTr,
                SurfaceEvidence: surface);
        }

        var hostG = finding.ScanResult?.ScanJob?.DomainAsset?.HostName ?? "bilinmeyen-varlik";
        return new HackerOneReportDraftFields(
            Title: string.IsNullOrWhiteSpace(finding.Title) ? "Güvenlik adayı" : finding.Title,
            Severity: finding.BugBountySeverity.ToString(),
            Asset: hostG,
            Weakness: finding.CweCode ?? "Belirtilmedi",
            Impact: "Ayrıcalıklı veri veya işleve yetkisiz erişim kanıtlanmadı (aksi platformda işaretli değilse).",
            StepsToReproduce: finding.Evidence ?? "Platform gözlem kaydı.",
            ProofOfConcept: finding.Evidence ?? "—",
            Notes: $"Sınıf={finding.FindingClass}; Öneri={finding.SubmissionRecommendation}",
            Language: "tr-TR",
            FindingType: finding.FindingClass.ToString(),
            Summary: "İç inceleme (TR).",
            TestingNotes: "HackerOne’a gönderilmez.",
            EligibilityReason: $"DemonstratedImpact={finding.DemonstratedImpact}");
    }

    private static string BuildXssEnglishEligibility(Finding finding) =>
        "XSS Candidate / VulnerabilityCandidate. Bug bounty eligible: No (until browser-side impact is proven). " +
        $"Class={finding.FindingClass}, BugBountySeverity=Unassigned, DemonstratedImpact=False, " +
        "Recommendation=Manual Review. " +
        "Single-marker reflection alone is not XSS exploit proof; encoding/context require Manual Review. Never auto-Submit.";

    private static string BuildAccessControlEnglishEligibility(Finding finding) =>
        "AccessControlCandidate / VulnerabilityCandidate. Bug bounty eligible: No. " +
        "Class=VulnerabilityCandidate, BugBountySeverity=Unassigned, DemonstratedImpact=False, " +
        "Recommendation=Manual Review. " +
        "Path reachability (/admin, /login, /dashboard, etc.) is not a vulnerability. " +
        "No unauthorized access to privileged data or functionality has been demonstrated. Never auto-Submit.";

    private static string BuildGenericEnglishEligibility(Finding finding) =>
        $"Bug bounty eligible: {(finding.BugBountyEligible ? "Yes" : "No")}. " +
        $"Class={finding.FindingClass}, TechnicalSeverity={finding.TechnicalSeverity}, " +
        $"Exploitability={finding.Exploitability}, DemonstratedImpact={finding.DemonstratedImpact}, " +
        $"Recommendation={HackerOneReportLanguage.FormatSubmissionRecommendation(finding.SubmissionRecommendation)}.";

    private static string BuildXssEnglishProofOfConcept(Finding finding)
    {
        if (!string.IsNullOrWhiteSpace(finding.Evidence) && !LooksTurkish(finding.Evidence))
        {
            return finding.Evidence!;
        }

        return
            "Harmless unique marker reflection was observed in the HTTP response (see platform finding metadata). " +
            $"ReflectionContext={finding.ReflectionContext?.ToString() ?? "Unknown"}; " +
            $"HtmlEncoded={finding.HtmlEncoded?.ToString() ?? "n/a"}; " +
            $"AttributeEncoded={finding.AttributeEncoded?.ToString() ?? "n/a"}. " +
            "No executable JavaScript payload was used. This is not a confirmed XSS exploit.";
    }

    private static bool LooksTurkish(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        return text.Contains('ş', StringComparison.OrdinalIgnoreCase)
               || text.Contains('ı', StringComparison.Ordinal)
               || text.Contains('ğ', StringComparison.OrdinalIgnoreCase)
               || text.Contains('ü', StringComparison.OrdinalIgnoreCase)
               || text.Contains('ö', StringComparison.OrdinalIgnoreCase)
               || text.Contains('ç', StringComparison.OrdinalIgnoreCase)
               || text.Contains("BB adayı", StringComparison.OrdinalIgnoreCase)
               || text.Contains("Teknik", StringComparison.OrdinalIgnoreCase)
               || text.Contains("Öneri", StringComparison.OrdinalIgnoreCase)
               || text.Contains("gerekir", StringComparison.OrdinalIgnoreCase)
               || text.Contains("kanıt", StringComparison.OrdinalIgnoreCase)
               || text.Contains("yansıtıldı", StringComparison.OrdinalIgnoreCase)
               || text.Contains("benzersiz", StringComparison.OrdinalIgnoreCase)
               || text.Contains("Sınıf=", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<BugBountyProgram?> ResolveProgramAsync(Guid? programId, CancellationToken cancellationToken)
    {
        if (programId is Guid id)
        {
            return await _db.BugBountyPrograms.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        var settings = await EnsureSettingsAsync(cancellationToken);
        if (settings.DefaultBugBountyProgramId is Guid def)
        {
            return await _db.BugBountyPrograms.FirstOrDefaultAsync(p => p.Id == def, cancellationToken);
        }

        return await _db.BugBountyPrograms.FirstOrDefaultAsync(
            p => p.PolicyKey == BugBountyProgramKeys.AmazonVrp, cancellationToken);
    }

    private async Task<HackerOneWorkspaceSettings> EnsureSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await _db.HackerOneWorkspaceSettings.OrderBy(s => s.CreatedAt).FirstOrDefaultAsync(cancellationToken);
        if (settings is not null)
        {
            return settings;
        }

        var amazon = await _db.BugBountyPrograms.FirstOrDefaultAsync(
            p => p.PolicyKey == BugBountyProgramKeys.AmazonVrp, cancellationToken);
        settings = new HackerOneWorkspaceSettings
        {
            DefaultBugBountyProgramId = amazon?.Id,
            OpenReportUrlTemplate = _options.OpenReportUrlTemplate,
            MinReadinessScoreForSubmit = _options.MinReadinessScoreForSubmit,
            PreferEnglishReports = true
        };
        _db.HackerOneWorkspaceSettings.Add(settings);
        await _db.SaveChangesAsync(cancellationToken);
        return settings;
    }

    private HackerOneWorkspaceSettingsDto MapSettings(
        HackerOneWorkspaceSettings s,
        bool hasToken,
        bool hasIdentifier,
        string? identifierHint) =>
        new(s.Id, s.DefaultBugBountyProgramId, s.OpenReportUrlTemplate, s.MinReadinessScoreForSubmit,
            s.PreferEnglishReports, _options.ApiEnabled, hasToken, hasIdentifier, identifierHint);

    private static BugBountyProgramDto MapProgram(BugBountyProgram p) =>
        new(p.Id, p.PolicyKey, p.Name, p.Handle, p.Platform, p.OpenReportUrl, p.IsEnabled, p.LastSyncedAt,
            p.PolicyRules.Select(r => new BugBountyPolicyRuleDto(
                r.Id, r.PolicyCategory, r.RecommendationWhenDemonstrated,
                r.RecommendationWhenNotDemonstrated, r.Notes)).ToList(),
            p.OffersBounties, p.Currency, p.SubmissionState, p.OpenScope, p.State);

    private static HackerOneReportDraftDto MapDraft(HackerOneReportDraft d) =>
        new(d.Id, d.FindingId, d.BugBountyProgramId, d.Program?.Handle ?? "",
            d.Title, d.Severity, d.Asset, d.Weakness, d.Impact, d.StepsToReproduce,
            d.ProofOfConcept, d.Notes, d.MarkdownBody, d.ReportReadinessScore, d.Status,
            d.CreatedAt, d.UpdatedAt, d.TurkishMarkdownBody);
}
