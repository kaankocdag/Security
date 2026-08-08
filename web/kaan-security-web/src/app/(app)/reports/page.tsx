import Link from 'next/link';
import { apiFetch } from '@/lib/api';
import { requireSession } from '@/lib/session';
import { formatDateTr } from '@/lib/utils';

interface ScanListItem {
  id: string;
  domainHostName: string;
  status: string;
  score: number | null;
  completedAt: string | null;
}

function ReportLinks({ scanId }: { scanId: string }) {
  const base = `/api/backend/api/reports/${scanId}`;
  return (
    <div className="flex flex-col gap-1.5">
      <div className="flex flex-wrap gap-1.5">
        <a
          href={`${base}?format=html&lang=tr`}
          target="_blank"
          rel="noreferrer"
          className="rounded-md border border-slate-200 bg-white px-2.5 py-1 text-xs font-semibold text-slate-700 hover:bg-slate-50"
        >
          HTML · TR
        </a>
        <a
          href={`${base}?format=html&lang=en`}
          target="_blank"
          rel="noreferrer"
          className="rounded-md border border-slate-200 bg-white px-2.5 py-1 text-xs font-semibold text-slate-700 hover:bg-slate-50"
        >
          HTML · EN
        </a>
      </div>
      <div className="flex flex-wrap gap-1.5">
        <a
          href={`${base}?format=txt&lang=tr`}
          target="_blank"
          rel="noreferrer"
          className="rounded-md border border-slate-800 bg-slate-800 px-2.5 py-1 text-xs font-semibold text-white hover:bg-slate-900"
        >
          TXT firmaya · TR
        </a>
        <a
          href={`${base}?format=txt&lang=en`}
          target="_blank"
          rel="noreferrer"
          className="rounded-md border border-slate-800 bg-slate-800 px-2.5 py-1 text-xs font-semibold text-white hover:bg-slate-900"
        >
          TXT vendor · EN
        </a>
      </div>
    </div>
  );
}

export default async function ReportsPage() {
  const { accessToken } = await requireSession();
  let scans: ScanListItem[] = [];
  try {
    scans = await apiFetch<ScanListItem[]>('/api/scans', {
      accessToken,
      serverSide: true
    });
  } catch {
    scans = [];
  }
  const finished = scans.filter((s) => s.status === 'Completed');
  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-2xl font-bold text-slate-900">Raporlar</h1>
        <p className="mt-1 text-sm text-slate-600">
          Tamamlanan taramalar için HTML detay ve firmaya iletilebilir TXT raporları — Türkçe (TR) ve
          İngilizce (EN).
        </p>
      </div>
      <div className="rounded-xl border border-slate-200 bg-slate-50 px-4 py-3 text-xs text-slate-600">
        <strong>TXT:</strong> Amazon / CDN / hosting destek taleplerine yapıştırılabilir uzun metin.
        EN sürümünde başlıklar ve satıcı talep şablonu İngilizce; bulgu gövdesi tarayıcı çıktısıdır.
      </div>
      <div className="grid gap-3 md:grid-cols-2">
        {finished.length === 0 ? (
          <div className="rounded-md border border-dashed border-slate-200 p-6 text-sm text-slate-500">
            Henüz tamamlanmış tarama yok.
          </div>
        ) : (
          finished.map((s) => (
            <div
              key={s.id}
              className="flex flex-col gap-3 rounded-2xl border border-slate-200 bg-white p-4 shadow-sm sm:flex-row sm:items-start sm:justify-between"
            >
              <div>
                <div className="font-semibold text-slate-800">{s.domainHostName}</div>
                <div className="text-xs text-slate-500">
                  {formatDateTr(s.completedAt)} · Puan:{' '}
                  {s.score !== null ? s.score + '/100' : '—'}
                </div>
                <div className="mt-3">
                  <Link
                    href={`/scans/${s.id}`}
                    className="text-xs font-semibold text-[color:var(--color-brand-700)] hover:underline"
                  >
                    Tarama detayı →
                  </Link>
                </div>
              </div>
              <ReportLinks scanId={s.id} />
            </div>
          ))
        )}
      </div>
    </div>
  );
}
