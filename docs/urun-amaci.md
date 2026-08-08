# Kaan Security Platform — Ne İşe Yarar?

## Tek cümlede

**Firmaların web sitelerini zarar vermeden tarayan, bulguları Türkçe açıklayan, nasıl düzeltileceğini gösteren ve düzeltme sonrası yeniden test eden bir “web güvenlik doktoru” SaaS platformudur.**

Bu ürün genel amaçlı bir saldırı / pentest / exploit aracı **değildir**. Serbest payload ve tahrip edici saldırı yoktur.

## Üç değerlendirme modu

| Mod | Ne yapar? |
| --- | --- |
| **PublicPassiveAssessment** | Kamuya açık sitelerde GET/HEAD pasif kontroller; domain doğrulama gerekmez; SystemAdmin |
| **IsolatedSecurityLab** | Allowlist hedeflerde imzalı lab senaryoları; serbest URL/payload yok; lab egress açık; SystemAdmin |
| **AuthorizedExternalAssessment** | Doğrulanmış domainde yetkili dış değerlendirme; SystemAdmin; mevcut kontrol paketi (exploit motoru yok) |

Ayrıntı: [assessment-modes.md](./assessment-modes.md)

## Kime hitap eder?

| Rol | Ne için kullanır? |
| --- | --- |
| Firma yöneticisi | Güvenlik skorunu ve yönetici özetini görür |
| Geliştirici / DevOps | Eksik header, cookie, TLS vb. bulguları düzeltir |
| Güvenlik analisti | Bulgu doğrulama, yeniden test, bilgi bankası |
| Platform admini (`SystemAdmin`) | Üye/firma onayı, KB, üç değerlendirme modu |

## Ne tarar? (PublicPassive / AuthorizedExternal paket)

- HTTPS / HTTP→HTTPS yönlendirme  
- TLS sertifika durumu  
- Güvenlik başlıkları (HSTS, CSP, X-Frame-Options, …)  
- Cookie güvenlik bayrakları  
- CORS yapılandırması  
- Mixed content göstergeleri  
- Hata mesajı sızıntısı  
- `security.txt` / `robots.txt` / `sitemap.xml`  

## Bulgu doğrulama / Bug bounty

Scanner çıktısı otomatik vulnerability sayılmaz. Amazon VRP politikası ile sınıflandırılır;
raporda **Security Assessment** ve **Bug Bounty Submission Candidates** ayrıdır.
Ayrıntı: [finding-validation-bug-bounty.md](./finding-validation-bug-bounty.md)

## Ne yapmaz?

- Brute force, serbest exploit, özel payload, form gönderimi  
- POST/PUT/DELETE ile hedefi değiştirme  
- Localhost / özel IP / cloud metadata’ya istek (SSRF koruması)  
- Lab dışında kullanıcı tanımlı shell/script  

## Teknoloji özeti

- **Backend:** ASP.NET Core 10, Clean Architecture, EF Core, SQL Server, Hangfire, SignalR  
- **Frontend:** Next.js 15 App Router, TypeScript, Tailwind  
- **Dağıtım:** Docker Compose (API + ScannerWorker + LabWorker + Web + MSSQL)

Yerel kurulum için [README.md](../README.md).
