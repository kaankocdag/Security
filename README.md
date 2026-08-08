# Kaan Security Platform

<p align="center">
  <strong>Kaan Security Scanner — Safe Web Security Assessment Platform</strong><br/>
  <a href="#english">English</a> · <a href="#turkce">Türkçe</a>
</p>

---

<a id="english"></a>

## English

A **defensive SaaS platform** for performing safe web security assessments on **authorized** targets.

Kaan Security Platform runs passive checks, identifies application-security **candidates**, classifies findings, provides remediation guidance, supports retesting, and optionally assists with responsible-disclosure workflows similar to HackerOne.

> **Important:** This project does **not** perform brute-force attacks, credential stuffing, destructive exploitation, unauthorized scanning, or automatic vulnerability-report submission. The existence of a path such as `/admin` is **not** treated as a vulnerability by itself.

### Features

| Area | Capability |
|------|------------|
| Passive security assessment | HTTPS, TLS, security headers, cookies, CORS, mixed content, error disclosures, well-known files |
| Application Security Candidates (ASC) | Non-destructive signals: access-control surfaces, reflected input, CORS, info disclosure, subdomain takeover, JS secrets, API surfaces, open redirects |
| Sensitive Surface Analyzer | Safe GET inspection of `/admin`, `/dashboard`, etc. Login / 403 / harmless pages → `DoNotSubmit`; privileged-content signals → `ManualReview` |
| HackerOne-style workspace | Scope sync, targets, candidates, report drafting, readiness scoring |
| Authenticated assessment | Anonymous vs authenticated GET comparison via cookie session or manual browser login (including Google SSO without forcing SSO automation) |
| Finding validation | Confirm/classify candidates; no submit without demonstrated impact |
| Reporting | HTML/TXT security reports (including summaries when no actionable finding) |
| Domain verification | DNS TXT, HTML, meta-tag, and development mock; ASC requires verified domain |
| Controlled security lab | Isolated, signed training scenarios via LabWorker |
| Knowledge base | Turkish explanations and remediation articles linked to findings |

### What this project is not

- A tool for indiscriminate internet-wide scanning  
- An exploit framework or a drop-in replacement for Nuclei / OWASP ZAP  
- An automated bug-bounty bot or a guarantee of rewards  
- A system for accessing third-party accounts or attempting privilege escalation  

### Architecture

Clean Architecture: six backend projects, two test projects, Next.js frontend.

```
src/
  Kaan.SecurityPlatform.Domain
  Kaan.SecurityPlatform.Application
  Kaan.SecurityPlatform.Infrastructure   # EF Core, Hangfire, scanner, HackerOne, auth-scan
  Kaan.SecurityPlatform.Api
  Kaan.SecurityPlatform.ScannerWorker
  Kaan.SecurityPlatform.LabWorker
tests/
  Kaan.SecurityPlatform.UnitTests
  Kaan.SecurityPlatform.IntegrationTests
web/
  kaan-security-web                      # Next.js App Router + Tailwind
```

**Assessment modes:** `PublicPassiveAssessment` · `ApplicationSecurityCandidate` · `IsolatedSecurityLab`

### Requirements

- .NET 10 SDK (`10.0.301+`)
- Node.js 20+ / npm 10+
- SQL Server 2019+, LocalDB (Windows), or Docker
- Docker Desktop (optional)

### Getting started

```powershell
dotnet restore
dotnet build

dotnet ef database update `
  --project src/Kaan.SecurityPlatform.Infrastructure `
  --startup-project src/Kaan.SecurityPlatform.Api

dotnet run --project src/Kaan.SecurityPlatform.Api
# Swagger: http://localhost:5089/swagger

dotnet run --project src/Kaan.SecurityPlatform.ScannerWorker

cd web/kaan-security-web
npm install
npm run dev
# UI: http://localhost:3000
```

**Docker:**

```powershell
copy .env.example .env
# Edit .env — set strong JWT_SIGNING_KEY and DB password
docker compose up -d --build
```

### Main UI routes

