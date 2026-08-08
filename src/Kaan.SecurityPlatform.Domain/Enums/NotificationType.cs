namespace Kaan.SecurityPlatform.Domain.Enums;

public enum NotificationType
{
    Info = 0,
    Success = 1,
    Warning = 2,
    Error = 3,
    ScanStarted = 10,
    ScanCompleted = 11,
    ScanFailed = 12,
    CriticalFinding = 13,
    DomainVerified = 20,
    DomainVerificationFailed = 21,
    MembershipApproved = 30,
    MembershipRejected = 31,
    RemediationUpdated = 40
}
