export interface FindingPlayDemo {
  title: string;
  warningBanner: string;
  withoutLabel: string;
  withoutBody: string;
  withLabel: string;
  withBody: string;
}

export interface FindingAttackSection {
  heading: string;
  body: string;
}

export interface FindingEducationEntry {
  fingerprint: string;
  title: string;
  knowledgeSlug?: string;
  playDemo: FindingPlayDemo;
  attackExplainer: FindingAttackSection[];
}

const catalog: FindingEducationEntry[] = [
  {
    fingerprint: 'sh.csp.missing',
    title: 'Content-Security-Policy eksik',
    knowledgeSlug: 'csp-uygulama-rehberi',
    playDemo: {
      title: 'CSP uyarı demosu',
      warningBanner:
        'Bu bir eğitim simülasyonudur. Hedef sitenize istek atılmaz, exploit veya payload çalıştırılmaz.',
      withoutLabel: 'CSP yok',
      withoutBody:
        'Tarayıcı, sayfaya enjekte edilen istenmeyen bir betiği engelleyemezdi. Örnek uyarı: “Bu sayfada üçüncü taraf bir script çalışabilirdi — oturum veya form verisi sızdırılabilir.”',
      withLabel: "CSP var (default-src 'self')",
      withBody:
        'Aynı betik Content-Security-Policy tarafından engellenirdi. Tarayıcı konsolunda “Refused to execute inline script…” benzeri bir uyarı görünür; kullanıcı verisi korunur.'
    },
    attackExplainer: [
      {
        heading: 'Ne eksik?',
        body: 'Sunucu Content-Security-Policy başlığını göndermiyor. Tarayıcı hangi script, stil ve kaynağın yüklenebileceğini bilmiyor.'
      },
      {
        heading: 'Risk nedir?',
        body: 'XSS veya enjekte edilmiş içerik durumunda tarayıcı, saldırganın script’ini meşru sayfa gibi çalıştırabilir. Bu, oturum çerezi, form verisi veya DOM üzerinden veri sızıntısına yol açabilir.'
      },
      {
        heading: 'Nasıl gerçekleşir? (eğitim)',
        body: 'Klasik senaryoda sayfaya zararlı bir script parçası eklenir (ör. yansıtılmış XSS). CSP olmadığında tarayıcı bunu çalıştırır. CSP ile yalnızca izin verilen kaynaklar çalışır; enjekte script engellenir. Platform hedefe saldırı göndermez — bu anlatım genel eğitim içindir.'
      },
      {
        heading: 'Nasıl engellenir?',
        body: "Strict-Transport-Security ile karıştırmayın: CSP, hangi içeriğin yükleneceğini kısıtlar. Başlangıç için default-src 'self'; script-src 'self' gibi sıkı bir politika kullanın; gerekirse nonce veya hash ekleyin."
      },
      {
        heading: 'Nasıl düzeltilir?',
        body: 'Tüm HTTPS yanıtlarına Content-Security-Policy ekleyin. Öneri ve örnek yapılandırma bu sayfanın “Önerilen çözüm” bölümünde yer alır.'
      }
    ]
  },
  {
    fingerprint: 'sh.hsts.missing',
    title: 'HSTS başlığı eksik',
    knowledgeSlug: 'hsts-nedir',
    playDemo: {
      title: 'HSTS uyarı demosu',
      warningBanner:
        'Eğitim simülasyonu — hedefe istek yok. Yalnızca HTTPS zorunluluğunun neden önemli olduğunu gösterir.',
      withoutLabel: 'HSTS yok',
      withoutBody:
        'Kullanıcı ilk kez http:// ile gelirse tarayıcı şifresiz bağlantıyı deneyebilir. Uyarı: “Ağdaki bir aracı, sizi sahte HTTP sayfasına yönlendirebilirdi (SSL stripping).”',
      withLabel: 'HSTS var',
      withBody:
        'Tarayıcı bir kez HTTPS gördükten sonra max-age süresi boyunca yalnızca HTTPS kullanır; HTTP denemeleri otomatik yükseltilir.'
    },
    attackExplainer: [
      {
        heading: 'Ne eksik?',
        body: 'Strict-Transport-Security başlığı yok. Tarayıcı “bu site her zaman HTTPS” kuralını öğrenemiyor.'
      },
      {
        heading: 'Risk nedir?',
        body: 'SSL stripping veya ilk HTTP ziyaretinde trafiğin şifresiz kalması riski artar; kimlik bilgileri ve çerezler açıkta iletilebilir.'
      },
      {
        heading: 'Nasıl gerçekleşir? (eğitim)',
        body: 'Kullanıcı http bağlantısına zorlanırsa veya yönlendirme kırılırsa, ara kişi şifresiz içeriği değiştirebilir. HSTS bu sınıfı büyük ölçüde kapatır. Canlı exploit yok — genel eğitim.'
      },
      {
        heading: 'Nasıl engellenir?',
        body: 'Strict-Transport-Security: max-age=63072000; includeSubDomains; preload ekleyin. Önce tüm alt alanların HTTPS desteklediğinden emin olun.'
      },
      {
        heading: 'Nasıl düzeltilir?',
        body: 'Web sunucusu veya reverse proxy’de HSTS başlığını tüm HTTPS yanıtlara ekleyin. Ayrıntı için bilgi bankası makalesine bakın.'
      }
    ]
  },
  {
    fingerprint: 'sh.clickjacking.missing',
    title: 'Clickjacking koruması eksik',
    playDemo: {
      title: 'Clickjacking uyarı demosu',
      warningBanner:
        'Eğitim simülasyonu. Hedefe iframe veya tıklama saldırısı uygulanmaz; yalnızca risk anlatılır.',
      withoutLabel: 'X-Frame-Options / frame-ancestors yok',
      withoutBody:
        'Sayfanız başka bir sitede görünmez iframe içinde açılabilirdi. Uyarı: “Kullanıcı aslında sizin butonunuza tıklıyormuş gibi kandırılabilirdi.”',
      withLabel: 'Koruma açık',
      withBody:
        "X-Frame-Options: DENY veya CSP frame-ancestors 'none' ile sayfa yabancı iframe’de yüklenmez; tıklama hilesi engellenir."
    },
    attackExplainer: [
      {
        heading: 'Ne eksik?',
        body: 'X-Frame-Options veya CSP frame-ancestors ile çerçeveleme engeli tanımlı değil.'
      },
      {
        heading: 'Risk nedir?',
        body: 'Clickjacking: kullanıcı, üstteki sahte arayüz sanırken alttaki gizli çerçevedeki gerçek butona tıklar (onay, satın alma, ayar değişikliği).'
      },
      {
        heading: 'Nasıl gerçekleşir? (eğitim)',
        body: 'Saldırgan sayfanızı şeffaf iframe ile kendi sitesine gömer. Kullanıcı görünür “Ödül kazan”a tıklarken aslında sizin “Sil” düğmesine basar. Platform bunu denemez.'
      },
      {
        heading: 'Nasıl engellenir?',
        body: "X-Frame-Options: DENY veya Content-Security-Policy: frame-ancestors 'none' / 'self'."
      },
      {
        heading: 'Nasıl düzeltilir?',
        body: 'Tüm hassas sayfalara çerçeveleme yasaklayan başlık ekleyin; çözüm önerisi bulgu kaydında listelenir.'
      }
    ]
  },
  {
    fingerprint: 'sh.nosniff.missing',
    title: 'X-Content-Type-Options eksik',
    playDemo: {
      title: 'MIME sniffing uyarı demosu',
      warningBanner:
        'Eğitim simülasyonu. Hedefe dosya yüklenmez veya çalıştırılmaz.',
      withoutLabel: 'X-Content-Type-Options yok',
      withoutBody:
        'Tarayıcı Content-Type’ı yok sayıp içeriği script gibi yorumlayabilirdi. Uyarı: “Yanlış türde sunulan bir dosya beklenmedik şekilde çalışabilirdi.”',
      withLabel: 'nosniff açık',
      withBody:
        'X-Content-Type-Options: nosniff ile tarayıcı bildirilen MIME türüne uyar; tip karışıklığına dayalı script çalıştırma riski düşer.'
    },
    attackExplainer: [
      {
        heading: 'Ne eksik?',
        body: 'X-Content-Type-Options: nosniff başlığı gönderilmiyor.'
      },
      {
        heading: 'Risk nedir?',
        body: 'MIME sniffing: tarayıcı dosyayı yanlışlıkla yürütülebilir içerik sayabilir; özellikle kullanıcı içeriği sunulan uygulamalarda risk artar.'
      },
      {
        heading: 'Nasıl gerçekleşir? (eğitim)',
        body: 'Örneğin text/plain sanılan bir yanıt, içerik “script gibi” görünürse bazı tarayıcılar onu çalıştırmayı deneyebilir. nosniff bunu engeller. Canlı saldırı yok.'
      },
      {
        heading: 'Nasıl engellenir?',
        body: 'Yanıtlara X-Content-Type-Options: nosniff ekleyin ve doğru Content-Type kullanın.'
      },
      {
        heading: 'Nasıl düzeltilir?',
        body: 'Sunucu veya CDN katmanında global header olarak nosniff tanımlayın.'
      }
    ]
  }
];

const byFingerprint = new Map(catalog.map((e) => [e.fingerprint, e]));

export function getFindingEducation(fingerprint?: string | null): FindingEducationEntry | null {
  if (!fingerprint) return null;
  return byFingerprint.get(fingerprint) ?? null;
}
