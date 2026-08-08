import { requireSession } from '@/lib/session';
import { apiFetch } from '@/lib/api';
import Link from 'next/link';
import { Activity, FileText, Radar, ShieldAlert } from 'lucide-react';
import { HelpTip } from '@/components/ui/help-tip';

interface DashboardProject {
  id: string;
  name: string;
  status: string;
  openFindingCount: number;
  domainCount: number;
}

interface DashboardScan {
  id: string;
  domainHostName: string;
  status: string;
  progressPercentage: number;
  score: number | null;
  completedAt: string | null;
}

export default async function DashboardPage() {
  const { accessToken, user } = await requireSession();

  if (user.membershipStatus !== 'Approved') {
    return <PendingApprovalView />;
  }

  let projects: DashboardProject[] = [];
  let scans: DashboardScan[] = [];
  try {
    projects = await apiFetch<DashboardProject[]>('/api/projects', {
      accessToken,
      serverSide: true
    });
  } catch {
    projects = [];
  }
  try {
    scans = await apiFetch<DashboardScan[]>('/api/scans', {
      accessToken,
      serverSide: true
    });
  } catch {
    scans = [];
  }

  const scored = scans.filter((s) => s.score !== null);
  const avgScore =
    scored.reduce((a, b) => a + (b.score ?? 0), 0) / Math.max(1, scored.length || 1);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="flex items-center text-2xl font-bold text-slate-900">
          Genel Bakış
          <HelpTip text="Firmanızın güvenlik nabzı: projeler, skor ve son taramalar." />
        </h1>
        <p className="mt-1 text-sm text-slate-600">
          Firmanın güvenlik doktoru sizin için nabız tutuyor.{' '}
          <Link href="/help" className="text-[color:var(--color-brand-700)] underline">
            Nasıl kullanılır?
          </Link>
        </p>
      </div>

      <div className="grid grid-cols-1 gap-4 md:grid-cols-4">
        <MetricCard
          label="Aktif Proje"
          tip="Kayıtlı güvenlik proje sayısı."
          value={String(projects.length)}
          icon={<Radar size={18} />}
        />
        <MetricCard
          label="Ortalama Skor"
          tip="Tamamlanan taramaların ortalama güvenlik skoru (0–100)."
          value={scored.length ? Math.round(avgScore || 0) + '/100' : '—'}
          icon={<Activity size={18} />}
        />
        <MetricCard
          label="Son Taramalar"
          tip="Listelenen pasif tarama işlerinin sayısı."
          value={String(scans.length)}
          icon={<FileText size={18} />}
        />
        <MetricCard
          label="Yüksek Öncelikli"
          tip="Skoru 70’in altında kalan taramalar — önce bunlara bakın."
          value={String(scans.filter((s) => (s.score ?? 100) < 70).length)}
          icon={<ShieldAlert size={18} />}
        />
      </div>

      <section className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        <div className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
          <div className="mb-3 flex items-center justify-between">
            <h2 className="text-sm font-semibold text-slate-800">Son Taramalar</h2>
            <Link href="/scans" className="text-xs text-[color:var(--color-brand-600)]">
              Tümünü gör
            </Link>
          </div>
          {scans.length === 0 ? (
            <EmptyRow message="Henüz tarama başlatılmamış. 'Sitemi Test Et' ile başlayın." />
          ) : (
            <ul className="space-y-2">
              {scans.slice(0, 8).map((s) => (
                <li
                  key={s.id}
                  className="flex items-center justify-between rounded-md border border-slate-100 bg-slate-50 px-3 py-2 text-sm"
                >
                  <div>
                    <div className="font-medium text-slate-800">{s.domainHostName}</div>
                    <div className="text-[11px] text-slate-500">
                      {s.status} · %{s.progressPercentage}
                    </div>
                  </div>
                  <div className="text-right">
                    <div className="text-sm font-semibold text-slate-800">
                      {s.score !== null ? s.score + '/100' : '—'}
                    </div>
                    <Link
                      href={`/scans/${s.id}`}
                      className="text-[11px] text-[color:var(--color-brand-600)]"
                    >
                      Detay
                    </Link>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </div>

        <div className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
          <div className="mb-3 flex items-center justify-between">
            <h2 className="text-sm font-semibold text-slate-800">Projeleriniz</h2>
            <Link href="/projects" className="text-xs text-[color:var(--color-brand-600)]">
              Yönet
            </Link>
          </div>
          {projects.length === 0 ? (
            <EmptyRow message="Henüz proje yok. Sitemi Test Et sihirbazından ilk projeyi oluşturabilirsiniz." />
          ) : (
            <ul className="space-y-2">
              {projects.slice(0, 8).map((p) => (
                <li
                  key={p.id}
                  className="flex items-center justify-between rounded-md border border-slate-100 bg-slate-50 px-3 py-2 text-sm"
                >
                  <div>
                    <div className="font-medium text-slate-800">{p.name}</div>
                    <div className="text-[11px] text-slate-500">
                      {p.domainCount} domain · {p.status}
                    </div>
                  </div>
                  <div className="text-sm font-semibold text-slate-800">
                    {p.openFindingCount} bulgu
                  </div>
                </li>
              ))}
            </ul>
          )}
        </div>
      </section>
    </div>
  );
}

function MetricCard({
  label,
  tip,
  value,
  icon
}: {
  label: string;
  tip: string;
  value: string;
  icon: React.ReactNode;
}) {
  return (
    <div className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
      <div className="flex items-center gap-2 text-xs text-slate-500">
        {icon}
        <span className="inline-flex items-center">
          {label}
          <HelpTip text={tip} />
        </span>
      </div>
      <div className="mt-2 text-2xl font-bold text-slate-900">{value}</div>
    </div>
  );
}

function EmptyRow({ message }: { message: string }) {
  return <div className="rounded-md border border-dashed border-slate-200 p-4 text-xs text-slate-500">{message}</div>;
}

function PendingApprovalView() {
  return (
    <div className="mx-auto max-w-2xl rounded-2xl border border-amber-200 bg-amber-50 p-6 text-amber-900">
      <h1 className="text-xl font-bold">Hesabınız onay bekliyor</h1>
      <p className="mt-2 text-sm">
        Kaan Security ekibi başvurunuzu inceliyor. Onay verildiğinde e-posta ile bilgilendirileceksiniz.
        Bu süre içinde sitenizi tarayamaz, proje oluşturamazsınız.
      </p>
      <p className="mt-2 text-xs">
        Aciliyet için: <a className="underline" href="mailto:onay@kaansecurity.local">onay@kaansecurity.local</a>
      </p>
    </div>
  );
}
