namespace Kaan.SecurityPlatform.Application.Authorization;

/// <summary>
/// Sistem genelinde kullanılan yetki politikalarının merkezi tanımı.
/// Controller'lar ve endpoint'ler bu sabitleri referans alır.
/// </summary>
public static class PolicyNames
{
    public const string RequireApprovedMember = "RequireApprovedMember";
    public const string RequireSystemAdmin = "RequireSystemAdmin";
    public const string RequireCompanyAdmin = "RequireCompanyAdmin";
    public const string RequireDeveloper = "RequireDeveloper";
    public const string RequireSecurityAnalyst = "RequireSecurityAnalyst";
    public const string RequireViewer = "RequireViewer";
    public const string CanManageCompany = "CanManageCompany";
    public const string CanStartScan = "CanStartScan";
    public const string CanApproveMembership = "CanApproveMembership";
    public const string CanEditKnowledge = "CanEditKnowledge";
    public const string CanManageLab = "CanManageLab";
    public const string CanManageBugBounty = "CanManageBugBounty";
    public const string CanManageTestAccounts = "CanManageTestAccounts";
    public const string CanRunAuthenticatedScan = "CanRunAuthenticatedScan";
    public const string CanRevealTestAccountSecret = "CanRevealTestAccountSecret";
    public const string CanDeleteTestAccount = "CanDeleteTestAccount";
    public const string CanApproveRegistration = "CanApproveRegistration";
    public const string CanApproveActiveValidation = "CanApproveActiveValidation";
}
