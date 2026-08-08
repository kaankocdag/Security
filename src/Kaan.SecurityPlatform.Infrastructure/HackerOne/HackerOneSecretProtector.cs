using Kaan.SecurityPlatform.Application.Features.HackerOne;
using Microsoft.AspNetCore.DataProtection;

namespace Kaan.SecurityPlatform.Infrastructure.HackerOne;

public sealed class HackerOneSecretProtector : IHackerOneSecretProtector
{
    private readonly IDataProtector _protector;

    public HackerOneSecretProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("Kaan.SecurityPlatform.HackerOne.ApiToken.v1");
    }

    public string Protect(string plaintext) => _protector.Protect(plaintext);

    public string Unprotect(string protectedPayload) => _protector.Unprotect(protectedPayload);
}
