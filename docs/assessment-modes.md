# Değerlendirme Modları

Platformda **üç mod** vardır.

## 1. PublicPassiveAssessment

| Kural | Değer |
| --- | --- |
| Hedef | İnternet üzerindeki kamuya açık web siteleri |
| Firma izni / domain doğrulama | **Gerekmez** |
| İstekler | Yalnızca güvenli **GET** ve **HEAD** |
| Kontroller | SSL/TLS, sertifika, güvenlik başlıkları, cookie, redirect, security.txt, kamuya açık bilgi sızıntısı göstergeleri |
| Yasak | Exploit, payload, login denemesi, form gönderimi, brute force, dizin taraması, veri değiştiren işlem |
| Başlatma | **SystemAdmin** |
| SSRF | Private IP, redirect ve DNS güvenliği **zorunlu** |

API: `POST /api/scans` (`assessmentMode` / `assessmentModeName` = `PublicPassiveAssessment`)

## 2. IsolatedSecurityLab

| Kural | Değer |
| --- | --- |
| Hedef | Yalnızca SystemAdmin’in allowlist’e **eklediği** hostname’ler |
| Senaryo | Yalnızca önceden tanımlı `ScenarioKey` |
| Kullanıcı girişi | URL / IP / port / payload / script / shell **yasak** |
| Ağ | Lab internet çıkışı **açık** (`Lab:AllowEgress=true`) |
| Başlatma | **SystemAdmin** + step-up parola + onay ifadesi |

API: `api/admin/lab/*`

## 3. AuthorizedExternalAssessment

| Kural | Değer |
| --- | --- |
| Hedef | Platformda kayıtlı ve **doğrulanmış** (`IsVerified`) domain |
| Firma / sahiplik | Domain doğrulama **zorunlu** |
| Başlatma | **SystemAdmin** |
| İstekler | Mevcut güvenlik kontrol paketi (GET/HEAD); hedefe gerçek istek gider |
| Yasak | Serbest exploit, özel payload yükleme, dosya yükleme/çalıştırma, brute force, form ile tahrip |

API: `POST /api/scans` (`assessmentMode` / `assessmentModeName` = `AuthorizedExternalAssessment`)

UI: Bulgu detayında SystemAdmin paneli (doğrulanmış domain gerekir).
