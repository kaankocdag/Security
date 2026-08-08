# Zafiyet Bilgi Bankası

Kaan Security Platform içindeki bilgi bankası, kullanıcıların bulgularını anlamalarını ve düzeltebilmelerini sağlayan Türkçe içerik havuzudur. Kullanıcı tarafından paylaşılan 60+ Instagram görsel karesi bu bankaya "makale + medya asset" olarak yüklenir.

## Veri Modeli

- `KnowledgeCategory` — kategori ağacı (ör. "Güvenlik Başlıkları", "Injection", "Kimlik Doğrulama").
- `KnowledgeArticle` — Slug, başlık, özet, Markdown gövde, CWE/OWASP/CVE eşlemesi, zorluk seviyesi, tahmini okuma süresi, tag'ler, kaynak atfı.
- `KnowledgeMediaAsset` — Bir makaleye bağlı görseller, MIME türü, boyut, açıklama, gösterim sırası.
- `KnowledgeArticleReference` — Dış bağlantılar (blog, RFC, CVE detay sayfası).
- `FindingKnowledgeLink` — Bulgu ile makale arasında relevans skorlu ilişki.

## Endpoints

Genel (auth gerektirmez):

- `GET /api/knowledge/categories`
- `GET /api/knowledge/articles`
- `GET /api/knowledge/articles/{slug}`

Admin (SystemAdmin):

- `POST /api/admin/knowledge/categories` / `PUT /api/admin/knowledge/categories/{id}` / `DELETE`
- `POST /api/admin/knowledge/articles` / `PUT` / `DELETE`
- `POST /api/admin/knowledge/articles/{articleId}/media` (multipart/form-data, en fazla 20MB)
- `DELETE /api/admin/knowledge/media/{mediaId}`

## Medya depolama

`IFileStorage` soyutlaması aracılığıyla:

- Dev / Docker → `LocalFileStorage` → `wwwroot/uploads/knowledge/{yyyy}/{mm}/` (docker volume: `kaan-uploads`).
- Prod S3 → `IFileStorage` yerine `S3FileStorage` yazılabilir (`ContentType`, `Length`, `Stream` ile).

## Instagram Serisinin Yüklenmesi

`docs/knowledge-seed.md` içindeki toplu yükleme rehberini takip edin. Kısaca:

1. Görseli 1200x1200 veya orijinal boyutunda WebP/PNG olarak kaydedin.
2. Admin panelinde ilgili kategoriyi seçin, makaleyi oluşturun (başlık, özet, Markdown gövde, tag'ler).
3. Makale kaydından sonra "Görsel Yükle" bölümünden ilgili görseli seçip, altına Türkçe açıklamasını girin. Sistem otomatik olarak `KnowledgeMediaAsset` oluşturur.
4. İlgili bulgu tipi ile eşlemek için `POST /api/knowledge/findings/{findingId}/links` (v0.2 planında) ile relevans skoru girilir.
