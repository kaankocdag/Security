namespace Kaan.SecurityPlatform.Domain.Enums;

public enum LabExecutionStatus
{
    PendingElevation = 0,
    Queued = 1,
    Running = 2,
    Completed = 3,
    Failed = 4,
    Cancelled = 5,
    CleaningUp = 6,
    Destroyed = 7
}

public enum LabStepKind
{
    VulnerableStart = 1,
    ControlRun = 2,
    ImpactDemo = 3,
    ShowLogs = 4,
    ExplainSecure = 5,
    ShowPatch = 6,
    SecureStart = 7,
    Retest = 8,
    Compare = 9,
    Destroy = 10
}

public enum LabStepStatus
{
    Pending = 0,
    Running = 1,
    Succeeded = 2,
    Failed = 3,
    Skipped = 4
}

public enum LabRuntimeMode
{
    Mock = 0,
    Docker = 1
}

public enum LabRiskCategory
{
    InputValidation = 0,
    OutputEncoding = 1,
    Session = 2,
    Authorization = 3,
    FileHandling = 4,
    Cryptography = 5,
    SecurityHeaders = 6,
    QueryConstruction = 7
}

public enum LabEnvironmentStatus
{
    Creating = 0,
    Ready = 1,
    RunningVulnerable = 2,
    RunningPatched = 3,
    Stopping = 4,
    Destroyed = 5,
    Failed = 6
}
