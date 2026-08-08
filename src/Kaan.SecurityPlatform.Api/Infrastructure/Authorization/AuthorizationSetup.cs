using Kaan.SecurityPlatform.Application.Authorization;
using Kaan.SecurityPlatform.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace Kaan.SecurityPlatform.Api.Infrastructure.Authorization;

public static class AuthorizationSetup
{
    public static IServiceCollection AddKaanAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(PolicyNames.RequireSystemAdmin, policy =>
                policy.RequireAuthenticatedUser().RequireRole(Roles.SystemAdmin));

            options.AddPolicy(PolicyNames.RequireCompanyAdmin, policy =>
                policy.RequireAuthenticatedUser().RequireRole(Roles.CompanyAdmin, Roles.SystemAdmin));

            options.AddPolicy(PolicyNames.RequireDeveloper, policy =>
                policy.RequireAuthenticatedUser().RequireRole(Roles.Developer, Roles.CompanyAdmin, Roles.SystemAdmin));

            options.AddPolicy(PolicyNames.RequireSecurityAnalyst, policy =>
                policy.RequireAuthenticatedUser().RequireRole(Roles.SecurityAnalyst, Roles.SystemAdmin));

            options.AddPolicy(PolicyNames.RequireViewer, policy =>
                policy.RequireAuthenticatedUser().RequireRole(Roles.Viewer, Roles.Developer, Roles.CompanyAdmin, Roles.SecurityAnalyst, Roles.SystemAdmin));

            options.AddPolicy(PolicyNames.RequireApprovedMember, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(ctx =>
                {
                    var raw = ctx.User.FindFirst(ClaimTypesExtended.MembershipStatus)?.Value;
                    if (!int.TryParse(raw, out var value))
                    {
                        return ctx.User.IsInRole(Roles.SystemAdmin);
                    }
                    return (MembershipStatus)value == MembershipStatus.Approved
                        || ctx.User.IsInRole(Roles.SystemAdmin);
                });
            });

            options.AddPolicy(PolicyNames.CanApproveMembership, policy =>
                policy.RequireAuthenticatedUser().RequireRole(Roles.SystemAdmin));

            options.AddPolicy(PolicyNames.CanEditKnowledge, policy =>
                policy.RequireAuthenticatedUser().RequireRole(Roles.SystemAdmin));

            // PublicPassiveAssessment: yalnızca SystemAdmin
            options.AddPolicy(PolicyNames.CanStartScan, policy =>
                policy.RequireAuthenticatedUser().RequireRole(Roles.SystemAdmin));

            options.AddPolicy(PolicyNames.CanManageCompany, policy =>
                policy.RequireAuthenticatedUser().RequireRole(Roles.CompanyAdmin, Roles.SystemAdmin));

            options.AddPolicy(PolicyNames.CanManageLab, policy =>
                policy.RequireAuthenticatedUser().RequireRole(Roles.SystemAdmin));

            options.AddPolicy(PolicyNames.CanManageBugBounty, policy =>
                policy.RequireAuthenticatedUser().RequireRole(Roles.SystemAdmin));

            // Authenticated scanning / test accounts — SystemAdmin only (HackerOne workspace)
            options.AddPolicy(PolicyNames.CanManageTestAccounts, policy =>
                policy.RequireAuthenticatedUser().RequireRole(Roles.SystemAdmin));
            options.AddPolicy(PolicyNames.CanRunAuthenticatedScan, policy =>
                policy.RequireAuthenticatedUser().RequireRole(Roles.SystemAdmin));
            options.AddPolicy(PolicyNames.CanRevealTestAccountSecret, policy =>
                policy.RequireAuthenticatedUser().RequireRole(Roles.SystemAdmin));
            options.AddPolicy(PolicyNames.CanDeleteTestAccount, policy =>
                policy.RequireAuthenticatedUser().RequireRole(Roles.SystemAdmin));
            options.AddPolicy(PolicyNames.CanApproveRegistration, policy =>
                policy.RequireAuthenticatedUser().RequireRole(Roles.SystemAdmin));
            options.AddPolicy(PolicyNames.CanApproveActiveValidation, policy =>
                policy.RequireAuthenticatedUser().RequireRole(Roles.SystemAdmin));
        });

        return services;
    }
}
