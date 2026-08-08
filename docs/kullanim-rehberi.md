# Kullanım rehberi

Uygulama içinde: üst bardaki **Rehber** veya menüdeki **Kullanım rehberi** (`/help`).
Menü satırlarındaki **i** ikonuna tıklayınca kısa açıklama açılır.

## SystemAdmin

1. **API’yi başlatın** (seed admin’i Demo firmaya bağlar).
2. `admin@kaansecurity.local` / `Kaan!Admin2026#` ile giriş → **çıkış → tekrar giriş** (firma claim’i için).
3. **Üye Onayları**: bekleyen kullanıcı/firmaları onaylayın.
4. **Public Passive Assessment**: proje + domain → pasif GET/HEAD tarama.
5. **Isolated Security Lab**: hedef hostname ekle → step-up parola → onay ifadesi → senaryo.
6. Sonuçlar: **Taramalar**, **Bulgular**, **Raporlar**.

## Firma kullanıcısı

1. Kayıt olun → admin onayını bekleyin.
2. Onay sonrası projeleri/domainleri görün; bulgu ve raporları okuyun.
3. Pasif tarama ve lab başlatma SystemAdmin’e aittir.

## Hata: “firmaya bağlı olmalısınız”

SystemAdmin JWT’sinde `CompanyId` yok demektir. API’yi yeniden başlatın (seed), sonra çıkış/giriş yapın.
Platformda hiç firma yoksa önce bir firma kaydı onaylayın.

## Tarama “Queued”de kalıyor

Development’ta Hangfire bağlantısı boşsa Api kendi içinde tarama işler (MemoryStorage).
**API’yi yeniden başlatın**; açılışta Queued kalan işler otomatik yeniden kuyruğa alınır.
Üretimde `ConnectionStrings:Hangfire` + `ScannerWorker` çalışmalıdır.
