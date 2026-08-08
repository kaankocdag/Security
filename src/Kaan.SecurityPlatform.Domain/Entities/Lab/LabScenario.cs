using Kaan.SecurityPlatform.Domain.Common;
using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Domain.Entities.Lab;

/// <summary>
/// Katalog meta kaydı. Çalıştırılabilir senaryo planı kod içi registry'dedir.
/// </summary>
public class LabScenario : BaseEntity
{
    public string ScenarioKey { get; set; } = string.Empty;
    public string TitleTr { get; set; } = string.Empty;
    public string SummaryTr { get; set; } = string.Empty;
    public LabRiskCategory RiskCategory { get; set; }
    public string VulnerableImageTag { get; set; } = string.Empty;
    public string PatchedImageTag { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public int DisplayOrder { get; set; }
}
