# Bilgi Bankası Seed Rehberi

Kullanıcının paylaştığı Instagram görselleri (60+ post) `SystemAdmin` yetkisi ile admin panelinden yüklenir. Görseller ürüne dahil edilmez; hukuki nedenlerle her post için kaynak atfı zorunludur.

## Ön hazırlık

1. Görselleri konu başlıklarına göre klasörleyin:
   - `security-headers/`
   - `injection/`
   - `authentication/`
   - `infrastructure/`
   - `owasp-top-10/`
   - `mobile-security/`
   - vb.
2. Her klasörde `manifest.json` oluşturun:

```json
{
  "category": "security-headers",
  "articles": [
    {
      "slug": "hsts-onemli-mi",
      "title": "HSTS Önemli mi? Kısa Cevap: Evet",
      "summary": "HTTP Strict Transport Security tarayıcıya HTTPS kilidi koyar.",
      "sourceAttribution": "@kaan.security Instagram — 12 Şubat 2026",
      "sourceUrl": "https://instagram.com/p/ABC123/",
      "tags": ["hsts", "tls", "https"],
      "difficulty": "Beginner",
      "readMinutes": 3,
      "images": ["hsts-01.png", "hsts-02.png"]
    }
  ]
}
```

## Toplu yükleme adımları

1. `admin@kaansecurity.local` ile giriş yapın.
2. `/admin/knowledge` sayfasına gidin.
3. Sırasıyla:
   - Kategoriyi ekleyin (yalnızca ilk seferde).
   - Makaleyi oluşturun. Markdown gövdesinde şu şablonu kullanın:

     ```markdown
     ## Nedir?

     Kısa bir Türkçe tanım.

     ## Örnek

     ```bash
     ...
     ```

     ## Neden Önemli?

     ...

     ## Nasıl Çözerim?

     - Adım 1
     - Adım 2

     > Kaynak: {sourceAttribution} · {sourceUrl}
     ```
   - Makale oluşturulduktan sonra alt paneldeki "Görsel Yükle" adımında her görseli ekleyin, altına Türkçe açıklama yazın.
4. `PublishedAt` alanı otomatik olarak yayına alma anına eşitlenir.

## Öneriler

- Instagram karesi 1080x1080'dir; yükleme sırasında orijinal boyutu korumak SEO açısından yeterlidir.
- 4MB üzerine çıkan görselleri `imagemagick`/`squoosh` ile WebP'ye dönüştürüp yükleyin.
- Bir makaleye 4-6 görsel yüklemekten kaçının; okuyucu deneyimi bozulur. Uzun anlatımlar için ayrı makaleler açın.
- CWE/OWASP/CVE alanları doldurulduğunda otomatik olarak bulgu ↔ makale eşlemesi kolaylaşır (`FindingKnowledgeLink`).
