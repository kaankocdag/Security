namespace Kaan.SecurityPlatform.Application.Common.Exceptions;

public class KaanApplicationException : Exception
{
    public string ErrorCode { get; }

    public KaanApplicationException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }

    public KaanApplicationException(string errorCode, string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}

public sealed class NotFoundException : KaanApplicationException
{
    public NotFoundException(string entityType, object id)
        : base("not_found", $"{entityType} bulunamadı: {id}")
    {
    }
}

public sealed class ForbiddenAccessException : KaanApplicationException
{
    public ForbiddenAccessException(string reason)
        : base("forbidden", reason)
    {
    }
}

public sealed class MembershipNotApprovedException : KaanApplicationException
{
    public MembershipNotApprovedException()
        : base("membership_not_approved", "Üyeliğiniz henüz onaylanmadı. Lütfen sistem yöneticisinin onayını bekleyin.")
    {
    }
}

public sealed class DomainNotVerifiedException : KaanApplicationException
{
    public DomainNotVerifiedException(string host)
        : base("domain_not_verified", $"'{host}' domaini doğrulanmadan tarama yapılamaz.")
    {
    }
}

public sealed class UnsafeScanTargetException : KaanApplicationException
{
    public UnsafeScanTargetException(string reason)
        : base("unsafe_scan_target", reason)
    {
    }
}
