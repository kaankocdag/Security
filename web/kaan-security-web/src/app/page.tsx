import Image from 'next/image';
import Link from 'next/link';

export default function LandingPage() {
  return (
    <main className="bg-[color:var(--color-surface)] text-[color:var(--color-ink)]">
      {/* Hero — tek kompozisyon, marka önde, full-bleed görsel */}
      <section className="relative min-h-[100svh] overflow-hidden">
        <div className="absolute inset-0">
          <Image
            src="https://images.unsplash.com/photo-1558494949-ef010cbdcc31?auto=format&fit=crop&w=2400&q=80"
            alt="Veri merkezi koridoru — sürekli izleme atmosferi"
            fill
            priority
            className="landing-hero-media object-cover"
            sizes="100vw"
          />
          <div className="absolute inset-0 bg-gradient-to-t from-[color:var(--color-ink)] via-[color:var(--color-ink)]/75 to-[color:var(--color-ink)]/35" />
          <div className="absolute inset-0 bg-[radial-gradient(ellipse_at_20%_80%,rgba(31,122,106,0.35),transparent_55%)]" />
        </div>

        <div className="relative z-10 mx-auto flex min-h-[100svh] max-w-6xl flex-col justify-end px-6 pb-16 pt-24 md:pb-24 md:pt-28">
          <p className="landing-rise font-display text-[clamp(2.75rem,10vw,6.5rem)] font-semibold leading-[0.92] tracking-tight text-white">
            Kaan Security
          </p>
          <div className="landing-rule mt-4 h-px w-24 bg-[color:var(--color-accent)] md:w-36" />

          <h1 className="landing-rise landing-rise-delay-1 mt-6 max-w-2xl font-display text-[clamp(1.5rem,3.6vw,2.35rem)] font-medium leading-snug text-white/95">
            Firmalar için web güvenlik doktoru.
          </h1>
          <p className="landing-rise landing-rise-delay-2 mt-4 max-w-xl text-base leading-relaxed text-white/75 md:text-lg">
            Pasif kontrol, Türkçe rapor ve yeniden test — saldırı değil, teşhis.
          </p>

          <div className="landing-rise landing-rise-delay-3 mt-8 flex flex-wrap gap-3">
            <Link
              href="/register"
              className="inline-flex items-center justify-center rounded-md bg-[color:var(--color-accent)] px-6 py-3 text-sm font-semibold text-white transition duration-300 hover:brightness-110"
            >
              Üye ol
            </Link>
            <Link
              href="/login"
              className="inline-flex items-center justify-center rounded-md border border-white/35 bg-white/5 px-6 py-3 text-sm font-semibold text-white backdrop-blur-sm transition duration-300 hover:bg-white/15"
            >
              Giriş yap
            </Link>
          </div>
        </div>
      </section>

      {/* Tek iş: nasıl çalışır */}
      <section className="border-t border-[color:var(--color-border-subtle)] bg-white px-6 py-20 md:py-28">
        <div className="mx-auto max-w-6xl">
          <p className="font-display text-sm font-semibold uppercase tracking-[0.2em] text-[color:var(--color-brand-600)]">
            Nasıl çalışır
          </p>
          <h2 className="mt-3 max-w-2xl font-display text-3xl font-semibold tracking-tight text-[color:var(--color-ink)] md:text-4xl">
            Doğrula, ölç, düzelt, yeniden bak.
          </h2>
          <p className="mt-4 max-w-2xl text-[color:var(--color-ink-soft)]">
            Domain sahipliği kanıtlanmadan tarama başlamaz. Kontroller yalnızca okuma amaçlıdır.
          </p>

          <ol className="mt-14 grid gap-10 md:grid-cols-3 md:gap-12">
            <Step
              n="01"
              title="Domain doğrula"
              text="DNS, HTML dosya veya meta etiket ile alan adınızın size ait olduğunu kanıtlayın."
            />
            <Step
              n="02"
              title="Pasif tarama"
              text="HTTPS, sertifika, güvenlik başlıkları, cookie ve bilgi sızıntısı kontrolleri çalışır."
            />
            <Step
              n="03"
              title="Türkçe rapor"
              text="Yönetici özeti ve teknik detay birlikte gelir; düzeltme sonrası yeniden test edersiniz."
            />
          </ol>
        </div>
      </section>

      {/* Tek iş: sınırlar / güven */}
      <section className="relative overflow-hidden px-6 py-20 md:py-28">
        <div className="absolute inset-0 bg-[linear-gradient(135deg,#0b2e28_0%,#12181f_55%,#1a2430_100%)]" />
        <div className="absolute -right-24 top-0 h-72 w-72 rounded-full bg-[color:var(--color-brand-500)]/20 blur-3xl" />
        <div className="relative z-10 mx-auto max-w-6xl">
          <h2 className="max-w-xl font-display text-3xl font-semibold tracking-tight text-white md:text-4xl">
            Saldırı aracı değil. Teşhis platformu.
          </h2>
          <p className="mt-4 max-w-xl text-white/70">
            Exploit, brute force veya form gönderimi yok. Admin onaylı üyelik ve firma izolasyonu ile
            yalnızca yetkili ekipler çalışır.
          </p>
          <Link
            href="/register"
            className="mt-10 inline-flex items-center justify-center rounded-md bg-white px-6 py-3 text-sm font-semibold text-[color:var(--color-ink)] transition duration-300 hover:bg-[color:var(--color-brand-50)]"
          >
            Başvuruyu başlat
          </Link>
        </div>
      </section>

      <footer className="border-t border-[color:var(--color-border-subtle)] bg-white px-6 py-8">
        <div className="mx-auto flex max-w-6xl flex-col gap-2 text-sm text-[color:var(--color-ink-soft)] md:flex-row md:items-center md:justify-between">
          <span className="font-display font-semibold text-[color:var(--color-ink)]">
            Kaan Security Platform
          </span>
          <span>Pasif güvenlik doktorluğu · © {new Date().getFullYear()}</span>
        </div>
      </footer>
    </main>
  );
}

function Step({ n, title, text }: { n: string; title: string; text: string }) {
  return (
    <li className="border-t border-[color:var(--color-border-subtle)] pt-6">
      <div className="font-display text-xs font-semibold tracking-[0.18em] text-[color:var(--color-accent)]">
        {n}
      </div>
      <h3 className="mt-3 font-display text-xl font-semibold text-[color:var(--color-ink)]">
        {title}
      </h3>
      <p className="mt-2 text-sm leading-relaxed text-[color:var(--color-ink-soft)]">{text}</p>
    </li>
  );
}
