import Link from 'next/link';
import { BbEligibleBadge } from '@/components/findings/bb-eligible-badge';
import { apiFetch } from '@/lib/api';
import { requireSession } from '@/lib/session';
import { formatDateTr, severityColor } from '@/lib/utils';

interface FindingListItem {
  id: string;
  title: string;
  severity: string;
  technicalSeverity?: string;
  findingClass?: string;
  bugBountyEligible?: boolean;
  submissionRecommendation?: string;
  category: string;
  status: string;
  domainHostName?: string | null;
  scanJobId?: string | null;
  affectedUrl?: string;
  firstSeenAt: string;
  lastSeenAt: string;
}

export default async function FindingsPage() {
  const { accessToken } = await requireSession();
  let findings: FindingListItem[] = [];
  try {
    findings = await apiFetch<FindingListItem[]>('/api/findings', {
      accessToken,
      serverSide: true
    });
  } catch {
    findings = [];
  }

  const bbCount = findings.filter((f) => f.bugBountyEligible).length;

  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-2xl font-bold text-slate-900">Bulgular</h1>
        <p className="mt-1 text-xs text-slate-500">
          Yeşil <strong>$ BB</strong> rozeti: Amazon VRP / HackerOne için para kazandırabilecek aday.
        </p>
      </div>
      {bbCount > 0 && (
        <div className="flex items-center gap-3 rounded-xl border-2 border-emerald-400 bg-emerald-50 px-4 py-3 text-sm text-emerald-950">
          <span className="flex h-9 w-9 items-center justify-center rounded-full bg-emerald-500 text-base font-black text-white">
            $
          </span>
          <div>
            <div className="font-bold text-emerald-800">$$$ {bbCount} Bug Bounty adayı</div>
            <div className="text-xs text-emerald-900/80">
              Demonstrated impact + politika uygun — listeye öncelik verin.
            </div>
          </div>
        </div>
      )}
      <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-left text-xs uppercase text-slate-500">
            <tr>
              <th className="px-4 py-2">BB</th>
              <th className="px-4 py-2">Şiddet</th>
              <th className="px-4 py-2">Domain</th>
              <th className="px-4 py-2">Başlık</th>
              <th className="px-4 py-2">Sınıf</th>
              <th className="px-4 py-2">Durum</th>
              <th className="px-4 py-2">Son görülme</th>
              <th className="px-4 py-2"></th>
            </tr>
          </thead>
          <tbody>
            {findings.length === 0 ? (
              <tr>
                <td colSpan={8} className="px-4 py-6 text-center text-slate-500">
                  Henüz bulgu yok.
                </td>
              </tr>
            ) : (
              findings.map((f) => (
                <tr
                  key={f.id}
                  className={`border-t border-slate-100 hover:bg-slate-50 ${
                    f.bugBountyEligible ? 'bg-emerald-50/70' : ''
                  }`}
                >
                  <td className="px-4 py-2">
                    <BbEligibleBadge eligible={f.bugBountyEligible} compact />
                  </td>
                  <td className="px-4 py-2">
                    <span className={`rounded-full border px-2 py-0.5 text-xs ${severityColor(f.technicalSeverity ?? f.severity)}`}>
                      {f.technicalSeverity ?? f.severity}
                    </span>
                  </td>
                  <td className="px-4 py-2 font-medium text-slate-700">
                    {f.domainHostName ?? '—'}
                  </td>
                  <td className="px-4 py-2 font-medium text-slate-800">{f.title}</td>
                  <td className="px-4 py-2 text-xs text-slate-600">{f.findingClass ?? '—'}</td>
                  <td className="px-4 py-2">{f.status}</td>
                  <td className="px-4 py-2 text-slate-600">{formatDateTr(f.lastSeenAt)}</td>
                  <td className="px-4 py-2 text-right">
                    <Link
                      href={`/findings/${f.id}`}
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
