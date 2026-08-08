using Kaan.SecurityPlatform.Application.Common.Interfaces.Lab;
using Kaan.SecurityPlatform.Application.Features.Lab;
using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Infrastructure.Lab.Scenarios;

internal abstract class LabScenarioBase : ILabScenario
{
    public abstract string ScenarioKey { get; }
    public abstract string TitleTr { get; }
    public abstract string SummaryTr { get; }
    public abstract LabRiskCategory RiskCategory { get; }
    public abstract string VulnerableImageTag { get; }
    public abstract string PatchedImageTag { get; }
    public abstract bool IsFullyImplemented { get; }
    public abstract int DisplayOrder { get; }
    protected abstract LabComparisonTemplate Comparison { get; }

    public LabSignedPlan GetSignedPlan()
    {
        var steps = new List<LabSignedStep>
        {
            new(LabStepKind.VulnerableStart, 1, "Zayıf ortamı başlat", "Kayıtlı zayıf lab imajı ayağa kalkar."),
            new(LabStepKind.ControlRun, 2, "Kontrol çalıştır", "İmzalı lab-içi kontrol zayıf yapılandırmayı gösterir."),
            new(LabStepKind.ImpactDemo, 3, "Etkiyi açıkla", "Eğitim amaçlı etki özeti (payload UI'da yok)."),
            new(LabStepKind.ShowLogs, 4, "Güvenli logları göster", "Sanitize edilmiş laboratuvar logları."),
            new(LabStepKind.ExplainSecure, 5, "Güvenli yaklaşımı anlat", "Doğru yapılandırma ilkeleri."),
            new(LabStepKind.ShowPatch, 6, "Yama farkını göster", "Patched imaj farkı özetlenir."),
            new(LabStepKind.SecureStart, 7, "Güvenli ortamı başlat", "Patched lab imajı ayağa kalkar."),
            new(LabStepKind.Retest, 8, "Yeniden test", "Aynı imzalı kontrol güvenli ortamda geçer."),
            new(LabStepKind.Compare, 9, "Karşılaştır", "Önce/sonra skor ve Türkçe özet."),
            new(LabStepKind.Destroy, 10, "Ortamı yok et", "Container ve ağ temizlenir.")
        };

        return new LabSignedPlan(ScenarioKey, steps, Comparison);
    }

    protected static LabComparisonTemplate SkeletonComparison(string topic) => new(
        $"{topic}: eğitim senaryosu iskeleti — mock kontrol ile risk özeti.",
        "Bu senaryo kayıtlı iskelettir; tam lab imajı sonraki sürümde tamamlanır.",
        "Parametreli, allowlist tabanlı ve en az ayrıcalık ilkelerine uyun.",
        "Mock ortamda ilk kontrol başarısız, yeniden test başarılı simüle edilir.");
}

internal sealed class MissingSecurityHeadersScenario : LabScenarioBase
{
    public override string ScenarioKey => LabScenarioKeys.MissingSecurityHeaders;
    public override string TitleTr => "Eksik güvenlik başlıkları";
    public override string SummaryTr =>
        "CSP, X-Content-Type-Options ve benzeri başlıkların eksikliğinin riskini izole lab'da gösterir.";
    public override LabRiskCategory RiskCategory => LabRiskCategory.SecurityHeaders;
    public override string VulnerableImageTag => "kaan-lab/missing-security-headers:vulnerable";
    public override string PatchedImageTag => "kaan-lab/missing-security-headers:patched";
    public override bool IsFullyImplemented => true;
    public override int DisplayOrder => 7;

    protected override LabComparisonTemplate Comparison => new(
        "Tarayıcı ve ara katman güvenlik başlıkları eksik; clickjacking ve MIME sniffing riski artar.",
        "Zayıf lab yanıtında beklenen güvenlik başlıkları yoktur.",
        "CSP, X-Frame-Options, X-Content-Type-Options, Referrer-Policy gibi başlıkları varsayılan olarak ekleyin.",
        "İlk kontrol başarısız; yama sonrası yeniden test başarılı.");
}

internal sealed class InsecureJwtScenario : LabScenarioBase
{
    public override string ScenarioKey => LabScenarioKeys.InsecureJwtConfig;
    public override string TitleTr => "Güvensiz JWT yapılandırması";
    public override string SummaryTr =>
        "Zayıf JWT doğrulama ayarlarının eğitim ortamında etkisini gösterir.";
    public override LabRiskCategory RiskCategory => LabRiskCategory.Cryptography;
    public override string VulnerableImageTag => "kaan-lab/insecure-jwt:vulnerable";
    public override string PatchedImageTag => "kaan-lab/insecure-jwt:patched";
    public override bool IsFullyImplemented => true;
    public override int DisplayOrder => 6;

