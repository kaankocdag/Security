namespace Kaan.SecurityPlatform.Application.Features.Lab;

public static class LabConstants
{
    public const string ConfirmPhrase = "LABORATUVAR SENARYOSUNU BASLATMAYI ONAYLIYORUM";
    public const int ElevationMinutes = 10;
    public const int ExecutionTimeoutMinutes = 15;
    public const string AuditCategory = "lab";
    public const string HangfireQueue = "labs";
    public const string InternalNetworkName = "kaan-lab-net";
    public const string ImageAllowlistPrefix = "kaan-lab/";

    public static readonly string[] AllowedScenarioKeys =
    [
        LabScenarioKeys.InputValidationFailure,
        LabScenarioKeys.OutputEncodingFailure,
        LabScenarioKeys.InsecureSessionConfig,
        LabScenarioKeys.BrokenAccessControl,
        LabScenarioKeys.InsecureFileValidation,
        LabScenarioKeys.InsecureJwtConfig,
        LabScenarioKeys.MissingSecurityHeaders,
        LabScenarioKeys.UnsafeQueryConstruction
    ];

    /// <summary>API gövdesinde yasak alan adları (case-insensitive).</summary>
    public static readonly string[] ForbiddenRequestFields =
    [
        "url", "host", "ip", "port", "payload", "command", "script", "file",
        "target", "endpoint", "hostname", "address", "shell", "cmd"
    ];
}

public static class LabScenarioKeys
{
    public const string InputValidationFailure = "InputValidationFailure";
    public const string OutputEncodingFailure = "OutputEncodingFailure";
    public const string InsecureSessionConfig = "InsecureSessionConfig";
    public const string BrokenAccessControl = "BrokenAccessControl";
    public const string InsecureFileValidation = "InsecureFileValidation";
    public const string InsecureJwtConfig = "InsecureJwtConfig";
    public const string MissingSecurityHeaders = "MissingSecurityHeaders";
    public const string UnsafeQueryConstruction = "UnsafeQueryConstruction";
}
