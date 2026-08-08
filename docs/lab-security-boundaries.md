# IsolatedSecurityLab — Güvenlik Sınırları

Bu modül **eğitim / deneme laboratuvarıdır**, genel saldırı aracı değildir.
Doğrulanmış domainlerdeki yetkili dış değerlendirme için ayrı mod: `AuthorizedExternalAssessment` (serbest payload yok).

## Desteklenen davranış

| Özellik | Durum |
| --- | --- |
| Allowlist hedef hostname (`LabTargetSite`) | Zorunlu |
| Önceden tanımlı senaryolar | Evet |
| Lab ağı internet çıkışı (`AllowEgress`) | **Açık** (deneme) |
| Step-up parola + onay + audit + acil durdur | Zorunlu |
| Timeout / CPU / bellek / pids / istek limiti | Zorunlu |

## Yasaklar

| Yasak | Gerekçe |
| --- | --- |
| Kullanıcı URL / IP / host / port girişi | Yalnızca allowlist `labTargetSiteId` + `ScenarioKey` |
| Özel payload, shell, script, dosya yükleme | İmzalı senaryo parametreleri |
| Genel exploit / brute / DDoS / credential stuffing | Sabit 10 adımlı pipeline |
| Docker socket’in lab container’a verilmesi | Socket yalnızca LabWorker host tarafında |
| Serbest payload / shell | Yasak — AuthorizedExternal ayrı modda, exploit motoru yok |

## API alanları

**Kabul:** `scenarioKey`, `confirmPhrase`, `elevationToken`, `labTargetSiteId`, `assessmentModeName=IsolatedSecurityLab`

**Yasak:** `url`, `host`, `ip`, `port`, `payload`, `command`, `script`, `file`, …

Onay ifadesi:

```text
LABORATUVAR SENARYOSUNU BASLATMAYI ONAYLIYORUM
```

## Docker

- Ağ: `kaan-lab-net-*`, `Internal = !AllowEgress` (varsayılan egress açık)
- Container: non-root, read-only FS (+ `/tmp` tmpfs), `cap_drop: ALL`, `no-new-privileges`
- Image allowlist: `kaan-lab/*`
- Compose: `docker compose --profile lab up lab-worker`
