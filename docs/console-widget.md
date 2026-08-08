# ActivityConsole Widget

`web/kaan-security-web/src/components/activity-console/activity-console.tsx` içindeki client komponent, sağ alt köşede sabitlenmiş, taşınabilir ve küçültülebilir bir canlı olay konsolüdür.

## Özellikler

- SignalR (`@microsoft/signalr`) ile `/hubs/activity` endpoint'ine bağlanır.
- Access token, sunucu tarafı `/api/session/hub-token` endpoint'i üzerinden geçici olarak alınır. Böylece httpOnly cookie'ye dokunmadan bağlantı kurulur.
- Bağlantı durumu (idle/connecting/connected/error) başlıkta renkli LED ile gösterilir.
- Layout durumu (`x`, `y`, `minimized`, `hidden`, `activeTab`) `localStorage` üzerinden `ksp:activity-console:v1` anahtarıyla kalıcıdır.
- Sürükleme: başlık çubuğuna mousedown → global mousemove ile pozisyon güncellenir. Ekran sınırları ± sağa dayanma korunur.
- Sekmeler:
  - **Aktivitem** (herkes) → `scan.queued`, `scan.progress`, `scan.completed`, `finding.created`, `domain.verified` olayları.
  - **Sistem** (SystemAdmin) → `membership.requested`, `worker.heartbeat` gibi platform seviyesi olaylar.
- Her sekme en fazla son 200 olayı tutar (bellek koruması).
- Kapatıldığında sağ alt köşede "Konsolu aç" düğmesi bırakılır.

## Backend olay yayını

- `ActivityHub` üç grup tanımlar: `user:{userId}`, `company:{companyId}`, `role:system-admin`.
- `SignalRActivityEventPublisher`, Application katmanındaki `IActivityEventPublisher` arayüzünü uygular ve `Clients.Group(...).SendAsync(eventName, payload)` çağırır.
- Executor, scan başlatıldığında/tamamlandığında `PublishToCompanyAsync` çağırır.
- Membership onay isteği/heartbeat gibi sistem olayları için ayrı SignalR gönderimi eklenir (`role:system-admin`).

## Yeni olay ekleme

1. `IActivityEventPublisher` için yeni bir Publish metodu tanımlayın (ör. `PublishToRoleAsync`).
2. `SignalRActivityEventPublisher` içinde ilgili grup ve event ismini gönderin.
3. Frontend'te `activity-console.tsx` içindeki `eventNames` dizisine yeni event adını ekleyin ve `prettify` sözlüğüne Türkçe karşılığını yazın.

## Erişilebilirlik

- Sürükleme sırasında `body.userSelect = 'none'` ayarlanır, mouseup ile temizlenir.
- Bütün butonların `title` metni Türkçe olarak sağlanmıştır (küçült, kapat, aç).
