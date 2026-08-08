# Güvenlik Sınırları

Kaan.SecurityPlatform bir "web güvenlik doktoru"dur; genel amaçlı **saldırı aracı değildir**.

Desteklenen modlar: **PublicPassiveAssessment**, **IsolatedSecurityLab**, **AuthorizedExternalAssessment**.

## İzin verilen davranışlar

- HEAD/GET ile HTTPS/HTTP yanıtlarını okuma (PublicPassive / AuthorizedExternal).
- Sertifika, güvenlik başlıkları, cookie, security.txt vb. kontroller.
- SSRF denetimi (`ITargetSafetyValidator`).
- IsolatedSecurityLab: allowlist hostname + imzalı senaryo; lab egress açık (deneme).
- AuthorizedExternalAssessment: yalnızca **doğrulanmış** domain + SystemAdmin.

## Kesin olarak yapılmayanlar

- Serbest exploit, özel payload, brute force, form submit.
- POST/PUT/DELETE ile durum değiştiren istekler.
- Localhost, private IP, cloud metadata, `file://`, `ftp://`, `gopher://`, `ldap://`.
- Lab’da serbest URL / IP / port / shell / script girişi.
- Doğrulanmamış domainde AuthorizedExternalAssessment.

## SSRF Koruması

`ITargetSafetyValidator`:

- Yalnızca `http` / `https`.
- Reddedilen host’lar: localhost, metadata, link-local vb.
- Private / rezerve IP aralıkları DNS sonrası yeniden kontrol.

## Yetki

| İşlem | Rol |
| --- | --- |
| PublicPassiveAssessment başlat | SystemAdmin |
| AuthorizedExternalAssessment başlat | SystemAdmin + doğrulanmış domain |
| IsolatedSecurityLab başlat | SystemAdmin (+ step-up + onay) |
| Üye/firma onayı, KB | SystemAdmin |
