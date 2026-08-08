using System.Security.Cryptography;
using System.Text;
using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Application.Common.Interfaces.Lab;
using Kaan.SecurityPlatform.Application.Common.Models;
using Kaan.SecurityPlatform.Application.Features.Lab;
using Kaan.SecurityPlatform.Application.Features.Lab.Dtos;
using Kaan.SecurityPlatform.Domain.Entities.Lab;
using Kaan.SecurityPlatform.Domain.Enums;
using Kaan.SecurityPlatform.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Kaan.SecurityPlatform.Infrastructure.Lab;

public sealed class LabExecutionService : ILabExecutionService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly ILabScenarioRegistry _registry;
    private readonly ILabAuditService _labAudit;
    private readonly ILabQueue _queue;
    private readonly ILabCleanupService _cleanup;
    private readonly IAssessmentModeGuard _modeGuard;
    private readonly IHostEnvironment _environment;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly LabOptions _options;

    public LabExecutionService(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        ILabScenarioRegistry registry,
        ILabAuditService labAudit,
        ILabQueue queue,
        ILabCleanupService cleanup,
        IAssessmentModeGuard modeGuard,
        IHostEnvironment environment,
        UserManager<ApplicationUser> userManager,
        IOptions<LabOptions> options)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _registry = registry;
        _labAudit = labAudit;
        _queue = queue;
        _cleanup = cleanup;
        _modeGuard = modeGuard;
        _environment = environment;
        _userManager = userManager;
        _options = options.Value;
    }

    public async Task<Result<ElevateLabResponse>> ElevateAsync(ElevateLabRequest request, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is null || string.IsNullOrWhiteSpace(_currentUser.Email))
        {
            return Result<ElevateLabResponse>.Failure("unauthorized", "Oturum gerekli.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return Result<ElevateLabResponse>.Failure("password_required", "Parola gerekli.");
        }

        var user = await _userManager.FindByIdAsync(_currentUser.UserId.Value.ToString());
        if (user is null)
        {
            return Result<ElevateLabResponse>.Failure("user_not_found", "Kullanıcı bulunamadı.");
        }

        var ok = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!ok)
        {
            var corrFail = Guid.NewGuid();
            await _labAudit.WriteAsync(corrFail, "lab.elevate.failed", "LabElevationTicket", null, new { Reason = "bad_password" }, cancellationToken);
            return Result<ElevateLabResponse>.Failure("invalid_password", "Parola doğrulanamadı.");
        }

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var hash = HashToken(token);
        var lifetime = _options.ElevationMinutes > 0 ? _options.ElevationMinutes : LabConstants.ElevationMinutes;
        var expires = _clock.UtcNow.AddMinutes(lifetime);

        var ticket = new LabElevationTicket
        {
            UserId = user.Id,
            TokenHash = hash,
            ExpiresAt = expires,
            ClientIp = _currentUser.IpAddress ?? "unknown"
        };
        _db.LabElevationTickets.Add(ticket);
        await _db.SaveChangesAsync(cancellationToken);

        await _labAudit.WriteAsync(Guid.NewGuid(), "lab.elevate.succeeded", "LabElevationTicket", ticket.Id.ToString(),
            new { ExpiresAt = expires }, cancellationToken);

        return Result<ElevateLabResponse>.Success(new ElevateLabResponse(token, expires, lifetime));
    }

    public Task<IReadOnlyList<LabScenarioDto>> ListScenariosAsync(CancellationToken cancellationToken = default)
    {
        var list = _registry.GetAll()
            .Select(s => new LabScenarioDto(
                s.ScenarioKey,
                s.TitleTr,
                s.SummaryTr,
                s.RiskCategory,
                s.IsFullyImplemented,
                s.DisplayOrder))
            .ToList();
        return Task.FromResult<IReadOnlyList<LabScenarioDto>>(list);
    }

    public async Task<IReadOnlyList<LabTargetSiteDto>> ListTargetSitesAsync(CancellationToken cancellationToken = default)
    {
        return await _db.LabTargetSites.AsNoTracking()
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new LabTargetSiteDto(
                t.Id, t.HostName, t.NormalizedHostName, t.NotesTr, t.IsEnabled, t.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<Result<LabTargetSiteDto>> AddTargetSiteAsync(CreateLabTargetSiteRequest request, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is null)
        {
            return Result<LabTargetSiteDto>.Failure("unauthorized", "Oturum gerekli.");
        }

        var host = (request.HostName ?? string.Empty).Trim().TrimEnd('.').ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(host) || host.Contains('/') || host.Contains(':') || host.Contains(' '))
        {
            return Result<LabTargetSiteDto>.Failure(
                "invalid_host",
                "Yalnızca hostname girin (ör. example.com). URL, IP, port veya path kabul edilmez.");
        }

        if (await _db.LabTargetSites.AnyAsync(t => t.NormalizedHostName == host, cancellationToken))
        {
            return Result<LabTargetSiteDto>.Failure("duplicate_host", "Bu hedef zaten allowlist'te.");
        }

        var site = new LabTargetSite
        {
            HostName = request.HostName.Trim(),
            NormalizedHostName = host,
            NotesTr = request.NotesTr?.Trim(),
            IsEnabled = true,
            CreatedByUserId = _currentUser.UserId.Value,
            CreatedByEmail = _currentUser.Email ?? string.Empty
        };
        _db.LabTargetSites.Add(site);
        await _db.SaveChangesAsync(cancellationToken);

        await _labAudit.WriteAsync(Guid.NewGuid(), "lab.target.added", "LabTargetSite", site.Id.ToString(),
            new { site.NormalizedHostName }, cancellationToken);

        return Result<LabTargetSiteDto>.Success(new LabTargetSiteDto(
            site.Id, site.HostName, site.NormalizedHostName, site.NotesTr, site.IsEnabled, site.CreatedAt));
    }

    public async Task<Result> DisableTargetSiteAsync(Guid targetSiteId, CancellationToken cancellationToken = default)
    {
        var site = await _db.LabTargetSites.FirstOrDefaultAsync(t => t.Id == targetSiteId, cancellationToken);
        if (site is null)
        {
            return Result.Failure("not_found", "Hedef site bulunamadı.");
        }

        site.IsEnabled = false;
        site.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await _labAudit.WriteAsync(Guid.NewGuid(), "lab.target.disabled", "LabTargetSite", site.Id.ToString(),
            new { site.NormalizedHostName }, cancellationToken);
        return Result.Success();
    }

    public async Task<Result<StartLabExecutionResponse>> StartAsync(StartLabExecutionRequest request, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is null)
        {
            return Result<StartLabExecutionResponse>.Failure("unauthorized", "Oturum gerekli.");
        }

        var nameCheck = _modeGuard.EnsureNameAllowed(request.AssessmentModeName ?? AssessmentModeNames.IsolatedSecurityLab);
        if (nameCheck.IsFailure)
        {
            return Result<StartLabExecutionResponse>.Failure(nameCheck.ErrorCode!, nameCheck.ErrorMessage!);
        }

        var envCheck = _modeGuard.EnsureEnvironmentAllows(
            AssessmentMode.IsolatedSecurityLab, _environment.EnvironmentName);
        if (envCheck.IsFailure)
        {
            return Result<StartLabExecutionResponse>.Failure(envCheck.ErrorCode!, envCheck.ErrorMessage!);
        }

        if (!string.Equals(request.ConfirmPhrase?.Trim(), LabConstants.ConfirmPhrase, StringComparison.Ordinal))
        {
            return Result<StartLabExecutionResponse>.Failure(
                "confirm_required",
                $"Onay ifadesi tam olarak şöyle olmalıdır: {LabConstants.ConfirmPhrase}");
        }

        var scenario = _registry.Get(request.ScenarioKey ?? string.Empty);
        if (scenario is null)
        {
            return Result<StartLabExecutionResponse>.Failure("unknown_scenario", "Senaryo kayıtlı değil.");
        }

        var target = await _db.LabTargetSites.FirstOrDefaultAsync(
            t => t.Id == request.LabTargetSiteId && t.IsEnabled, cancellationToken);
        if (target is null)
        {
            return Result<StartLabExecutionResponse>.Failure(
                "target_required",
                "IsolatedSecurityLab yalnızca allowlist'e eklenmiş hedef sitelerde çalışır. Önce hedef ekleyin.");
        }

        var ticket = await ValidateElevationTokenAsync(request.ElevationToken, cancellationToken);
        if (ticket is null)
        {
            return Result<StartLabExecutionResponse>.Failure("elevation_required", "Geçerli lab yükselme bileti gerekli.");
        }

        ticket.ConsumedAt = _clock.UtcNow;
        var correlationId = Guid.NewGuid();
        var plan = scenario.GetSignedPlan();

        var execution = new LabExecution
        {
            ScenarioKey = scenario.ScenarioKey,
            AssessmentMode = AssessmentMode.IsolatedSecurityLab,
            Status = LabExecutionStatus.Queued,
            RuntimeMode = LabRuntimeMode.Mock,
            LabTargetSiteId = target.Id,
            TargetHostName = target.NormalizedHostName,
            ElevatedByUserId = _currentUser.UserId.Value,
            ElevatedByEmail = _currentUser.Email ?? string.Empty,
            AuditCorrelationId = correlationId,
            ElevationTicketId = ticket.Id
        };

        foreach (var step in plan.Steps)
        {
            execution.Steps.Add(new LabExecutionStep
            {
                StepKind = step.StepKind,
                StepOrder = step.StepOrder,
                TitleTr = step.TitleTr,
                Status = LabStepStatus.Pending
            });
        }

        execution.Approval = new LabAuthorizationApproval
        {
            UserId = _currentUser.UserId.Value,
            ConfirmPhrase = LabConstants.ConfirmPhrase,
            ClientIp = _currentUser.IpAddress ?? "unknown",
            UserAgent = _currentUser.UserAgent,
            ApprovedAt = _clock.UtcNow
        };

        _db.LabExecutions.Add(execution);
        await _db.SaveChangesAsync(cancellationToken);

        await _labAudit.WriteAsync(correlationId, "lab.execution.started", "LabExecution", execution.Id.ToString(),
            new
            {
                scenario.ScenarioKey,
                AssessmentMode = AssessmentMode.IsolatedSecurityLab.ToString(),
                target.NormalizedHostName,
                ConfirmPhrase = LabConstants.ConfirmPhrase
            }, cancellationToken);

        await _queue.EnqueueAsync(execution.Id, cancellationToken);

        return Result<StartLabExecutionResponse>.Success(
            new StartLabExecutionResponse(execution.Id, correlationId, execution.Status));
    }

    public async Task<IReadOnlyList<LabExecutionListItemDto>> ListExecutionsAsync(CancellationToken cancellationToken = default)
    {
        var items = await _db.LabExecutions.AsNoTracking()
            .OrderByDescending(e => e.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        return items.Select(e =>
        {
            var title = _registry.Get(e.ScenarioKey)?.TitleTr ?? e.ScenarioKey;
            return new LabExecutionListItemDto(
                e.Id,
                e.ScenarioKey,
                title,
                e.TargetHostName,
                e.Status,
                e.RuntimeMode,
                e.AuditCorrelationId,
                e.CreatedAt,
                e.CompletedAt);
        }).ToList();
    }

    public async Task<Result<LabExecutionDetailDto>> GetAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        var e = await _db.LabExecutions.AsNoTracking()
            .Include(x => x.Steps)
            .Include(x => x.Comparison)
            .FirstOrDefaultAsync(x => x.Id == executionId, cancellationToken);

        if (e is null)
        {
            return Result<LabExecutionDetailDto>.Failure("not_found", "Lab oturumu bulunamadı.");
        }

        var title = _registry.Get(e.ScenarioKey)?.TitleTr ?? e.ScenarioKey;
        var steps = e.Steps.OrderBy(s => s.StepOrder)
            .Select(s => new LabExecutionStepDto(
                s.StepKind, s.StepOrder, s.TitleTr, s.Status, s.SummaryTr, s.StartedAt, s.CompletedAt))
            .ToList();

        LabComparisonDto? cmp = e.Comparison is null
            ? null
            : new LabComparisonDto(
                e.Comparison.InitialTestFailed,
                e.Comparison.RetestSucceeded,
                e.Comparison.VulnerableScore,
                e.Comparison.PatchedScore,
                e.Comparison.RiskTr,
                e.Comparison.WhyTr,
                e.Comparison.FixTr,
                e.Comparison.SummaryTr);

        return Result<LabExecutionDetailDto>.Success(new LabExecutionDetailDto(
            e.Id,
            e.ScenarioKey,
            title,
            e.TargetHostName,
            e.AssessmentMode,
            e.Status,
            e.RuntimeMode,
            e.AuditCorrelationId,
            e.ElevatedByEmail,
            e.CreatedAt,
            e.StartedAt,
            e.CompletedAt,
            e.FailureReasonTr,
            steps,
            cmp));
    }

    public async Task<Result<IReadOnlyList<LabExecutionLogDto>>> GetLogsAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        var exists = await _db.LabExecutions.AsNoTracking().AnyAsync(e => e.Id == executionId, cancellationToken);
        if (!exists)
        {
            return Result<IReadOnlyList<LabExecutionLogDto>>.Failure("not_found", "Lab oturumu bulunamadı.");
        }

        var logs = await _db.LabExecutionLogs.AsNoTracking()
            .Where(l => l.LabExecutionId == executionId)
            .OrderBy(l => l.LoggedAt)
            .Select(l => new LabExecutionLogDto(l.Id, l.Level, l.MessageTr, l.LoggedAt, l.LabExecutionStepId))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<LabExecutionLogDto>>.Success(logs);
    }

    public async Task<Result> CancelAsync(Guid executionId, string? reasonTr = null, CancellationToken cancellationToken = default)
    {
        var e = await _db.LabExecutions.FirstOrDefaultAsync(x => x.Id == executionId, cancellationToken);
        if (e is null)
        {
            return Result.Failure("not_found", "Lab oturumu bulunamadı.");
        }

        if (e.Status is LabExecutionStatus.Completed or LabExecutionStatus.Destroyed or LabExecutionStatus.Cancelled)
        {
            return Result.Failure("already_finished", "Oturum zaten sonlanmış.");
        }

        e.Status = LabExecutionStatus.Cancelled;
        e.CancelledAt = _clock.UtcNow;
        e.CancelReasonTr = string.IsNullOrWhiteSpace(reasonTr) ? "Acil durdur" : reasonTr.Trim();
        await _db.SaveChangesAsync(cancellationToken);

        await _labAudit.WriteAsync(e.AuditCorrelationId, "lab.execution.cancelled", "LabExecution", e.Id.ToString(),
            new { e.CancelReasonTr }, cancellationToken);

        await _cleanup.CleanupExecutionAsync(executionId, e.CancelReasonTr, cancellationToken);
        return Result.Success();
    }

    private async Task<LabElevationTicket?> ValidateElevationTokenAsync(string? token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token) || _currentUser.UserId is null)
        {
            return null;
        }

        var hash = HashToken(token.Trim());
        var now = _clock.UtcNow;
        return await _db.LabElevationTickets
            .Where(t => t.UserId == _currentUser.UserId
                        && t.TokenHash == hash
                        && !t.IsRevoked
                        && t.ConsumedAt == null
                        && t.ExpiresAt > now)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
