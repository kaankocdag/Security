# Domain Doğrulama

Tarama başlatılabilmesi için her `DomainAsset` mutlaka doğrulanmış olmalıdır. `IDomainVerificationService` üç strateji sağlar:

## 1. DNS TXT Kaydı

- Backend `_kaan-security.<domain>` TXT kaydı arar.
- Beklenen değer: `kaan-security-verification=<TOKEN>`.
- Süreç:
  1. `POST /api/domains/{id}/verification/start` → yanıtta `token` döner.
  2. Kullanıcı DNS sağlayıcısında TXT kaydı ekler.
  3. `POST /api/domains/{id}/verification/run` → DNS lookup ile doğrulanır.

## 2. HTML Dosya

- `GET https://<domain>/.well-known/kaan-security-<TOKEN>.txt`
- İçerik dosya adındaki TOKEN ile başlamalıdır.

## 3. HTML Meta Etiketi

- Anasayfada `<meta name="kaan-security-verification" content="<TOKEN>">` etiketi aranır.
- Sadece `text/html` yanıtlarında geçerlidir.

## 4. Mock (yalnız Development)

- `appsettings.Development.json` içindeki `DomainVerification:AllowMock=true` iken aktifleşir.
- `Mock` yöntemi seçildiğinde token üretimi sonrası `run` çağrısı domain'i otomatik doğrular. Üretim ortamında devre dışı bırakılmalıdır.

## Yeniden doğrulama

Domain silinene kadar doğrulanmış kalır. Ancak SSL sertifikası ya da altyapı değişikliği sonrasında admin panelinden `Re-verify` tetiklenerek yeniden çalıştırılabilir.

## Manuel doğrulama (SystemAdmin)

`POST /api/domains/{id}/verification/manual` — body: `{ "isVerified": true|false, "note": "..." }`  
Yalnızca `RequireSystemAdmin`. DNS/HTML kontrolü yapılmaz; audit: `domain.verify.manual`.  
UI: `/domains` sayfasında **Manuel doğrula** / **Doğrulamayı kaldır**.
