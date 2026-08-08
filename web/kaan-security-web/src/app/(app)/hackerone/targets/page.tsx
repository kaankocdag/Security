import { requireSession, isSystemAdmin } from '@/lib/session';
import { redirect } from 'next/navigation';
import { apiFetch } from '@/lib/api';
import { HackerOneTargetsClient, type TargetCandidate, type TargetDomain } from './targets-client';

export default async function HackerOneTargetsPage() {
  const { accessToken, user } = await requireSession();
  if (!isSystemAdmin(user)) redirect('/dashboard');

  let domains: TargetDomain[] = [];
  let candidates: TargetCandidate[] = [];
  try {
    const [all, cands] = await Promise.all([
      apiFetch<TargetDomain[]>('/api/domains', { accessToken, serverSide: true }),
      apiFetch<TargetCandidate[]>('/api/hackerone/candidates', { accessToken, serverSide: true })
    ]);
    domains = (all || []).filter((d) => d.source === 'HackerOne' || d.hackerOneProgramHandle);
    candidates = cands || [];
  } catch {
    domains = [];
    candidates = [];
  }

  return (
    <div className="space-y-3">
      <div>
        <h2 className="text-lg font-semibold text-slate-900">Targets — para potansiyeli avı</h2>
        <p className="text-sm text-slate-600">
          Bounty-eligible hedefleri işaretleyin → <strong>Candidate Assessment</strong> (ASC) çalışır →
          ManualReview/Submit adayı çıkarsa jackpot efekti + ses → <strong>Rapor hazırla</strong> ile EN
          HackerOne taslağı. Pasif header taraması Report Builder doldurmaz; ASC gerekir.
        </p>
      </div>
      <HackerOneTargetsClient initialDomains={domains} initialCandidates={candidates} />
    </div>
  );
}
