using Kaan.SecurityPlatform.Domain.Common;

namespace Kaan.SecurityPlatform.Domain.Entities.BugBounty;

/// <summary>Global (tek satır) HackerOne workspace ayarları.</summary>
public class HackerOneWorkspaceSettings : BaseEntity
{
    public Guid? DefaultBugBountyProgramId { get; set; }
    public string OpenReportUrlTemplate { get; set; } = "https://hackerone.com/{handle}";
    public int MinReadinessScoreForSubmit { get; set; } = 70;
    public bool PreferEnglishReports { get; set; } = true;

    public BugBountyProgram? DefaultProgram { get; set; }
}
