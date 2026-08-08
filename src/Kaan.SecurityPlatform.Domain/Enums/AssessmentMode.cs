namespace Kaan.SecurityPlatform.Domain.Enums;

/// <summary>
/// Desteklenen değerlendirme modları.
/// </summary>
public enum AssessmentMode
{
    /// <summary>
    /// Kamuya açık sitelerde yalnızca GET/HEAD pasif kontroller.
    /// Domain doğrulama / firma izni gerekmez. SystemAdmin başlatır.
    /// </summary>
    PublicPassiveAssessment = 1,

    /// <summary>
    /// Önceden tanımlı laboratuvar senaryoları; hedef yalnızca allowlist'teki siteler.
    /// Kullanıcı URL/IP/payload giremez. SystemAdmin başlatır.
    /// </summary>
    IsolatedSecurityLab = 2,

    /// <summary>
    /// Doğrulanmış domain üzerinde yetkili dış değerlendirme.
    /// SystemAdmin + IsVerified zorunlu. Serbest exploit/payload yoktur;
    /// mevcut güvenlik kontrol paketi çalıştırılır.
    /// </summary>
    AuthorizedExternalAssessment = 3,

    /// <summary>
    /// Doğrulanmış domainde güvenli application-security candidate motorları
    /// (AccessControl heuristik, tek XSS marker, CORS, info-disclosure).
    /// Agresif payload / DoS / stuffing yok. SystemAdmin + IsVerified.
    /// </summary>
    ApplicationSecurityCandidate = 4
}
