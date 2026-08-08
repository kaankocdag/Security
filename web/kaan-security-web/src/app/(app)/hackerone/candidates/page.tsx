import { requireSession, isSystemAdmin } from '@/lib/session';
import { redirect } from 'next/navigation';
import { apiFetch } from '@/lib/api';
import Link from 'next/link';
import { BbEligibleBadge } from '@/components/findings/bb-eligible-badge';
import { CandidatesClient } from './candidates-client';

export interface Candidate {
  findingId: string;
  title: string;
  technicalSeverity: string;
  findingClass: string;
  submissionRecommendation: string;
  bugBountyEligible: boolean;
  demonstratedImpact: boolean;
  programPolicyMatch?: string | null;
  domainHostName?: string | null;
  affectedUrl?: string | null;
  fingerprint?: string | null;
  rootCauseGroupId?: string | null;
  eligibilityReason?: string | null;
  lastSeenAt: string;
}

export default async function HackerOneCandidatesPage() {
  const { accessToken, user } = await requireSession();
  if (!isSystemAdmin(user)) redirect('/dashboard');

  let items: Candidate[] = [];
  let domains: { id: string; hostName: string; isVerified: boolean }[] = [];
  try {
    [items, domains] = await Promise.all([
      apiFetch<Candidate[]>('/api/hackerone/candidates', { accessToken, serverSide: true }),
      apiFetch<{ id: string; hostName: string; isVerified: boolean }[]>('/api/domains', {
        accessToken,
        serverSide: true
      })
    ]);
  } catch {
    items = [];
    domains = [];
  }

  return (
    <div className="space-y-4">
      <p className="text-sm text-slate-600">
        Yalnızca <code>Submit</code> ve <code>ManualReview</code> önerili bulgular. Hardening /
        header çıktıları burada listelenmez.
      </p>
      <CandidatesClient initial={items} domains={domains} />
      <ul className="divide-y divide-slate-200 rounded-lg border border-slate-200 bg-white/80">
        {items.length === 0 && (
          <li className="px-4 py-6 text-sm text-slate-500">Aday bulgu yok.</li>
        )}
        {items.map((c) => (
          <li key={c.findingId} className="flex flex-wrap items-start justify-between gap-3 px-4 py-3">
            <div className="min-w-0 flex-1">
              <div className="flex flex-wrap items-center gap-2">
                <Link href={`/findings/${c.findingId}`} className="font-medium text-slate-900 hover:underline">
                  {c.title}
                </Link>
                <BbEligibleBadge eligible={c.bugBountyEligible} compact />
                <span className="rounded bg-slate-100 px-1.5 py-0.5 text-[11px] text-slate-600">
                  {c.submissionRecommendation}
                </span>
              </div>
              <div className="mt-1 text-xs text-slate-500">
                {c.domainHostName ?? '—'} · {c.technicalSeverity} · {c.findingClass}
                {c.programPolicyMatch ? ` · ${c.programPolicyMatch}` : ''}
              </div>
            </div>
            <Link
              href={`/hackerone/report-builder?findingId=${c.findingId}`}
              className="shrink-0 rounded-md bg-[color:var(--color-brand-600)] px-3 py-1.5 text-xs font-medium text-white"
            >
              Report Builder
            </Link>
          </li>
        ))}
      </ul>
    </div>
  );
}
