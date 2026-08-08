# Yol Haritası

## v0.1 (Bu sürüm)

- ASP.NET Core 10 + Next.js 15 iskeleti
- JWT + refresh token + admin onaylı üyelik
- SecurityProject / DomainAsset CRUD
- Domain doğrulama (DNS TXT, HTML dosya, Meta etiket, Mock)
- Pasif tarayıcı iskeleti + 9 farklı `IPassiveSecurityCheck`
- Finding + Report + SecurityScore + retest karşılaştırma
- Hangfire Worker + SignalR ActivityHub
- Zafiyet Bilgi Bankası: kategori, makale, medya upload, admin CRUD
- Rol bazlı taşınabilir/küçültülebilir ActivityConsole widget
- Docker Compose ile MSSQL + API + Worker + Web + Nginx

## v0.2

- QuestPDF ile PDF rapor
- Gerçek e-posta (SMTP)
- Slack / Teams / Discord bildirimleri
- Firma alt hesabı (multi-workspace) desteği
- Planlı taramalar için `RecurringJobs`

## v0.3

- Semgrep / Trivy / Gitleaks entegrasyonu (Docker sidecar)
- Github/GitLab OAuth ile depo bağlama
- CI/CD entegrasyonu (`kaan-security check` CLI)

## v0.4+

- Ödeme, plan/abonelik yönetimi (Stripe)
- Çok dilli (EN/TR) full localization
- Mobile push bildirimi
- ML tabanlı false positive önceliklendirme