| Route | Purpose |
|-------|---------|
| `/hackerone/targets` | Targets, batch/single ASC, “what was scanned”, reports, authenticated scan |
| `/hackerone/candidates` | Candidate findings |
| `/hackerone/report-builder` | Evidence-based report drafts |
| `/domains` | Domain management & verification |
| `/scans` | Scan jobs |
| `/admin/lab` | Controlled lab (SystemAdmin) |

### Security boundaries (short)

- ASC requires domain verification  
- Only `http` / `https`; SSRF blocks localhost, private nets, cloud metadata  
- New users stay `Pending` until SystemAdmin approval  
- Auth scans are GET-focused with rate limits, blocked paths, and a secret vault  
- Path existence alone is never a vulnerability  

Details: [docs/security-boundaries.md](docs/security-boundaries.md)

### Secrets & development data

Use `.env.example` / User Secrets / environment variables for JWT keys and DB passwords.  
**Do not commit** `.env`, `.env.local`, tokens, session cookies, or production connection strings.

### Testing

```powershell
dotnet test Kaan.SecurityPlatform.slnx

cd web/kaan-security-web
npm run typecheck
npm run build
```

### Documentation

- [docs/urun-amaci.md](docs/urun-amaci.md)  
- [docs/architecture.md](docs/architecture.md)  
- [docs/security-boundaries.md](docs/security-boundaries.md)  
- [docs/scanner-design.md](docs/scanner-design.md)  
- [docs/domain-verification.md](docs/domain-verification.md)  
- [docs/roadmap.md](docs/roadmap.md)  

### Responsible use

For **authorized** security testing, defensive research, and education only. Assess only systems you own or have **explicit written permission** to test. You must comply with applicable laws and program policies.

### License

No license is granted unless a `LICENSE` file is included. All rights reserved by default.

---

<a id="turkce"></a>

## Türkçe

Yetkili hedefler üzerinde **güvenli, savunma amaçlı** web güvenlik değerlendirmesi yapan SaaS platformu.

Sistem bir genel saldırı aracı değildir. Amaç: doğrulanmış domainlerde pasif / aday tespiti yapmak, bulguları sınıflandırmak, Türkçe açıklamak, düzeltme önermek, yeniden test etmek ve (isteğe bağlı) HackerOne tarzı bug bounty iş akışını desteklemektir.

> **Önemli:** Brute force, exploit payload, kimlik bilgisi stuffing, yetkisiz hedef tarama ve otomatik HackerOne gönderimi yoktur. Yol varlığı (`/admin` gibi) tek başına zafiyet sayılmaz.

### Ne işe yarar?

| Alan | Ne sağlar? |
|------|------------|
| **Pasif güvenlik taraması** | HTTPS, TLS, security headers, cookie, CORS, mixed content, hata sızıntısı, well-known |
| **ASC** | Güvenli aday motorları (access-control yüzeyi, reflected input, CORS, info disclosure, takeover sinyali, JS secret, API surface, open redirect) |
| **Sensitive Surface Analyzer** | `/admin` vb. yıkıcı olmayan GET analizi; login/403 → DoNotSubmit; ayrıcalıklı sinyal → ManualReview |
| **HackerOne workspace** | Scope sync, targets, adaylar, report builder |
| **Girişli tarama** | Anonim vs girişli GET; çerez oturumu / manuel tarayıcı (Google-SSO dahil) |
| **Validation & rapor** | Demonstrated impact olmadan Submit yok; HTML/TXT güvenlik raporu |

### Ne değildir?

- Rastgele internet tarayıcısı değil  
- Nuclei / ZAP / exploit framework değil  
- Ödül garantili bug bounty botu değil  
- Üçüncü kişi hesabına erişim denemez  

### Hızlı başlangıç

Yukarıdaki **Getting started** komutlarının aynısıdır. Ortam değişkenleri için `.env.example` dosyasını kopyalayıp güçlü değerler verin.

### Sorumlu kullanım

Yalnızca sahip olduğunuz veya **yazılı yetkiniz olan** hedeflerde kullanın.

---

**Repo:** [github.com/kaankocdag/Security](https://github.com/kaankocdag/Security)
