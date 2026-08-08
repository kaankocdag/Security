import { requireSession, isSystemAdmin } from '@/lib/session';
import { redirect } from 'next/navigation';
import { apiFetch } from '@/lib/api';
import Link from 'next/link';

interface Overview {
  candidateCount: number;
  submitRecommendedCount: number;
  manualReviewCount: number;
  draftCount: number;
  readyDraftCount: number;
  submissionCount: number;
  bugBountyEligibleCount: number;
  apiEnabled: boolean;
  defaultProgramHandle?: string | null;
}

export default async function HackerOneOverviewPage() {
  const { accessToken, user } = await requireSession();
  if (!isSystemAdmin(user)) redirect('/dashboard');

  let overview: Overview | null = null;
  try {
    overview = await apiFetch<Overview>('/api/hackerone/overview', { accessToken, serverSide: true });
  } catch {
    overview = null;
  }

  const cards: { label: string; value: string | number; href: string }[] = overview
    ? [
        { label: 'Targets', value: 'Tara', href: '/hackerone/targets' },
        { label: 'Adaylar', value: overview.candidateCount, href: '/hackerone/candidates' },
        { label: 'Submit önerisi', value: overview.submitRecommendedCount, href: '/hackerone/candidates' },
        { label: 'Manual review', value: overview.manualReviewCount, href: '/hackerone/candidates' },
        { label: 'BB eligible', value: overview.bugBountyEligibleCount, href: '/hackerone/candidates' },
        { label: 'Taslaklar', value: overview.draftCount, href: '/hackerone/report-builder' },
        { label: 'Hazır taslak', value: overview.readyDraftCount, href: '/hackerone/report-builder' },
        { label: 'Gönderimler', value: overview.submissionCount, href: '/hackerone/submissions' }
      ]
    : [];

  return (
    <div className="space-y-4">
      <p className="text-sm text-slate-600">
        Akış: <Link className="underline" href="/hackerone/targets">Targets</Link> (işaretle + tara) →{' '}
        <Link className="underline" href="/hackerone/candidates">Candidates</Link> →{' '}
        <Link className="underline" href="/hackerone/report-builder">Report Builder</Link> (EN H1 / TR iç)
      </p>
      <div className="text-sm text-slate-600">
        API: {overview?.apiEnabled ? 'açık' : 'kapalı'} · Varsayılan program:{' '}
        {overview?.defaultProgramHandle ?? '—'}
      </div>
      {!overview && (
        <p className="text-sm text-amber-700">Overview yüklenemedi. API çalışıyor mu?</p>
      )}
      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        {cards.map((c) => (
          <Link
            key={c.label}
            href={c.href}
            className="rounded-lg border border-slate-200 bg-white/80 px-4 py-3 hover:border-slate-300"
          >
            <div className="text-2xl font-semibold text-slate-900">{c.value}</div>
            <div className="text-xs font-medium uppercase tracking-wide text-slate-500">{c.label}</div>
          </Link>
        ))}
      </div>
    </div>
  );
}
