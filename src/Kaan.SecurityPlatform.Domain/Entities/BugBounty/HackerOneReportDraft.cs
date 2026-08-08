using Kaan.SecurityPlatform.Domain.Common;
using Kaan.SecurityPlatform.Domain.Entities.Findings;
using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Domain.Entities.BugBounty;

public class HackerOneReportDraft : BaseEntity, IAuditableEntity
{
    public Guid FindingId { get; set; }
    public Guid BugBountyProgramId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Asset { get; set; } = string.Empty;
    public string Weakness { get; set; } = string.Empty;
    public string Impact { get; set; } = string.Empty;
    public string StepsToReproduce { get; set; } = string.Empty;
    public string ProofOfConcept { get; set; } = string.Empty;
    public string? Notes { get; set; }
    /// <summary>HackerOne’a kopyalanacak İngilizce rapor (en-US).</summary>
    public string? MarkdownBody { get; set; }
    /// <summary>İç inceleme için Türkçe rapor — HackerOne’a gönderilmez.</summary>
    public string? TurkishMarkdownBody { get; set; }
    public int ReportReadinessScore { get; set; }
    public HackerOneReportDraftStatus Status { get; set; } = HackerOneReportDraftStatus.Draft;
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    public Finding? Finding { get; set; }
    public BugBountyProgram? Program { get; set; }
    public ICollection<HackerOneSubmissionRecord> Submissions { get; set; } = new List<HackerOneSubmissionRecord>();
}
