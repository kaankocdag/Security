# Pasif Tarayıcı Tasarımı

## Bileşenler

- **`IScanQueue`** → Hangfire üzerinden `IScanExecutor.ExecuteAsync(scanJobId)` çağrısı.
- **`IScanExecutor`** → `PassiveScanExecutor` implementasyonu.
- **`IPassiveSecurityCheck`** → Tek metodlu kontrol arayüzü.
- **`SecureHttpClientFactory`** → SSRF koruması entegre HttpClient üretir.
- **`ITargetSafetyValidator`** → Her istek öncesi zorunlu güvenlik denetimi.
- **`ISecurityScoreCalculator`** → Bulgu ağırlıklandırma ve skor üretimi.
- **`IActivityEventPublisher`** → SignalR üzerinden gerçek zamanlı olay yayını.

## Kontrol listesi

- `HttpsCheck`, `HttpsRedirectCheck` — TLS zorunluluğu
- `CertificateCheck` — Sertifika geçerliliği ve zincir
- `SecurityHeadersCheck` — HSTS, CSP, X-Content-Type-Options, X-Frame-Options, Referrer-Policy, Permissions-Policy, Server, X-Powered-By
- `CookieSecurityCheck` — Secure/HttpOnly/SameSite denetimi
- `CorsConfigurationCheck` — Aşırı serbest CORS yapılandırmasının tespiti
- `MixedContentIndicatorCheck` — HTTPS içinde HTTP kaynak referansları
- `ErrorMessageLeakCheck` — Sunucu hata mesajlarında bilgi sızıntısı
- `WellKnownFileCheck` — security.txt, robots.txt, sitemap.xml

Her kontrol `Order` alanına sahip; executor bunları küçükten büyüğe sırayla çalıştırır ve `ProgressPercentage` günceller. Kontrol hataları `CheckStatus.Skipped/Failed` ile raporlanır fakat taramayı kesmez.

## Skor formülü

`ISecurityScoreCalculator` bulguları şu ağırlıklarla değerlendirir:

- Severity puanları (Confirmed): Critical −25, High −15, Medium −7, Low −3, Info −1
- Confidence çarpanları: Confirmed 1.0, StrongIndication 0.7, Recommendation 0.4
- False positive olarak işaretlenen bulgular hesaba katılmaz.

Sonuç 0-100 arasına sıkıştırılır ve harf notuna (A/B/C/D/F) çevrilir. Rapor açıklaması Türkçe üretilir.

## Yeniden test (Retest)

- `RetestFindingAsync` yeni bir `ScanJob` yaratır: `IsRetest = true`, `RetestForFindingId`, `PreviousScanJobId`.
- Executor tarama tamamlandığında bulguları önceki `ScanResult` ile karşılaştırır ve `RetestComparison` kaydı üretir:
  - Yeni taramada aynı `Fingerprint` yoksa → `Resolved`
  - Şiddet düştüyse → `Improved`
  - Şiddet arttıysa → `Regressed`
  - Aynı kaldıysa → `StillPresent`
- Çözümlenmiş bulguların ana kaydı `FindingStatus.Fixed` olarak güncellenir.