    protected override LabComparisonTemplate Comparison => new(
        "JWT doğrulama zayıf; sahte veya manipüle token kabul edilebilir.",
        "Zayıf lab, imza algoritması kısıtını uygulamadan token kabul eder.",
        "Algoritmayı sabitleyin, güçlü anahtar kullanın, issuer/audience doğrulayın.",
        "İlk kontrol başarısız; yama sonrası yeniden test başarılı.");
}

internal sealed class InputValidationFailureScenario : LabScenarioBase
{
    public override string ScenarioKey => LabScenarioKeys.InputValidationFailure;
    public override string TitleTr => "Girdi doğrulama hatası";
    public override string SummaryTr => "Kayıtlı senaryo iskeleti: girdi doğrulama eksikliğinin eğitim özeti.";
    public override LabRiskCategory RiskCategory => LabRiskCategory.InputValidation;
    public override string VulnerableImageTag => "kaan-lab/input-validation:vulnerable";
    public override string PatchedImageTag => "kaan-lab/input-validation:patched";
    public override bool IsFullyImplemented => false;
    public override int DisplayOrder => 1;
    protected override LabComparisonTemplate Comparison => SkeletonComparison("Girdi doğrulama");
}

internal sealed class OutputEncodingFailureScenario : LabScenarioBase
{
    public override string ScenarioKey => LabScenarioKeys.OutputEncodingFailure;
    public override string TitleTr => "Çıktı kodlama hatası";
    public override string SummaryTr => "Kayıtlı senaryo iskeleti: çıktı kodlama eksikliğinin eğitim özeti.";
    public override LabRiskCategory RiskCategory => LabRiskCategory.OutputEncoding;
    public override string VulnerableImageTag => "kaan-lab/output-encoding:vulnerable";
    public override string PatchedImageTag => "kaan-lab/output-encoding:patched";
    public override bool IsFullyImplemented => false;
    public override int DisplayOrder => 2;
    protected override LabComparisonTemplate Comparison => SkeletonComparison("Çıktı kodlama");
}

internal sealed class InsecureSessionConfigScenario : LabScenarioBase
{
    public override string ScenarioKey => LabScenarioKeys.InsecureSessionConfig;
    public override string TitleTr => "Güvensiz oturum yapılandırması";
    public override string SummaryTr => "Kayıtlı senaryo iskeleti: oturum çerezi güvenliği.";
    public override LabRiskCategory RiskCategory => LabRiskCategory.Session;
    public override string VulnerableImageTag => "kaan-lab/insecure-session:vulnerable";
    public override string PatchedImageTag => "kaan-lab/insecure-session:patched";
    public override bool IsFullyImplemented => false;
    public override int DisplayOrder => 3;
    protected override LabComparisonTemplate Comparison => SkeletonComparison("Oturum yapılandırması");
}

internal sealed class BrokenAccessControlScenario : LabScenarioBase
{
    public override string ScenarioKey => LabScenarioKeys.BrokenAccessControl;
    public override string TitleTr => "Bozuk erişim kontrolü";
    public override string SummaryTr => "Kayıtlı senaryo iskeleti: yetkilendirme hatası eğitimi.";
    public override LabRiskCategory RiskCategory => LabRiskCategory.Authorization;
    public override string VulnerableImageTag => "kaan-lab/broken-access:vulnerable";
    public override string PatchedImageTag => "kaan-lab/broken-access:patched";
    public override bool IsFullyImplemented => false;
    public override int DisplayOrder => 4;
    protected override LabComparisonTemplate Comparison => SkeletonComparison("Erişim kontrolü");
}

internal sealed class InsecureFileValidationScenario : LabScenarioBase
{
    public override string ScenarioKey => LabScenarioKeys.InsecureFileValidation;
    public override string TitleTr => "Güvensiz dosya doğrulama";
    public override string SummaryTr => "Kayıtlı senaryo iskeleti: dosya yükleme doğrulama eğitimi.";
    public override LabRiskCategory RiskCategory => LabRiskCategory.FileHandling;
    public override string VulnerableImageTag => "kaan-lab/insecure-file:vulnerable";
    public override string PatchedImageTag => "kaan-lab/insecure-file:patched";
    public override bool IsFullyImplemented => false;
    public override int DisplayOrder => 5;
    protected override LabComparisonTemplate Comparison => SkeletonComparison("Dosya doğrulama");
}

internal sealed class UnsafeQueryConstructionScenario : LabScenarioBase
{
    public override string ScenarioKey => LabScenarioKeys.UnsafeQueryConstruction;
    public override string TitleTr => "Güvensiz sorgu oluşturma";
    public override string SummaryTr => "Kayıtlı senaryo iskeleti: parametreli sorgu eğitimi.";
    public override LabRiskCategory RiskCategory => LabRiskCategory.QueryConstruction;
    public override string VulnerableImageTag => "kaan-lab/unsafe-query:vulnerable";
    public override string PatchedImageTag => "kaan-lab/unsafe-query:patched";
    public override bool IsFullyImplemented => false;
    public override int DisplayOrder => 8;
    protected override LabComparisonTemplate Comparison => SkeletonComparison("Sorgu oluşturma");
}
