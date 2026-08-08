# Finding Validation & Bug Bounty Eligibility

Scanner çıktısı otomatik olarak **vulnerability** sayılmaz. Her bulgu Amazon VRP / HackerOne tarzı politikalara göre sınıflandırılır.

## Alanlar

| Alan | Açıklama |
| --- | --- |
| `Severity` | Scanner şiddeti (değişmez) |
| `TechnicalSeverity` | Doğrulama sonrası teknik şiddet |
| `Exploitability` | None → Demonstrated |
| `DemonstratedImpact` | Gerçek etki kanıtlandı mı? |
| `RequiresManualValidation` | Manuel doğrulama gerekir mi? |
| `FindingClass` | Vulnerability / Misconfiguration / Hardening / Informational / Compliance / SEO |
| `BugBountyEligible` | BB aday listesine girer mi? |
| `EligibilityReason` | Kısa gerekçe |
| `ProgramPolicyMatch` | örn. `AmazonVRP` |
| `SubmissionRecommendation` | Submit / ManualReview / DoNotSubmit |

## AmazonVRPPolicy (özet)

| Kategori | Öneri |
| --- | --- |
| MissingSecurityHeaders | DoNotSubmit |
| MissingCookieFlags | DoNotSubmit |
| Clickjacking (impact yok) | DoNotSubmit |
| ScannerOutputOnly | DoNotSubmit |
| XSS (impact var) | Submit |
| SQLi / IDOR / AuthBypass / PrivEsc | Submit |
| InformationDisclosure | ManualReview |

Pasif scanner bugün XSS/SQLi üretmez; bu kategoriler gelecekteki doğrulanmış bulgular içindir. **Exploit motoru yoktur.**

## Rapor bölümleri

1. **Security Assessment Findings** — tüm sınıflandırılmış bulgular  
2. **Bug Bounty Submission Candidates** — yalnızca `BugBountyEligible && DemonstratedImpact` ve `DoNotSubmit` olmayanlar  

Puanlama `TechnicalSeverity` kullanır; scanner High/Critical hardening bulguları skoru şişirmez.
