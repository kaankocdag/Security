import { requireSession, isSystemAdmin } from '@/lib/session';
import { redirect } from 'next/navigation';
import { apiFetch } from '@/lib/api';
import Link from 'next/link';

interface Submission {
  id: string;
  draftId: string;
  externalReportId?: string | null;
  externalReportUrl?: string | null;
  status: string;
  errorMessage?: string | null;
  submittedAt?: string | null;
}

export default async function SubmissionsPage() {
  const { accessToken, user } = await requireSession();
  if (!isSystemAdmin(user)) redirect('/dashboard');

  let items: Submission[] = [];
  try {
    items = await apiFetch<Submission[]>('/api/hackerone/submissions', { accessToken, serverSide: true });
  } catch {
    items = [];
  }

  return (
    <ul className="divide-y divide-slate-200 rounded-lg border border-slate-200 bg-white/80">
      {items.length === 0 && (
        <li className="px-4 py-6 text-sm text-slate-500">
          Henüz API gönderimi yok. Copy/Open ile manuel raporlama kullanılabilir.
        </li>
      )}
      {items.map((s) => (
        <li key={s.id} className="px-4 py-3 text-sm">
          <div className="font-medium text-slate-900">{s.status}</div>
          <div className="text-xs text-slate-500">
            Draft:{' '}
            <Link href={`/hackerone/report-builder?draftId=${s.draftId}`} className="underline">
              {s.draftId.slice(0, 8)}…
            </Link>
            {s.submittedAt ? ` · ${new Date(s.submittedAt).toLocaleString('tr-TR')}` : ''}
          </div>
          {s.externalReportUrl && (
            <a href={s.externalReportUrl} target="_blank" rel="noreferrer" className="text-xs underline">
              {s.externalReportUrl}
            </a>
          )}
          {s.errorMessage && <div className="text-xs text-red-600">{s.errorMessage}</div>}
        </li>
      ))}
    </ul>
  );
}
