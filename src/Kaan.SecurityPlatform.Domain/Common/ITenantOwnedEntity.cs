namespace Kaan.SecurityPlatform.Domain.Common;

/// <summary>
/// Firma bazlı çok kiracılı izolasyon için işaretleyici arayüz.
/// Bu arayüzü uygulayan her entity, EF Core global query filter aracılığıyla
/// oturumdaki kullanıcının firmasına göre otomatik filtrelenir.
/// </summary>
public interface ITenantOwnedEntity
{
    Guid CompanyId { get; set; }
}
