import { requireSession, isSystemAdmin } from '@/lib/session';
import Link from 'next/link';

export default async function HelpPage() {
  const { user } = await requireSession();
  const admin = isSystemAdmin(user);

  return (
    <div className="mx-auto max-w-3xl space-y-8">
      <div>
        <h1 className="text-2xl font-bold text-slate-900">Kullanım rehberi</h1>
        <p className="mt-1 text-sm text-slate-600">
          Menü ve butonların yanındaki <span className="font-semibold">i</span> ikonlarına
          tıklayarak kısa açıklama görebilirsiniz. Bu sayfa özet akışı gösterir.
        </p>
      </div>

      {admin ? <AdminGuide /> : <UserGuide />}

      <section className="rounded-lg border border-slate-200 bg-white p-5 text-sm text-slate-700">
        <h2 className="text-base font-semibold text-slate-900">Üç değerlendirme modu</h2>
        <ul className="mt-3 list-disc space-y-2 pl-5">
          <li>
            <strong>PublicPassiveAssessment</strong> — Kamuya açık siteye yalnızca GET/HEAD.
            Domain doğrulama gerekmez. Sadece SystemAdmin.
          </li>
          <li>
            <strong>IsolatedSecurityLab</strong> — Allowlist’e eklediğiniz hedeflerde imzalı lab
            senaryoları. Serbest URL/payload yok. Sadece SystemAdmin.
          </li>
          <li>
            <strong>AuthorizedExternalAssessment</strong> — Doğrulanmış domainde yetkili dış
            değerlendirme. SystemAdmin. Serbest exploit/payload yok; kontrol paketi hedefe istek
            atar. Bulgu detayından başlatılır.
          </li>
        </ul>
      </section>
    </div>
  );
}

function AdminGuide() {
  return (
    <section className="space-y-4 rounded-lg border border-emerald-200 bg-emerald-50/40 p-5">
      <h2 className="text-lg font-semibold text-slate-900">SystemAdmin — nasıl kullanırım?</h2>
      <ol className="list-decimal space-y-3 pl-5 text-sm text-slate-700">
        <li>
          <strong>Üye Onayları</strong> (
          <Link href="/admin/users" className="text-[color:var(--color-brand-700)] underline">
            /admin/users
          </Link>
          ): Yeni kayıtları onaylayın veya reddedin. Onaysız kullanıcı tarama yapamaz.
        </li>
        <li>
          <strong>Public Passive Assessment</strong> (
          <Link href="/site-test" className="text-[color:var(--color-brand-700)] underline">
            /site-test
          </Link>
          ): Proje + domain ekleyip pasif taramayı başlatın. Sonuçlar{' '}
          <Link href="/scans" className="underline">
            Taramalar
          </Link>{' '}
          ve{' '}
          <Link href="/findings" className="underline">
            Bulgular
          </Link>
          ’da görünür.
        </li>
        <li>
          <strong>Isolated Security Lab</strong> (
          <Link href="/admin/lab" className="text-[color:var(--color-brand-700)] underline">
            /admin/lab
          </Link>
          ): Önce hedef hostname ekleyin → step-up parola → onay ifadesi → senaryoyu başlatın.
          Acil durdur her zaman hazır.
        </li>
        <li>
          <strong>KB Yönetimi</strong>: Bilgi bankası makalelerini düzenleyin; bulgu
          açıklamalarına bağlanır.
        </li>
        <li>
          <strong>Panel / Projeler / Domainler</strong>: Demo firma üzerinden varlıkları görün;
          seed sonrası admin Demo Teknoloji firmasına bağlıdır. Çıkış yapıp yeniden giriş yapın
          ki firma claim’i yenilensin.
        </li>
      </ol>
      <p className="rounded-md border border-amber-200 bg-amber-50 px-3 py-2 text-xs text-amber-900">
        İlk kurulumda admin@kaansecurity.local ile giriş yaptıktan sonra hâlâ “firmaya bağlı
        olmalısınız” görürseniz: API’yi yeniden başlatın, sonra çıkış → tekrar giriş yapın.
      </p>
    </section>
  );
}

function UserGuide() {
  return (
    <section className="space-y-4 rounded-lg border border-slate-200 bg-white p-5">
      <h2 className="text-lg font-semibold text-slate-900">Firma kullanıcısı — nasıl kullanırım?</h2>
      <ol className="list-decimal space-y-3 pl-5 text-sm text-slate-700">
        <li>
          Kayıt olun → SystemAdmin onaylayana kadar yalnızca panelde “onay bekliyor” görürsünüz.
        </li>
        <li>
          Onay sonrası <strong>Projeler</strong> ve <strong>Domainler</strong> ile sitelerinizi
          yönetin (pasif tarama başlatma SystemAdmin’e aittir).
        </li>
        <li>
          <strong>Taramalar / Bulgular / Raporlar</strong>: Sonuçları ve Türkçe düzeltme
          önerilerini inceleyin.
        </li>
        <li>
          <strong>Bilgi Bankası</strong>: Bulgularla ilgili eğitim içeriklerini okuyun.
        </li>
      </ol>
      <p className="text-xs text-slate-500">
        Aktif saldırı, exploit veya rastgele hedef testi bu platformda yoktur.
      </p>
    </section>
  );
}
