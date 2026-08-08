using Kaan.SecurityPlatform.Domain.Common;
using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Domain.Entities.Validation;

public class ValidationEvidence : BaseEntity
{
    public Guid ValidationRunId { get; set; }
    public ValidationEvidenceType EvidenceType { get; set; } = ValidationEvidenceType.HttpObservation;
    public string RequestMethod { get; set; } = "GET";
    public string? RedactedRequestUrl { get; set; }
    public int? ResponseStatusCode { get; set; }
    public string? FinalUrl { get; set; }
    public string? RedirectChain { get; set; }
    public string? ResponseContentType { get; set; }
    public string? ResponseHash { get; set; }
    public string? RedactedResponseExcerpt { get; set; }
    public ValidationSessionRole SessionRole { get; set; } = ValidationSessionRole.Anonymous;
    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;

    public FindingValidationRun? ValidationRun { get; set; }
}
