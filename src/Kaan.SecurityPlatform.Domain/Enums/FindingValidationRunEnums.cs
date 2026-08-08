namespace Kaan.SecurityPlatform.Domain.Enums;

public enum ValidationStatus
{
    NotStarted = 0,
    PreconditionsMissing = 1,
    AwaitingUserApproval = 2,
    Running = 3,
    CandidateOnly = 4,
    Confirmed = 5,
    NotReproduced = 6,
    Inconclusive = 7,
    ManualReviewRequired = 8,
    BlockedByPolicy = 9,
    Stopped = 10,
    Failed = 11
}

public enum ValidationMode
{
    PassiveReadOnly = 0,
    SafeDifferential = 1,
    ManualOnly = 2
}

public enum ValidationRiskLevel
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

public enum ValidationAutomationKind
{
    Automatic = 0,
    SemiAutomatic = 1,
    ManualOnly = 2
}

public enum ValidationImpactType
{
    None = 0,
    HardeningGap = 1,
    ConfigurationAnomaly = 2,
    UnauthorizedDataRead = 3,
    PrivilegedFunctionExposure = 4,
    CrossOriginReadable = 5,
    OpenRedirect = 6,
    Other = 99
}

public enum ValidationEvidenceType
{
    HttpObservation = 0,
    HeaderInspection = 1,
    CookieInspection = 2,
    TlsInspection = 3,
    DifferentialComparison = 4,
    PolicyDecision = 5,
    ManualGuidance = 6
}

public enum ValidationSessionRole
{
    Anonymous = 0,
    TestAccountA = 1,
    TestAccountB = 2,
    AuthorizedAdminTestAccount = 3
}

public enum ScopePolicyStatus
{
    Unknown = 0,
    InScope = 1,
    OutOfScope = 2,
    Expired = 3,
    Unverified = 4
}

/// <summary>Validation-run submission recommendation (Finding.SubmissionRecommendation ile eşlenir).</summary>
public enum ValidationSubmissionRecommendation
{
    DoNotSubmit = 0,
    ManualReview = 1,
    NeedsAdditionalEvidence = 2,
    SubmitCandidate = 3
}

public enum ValidationConfidence
{
    Low = 0,
    Medium = 1,
    High = 2
}
