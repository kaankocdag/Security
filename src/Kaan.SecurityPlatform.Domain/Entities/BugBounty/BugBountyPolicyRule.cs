using Kaan.SecurityPlatform.Domain.Common;
using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Domain.Entities.BugBounty;

public class BugBountyPolicyRule : BaseEntity
{
    public Guid BugBountyProgramId { get; set; }
    public BugBountyPolicyCategory PolicyCategory { get; set; }
    public SubmissionRecommendation RecommendationWhenDemonstrated { get; set; }
    public SubmissionRecommendation RecommendationWhenNotDemonstrated { get; set; }
    public string? Notes { get; set; }

    public BugBountyProgram? Program { get; set; }
}
