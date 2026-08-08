using Microsoft.AspNetCore.Identity;

namespace Kaan.SecurityPlatform.Infrastructure.Identity;

public class ApplicationRole : IdentityRole<Guid>
{
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }

    public ApplicationRole()
    {
    }

    public ApplicationRole(string roleName) : base(roleName)
    {
    }
}
