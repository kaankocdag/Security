namespace Kaan.SecurityPlatform.Domain.Enums;

public enum BugBountyPlatform
{
    HackerOne = 0,
    Other = 99
}

public enum HackerOneReportDraftStatus
{
    Draft = 0,
    Ready = 1,
    Submitted = 2,
    Archived = 3
}

public enum HackerOneSubmissionStatus
{
    Pending = 0,
    Submitted = 1,
    Failed = 2,
    Cancelled = 3
}
