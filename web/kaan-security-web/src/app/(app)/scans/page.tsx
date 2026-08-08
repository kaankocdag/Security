import Link from 'next/link';
import { apiFetch } from '@/lib/api';
import { requireSession } from '@/lib/session';
import { formatDateTr } from '@/lib/utils';

interface ScanListItem {
  id: string;
  domainHostName: string;
  scanType: string;
  status: string;
  progressPercentage: number;
  score: number | null;
  startedAt: string | null;
  completedAt: string | null;
}

export default async function ScansPage() {
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
  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-slate-900">Taramalar</h1>
        <Link
          href="/site-test"
          className="rounded-md bg-[color:var(--color-brand-600)] px-3 py-2 text-sm font-semibold text-white hover:bg-[color:var(--color-brand-700)]"
        >
          Yeni tarama
        </Link>
      </div>
      <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-left text-xs uppercase text-slate-500">
            <tr>
              <th className="px-4 py-2">Domain</th>
              <th className="px-4 py-2">Tür</th>
              <th className="px-4 py-2">Durum</th>
              <th className="px-4 py-2">İlerleme</th>
              <th className="px-4 py-2">Puan</th>
              <th className="px-4 py-2">Başlangıç</th>
              <th className="px-4 py-2">Bitiş</th>
              <th className="px-4 py-2"></th>
            </tr>
          </thead>
          <tbody>
            {scans.length === 0 ? (
              <tr>
                <td colSpan={8} className="px-4 py-6 text-center text-slate-500">
                  Henüz tarama yok.
                </td>
              </tr>
            ) : (
              scans.map((s) => (
                <tr key={s.id} className="border-t border-slate-100 hover:bg-slate-50">
                  <td className="px-4 py-2 font-medium text-slate-800">{s.domainHostName}</td>
                  <td className="px-4 py-2">{s.scanType}</td>
                  <td className="px-4 py-2">{s.status}</td>
                  <td className="px-4 py-2">%{s.progressPercentage}</td>
                  <td className="px-4 py-2">{s.score !== null ? s.score + '/100' : '—'}</td>
                  <td className="px-4 py-2 text-slate-600">{formatDateTr(s.startedAt)}</td>
                  <td className="px-4 py-2 text-slate-600">{formatDateTr(s.completedAt)}</td>
                  <td className="px-4 py-2 text-right">
                    <Link
                      href={`/scans/${s.id}`}
                      className="text-xs font-semibold text-[color:var(--color-brand-700)] hover:underline"
                    >
                      Detay
                    </Link>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
