import { requireSession, isSystemAdmin } from '@/lib/session';
import { redirect } from 'next/navigation';
import { apiFetch } from '@/lib/api';
import { ProgramsClient } from './programs-client';

export interface Program {
  id: string;
  policyKey: string;
  name: string;
  handle: string;
  platform: string;
  openReportUrl?: string | null;
  isEnabled: boolean;
  lastSyncedAt?: string | null;
  offersBounties?: boolean;
  currency?: string | null;
  submissionState?: string | null;
  openScope?: boolean;
  state?: string | null;
  rules: {
    id: string;
    policyCategory: string;
    recommendationWhenDemonstrated: string;
    recommendationWhenNotDemonstrated: string;
    notes?: string | null;
  }[];
}

export default async function ProgramsPage() {
  const { accessToken, user } = await requireSession();
  if (!isSystemAdmin(user)) redirect('/dashboard');

  let programs: Program[] = [];
  try {
    programs = await apiFetch<Program[]>('/api/hackerone/programs', { accessToken, serverSide: true });
  } catch {
    programs = [];
  }

  return (
    <div className="space-y-4">
      <ProgramsClient />
      <ul className="space-y-3">
        {programs.map((p) => (
          <li key={p.id} className="rounded-lg border border-slate-200 bg-white/80 p-4">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <div>
                <div className="font-semibold text-slate-900">
                  {p.name} <span className="text-sm font-normal text-slate-500">({p.handle})</span>
                </div>
                <div className="text-xs text-slate-500">
                  {p.policyKey} · {p.isEnabled ? 'enabled' : 'disabled'}
                  {p.lastSyncedAt ? ` · synced ${new Date(p.lastSyncedAt).toLocaleString('tr-TR')}` : ''}
                </div>
                <div className="mt-1 text-xs text-slate-600">
                  {p.offersBounties ? (
                    <span className="font-medium text-emerald-700">
                      Bounty ödüyor{p.currency ? ` (${p.currency})` : ''}
                    </span>
                  ) : (
                    <span className="text-slate-500">Bounty yok / VDP</span>
                  )}
                  {p.submissionState ? ` · submission: ${p.submissionState}` : ''}
                  {p.state ? ` · state: ${p.state}` : ''}
                  {p.openScope ? ' · open scope' : ''}
                </div>
              </div>
              {p.openReportUrl && (
                <a
                  href={p.openReportUrl}
                  target="_blank"
                  rel="noreferrer"
                  className="text-sm text-[color:var(--color-brand-700)] underline"
                >
                  Open
                </a>
              )}
            </div>
            <ul className="mt-3 space-y-1 text-xs text-slate-600">
              {p.rules.map((r) => (
                <li key={r.id}>
                  {r.policyCategory}: demonstrated→{r.recommendationWhenDemonstrated}, else→
                  {r.recommendationWhenNotDemonstrated}
                </li>
              ))}
            </ul>
          </li>
        ))}
      </ul>
    </div>
  );
}
