namespace Kaan.SecurityPlatform.Application.Authorization;

public static class Roles
{
    public const string SystemAdmin = "SystemAdmin";
    public const string CompanyAdmin = "CompanyAdmin";
    public const string Developer = "Developer";
    public const string SecurityAnalyst = "SecurityAnalyst";
    public const string Viewer = "Viewer";

    public static readonly IReadOnlyCollection<string> All = new[]
    {
        SystemAdmin,
        CompanyAdmin,
        Developer,
        SecurityAnalyst,
        Viewer
    };
}
