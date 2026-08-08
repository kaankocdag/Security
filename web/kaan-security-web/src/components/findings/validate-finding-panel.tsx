'use client';

import { useState } from 'react';
import { apiFetch } from '@/lib/api';

interface Preconditions {
  findingId: string;
  validatorType: string;
  automationKind: string;
  riskLevel: string;
  canStartAutomatic: boolean;
  manualOnly: boolean;
  missingItems: string[];
  targetInBountyScope: boolean;
  testingMethodAllowed: boolean;
  authorizationValid: boolean;
  hasScopePolicy: boolean;
  hasAuthorizationEvidence: boolean;
  disclaimer: string;
}

interface ValidationRun {
  id: string;
  status: string;
  actualRequestCount: number;
  maxRequestCount: number;
  stopReason?: string | null;
  result?: {
    confirmedVulnerability: boolean;
    demonstratedImpact: boolean;
    submissionEligible: boolean;
    potentialRewardEligible: boolean;
    submissionRecommendation: string;
    eligibilityReason?: string | null;
    manualReviewReasons: string[];
    expectedResult?: string | null;
    actualResult?: string | null;
    rewardDisclaimer: string;
  } | null;
}

function Badge({ label, tone }: { label: string; tone: string }) {
  return (
    <span className={`rounded px-1.5 py-0.5 text-[10px] font-semibold uppercase ${tone}`}>{label}</span>
  );
}

export function ValidateFindingPanel({
  findingId,
  domainAssetId,
  confirmedVulnerability,
  demonstratedImpact,
  submissionEligible,
  potentialRewardEligible,
  latestValidationStatus,
  findingClass
}: {
  findingId: string;
  domainAssetId?: string | null;
  confirmedVulnerability?: boolean;
  demonstratedImpact?: boolean;
  submissionEligible?: boolean;
  potentialRewardEligible?: boolean;
  latestValidationStatus?: string | null;
  findingClass?: string;
}) {
  const [open, setOpen] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [pre, setPre] = useState<Preconditions | null>(null);
  const [run, setRun] = useState<ValidationRun | null>(null);
  const [approved, setApproved] = useState(false);
  const [ownedResource, setOwnedResource] = useState('');

  async function loadPreconditions() {
    setBusy(true);
    setError(null);
    try {
      const data = await apiFetch<Preconditions>(`/api/findings/validation/${findingId}/preconditions`);
      setPre(data);
      setOpen(true);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Ön koşullar alınamadı');
    } finally {
      setBusy(false);
    }
  }

  async function startValidation() {
    if (!approved) {
      setError('Açık kullanıcı onayı zorunlu.');
      return;
    }
    setBusy(true);
    setError(null);
    try {
      const data = await apiFetch<ValidationRun>('/api/findings/validation/start', {
        method: 'POST',
        body: {
          findingId,
          explicitUserApproval: true,
          ownedTestResourceUrl: ownedResource || null
        }
      });
      setRun(data);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Doğrulama başlatılamadı');
    } finally {
      setBusy(false);
    }
  }

  async function stopValidation() {
    if (!run?.id) return;
    setBusy(true);
    try {
      const data = await apiFetch<ValidationRun>(`/api/findings/validation/runs/${run.id}/stop`, {
        method: 'POST'
      });
      setRun(data);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Durdurma başarısız');
    } finally {
      setBusy(false);
    }
  }

  const manualOnly = pre?.manualOnly === true;
  const buttonLabel = manualOnly ? 'Manuel Doğrulama Gerekli' : 'Bulguyu Doğrula';

  return (
    <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
      <div className="flex flex-wrap items-center gap-2">
        <h2 className="text-sm font-semibold text-slate-800">Finding Validation</h2>
        {findingClass?.includes('Candidate') && <Badge label="Candidate" tone="bg-sky-100 text-sky-800" />}
        {latestValidationStatus === 'ManualReviewRequired' && (
          <Badge label="Manual Review" tone="bg-amber-100 text-amber-900" />
        )}
        {confirmedVulnerability && <Badge label="Confirmed" tone="bg-emerald-100 text-emerald-800" />}
        {demonstratedImpact && <Badge label="Impact Demonstrated" tone="bg-emerald-100 text-emerald-900" />}
        {submissionEligible ? (
          <Badge label="Submission Candidate" tone="bg-amber-200 text-amber-950" />
        ) : (
          <Badge label="Not Submission Eligible" tone="bg-slate-200 text-slate-700" />
        )}
        {latestValidationStatus === 'BlockedByPolicy' && (
          <Badge label="Blocked by Policy" tone="bg-rose-100 text-rose-800" />
        )}
        {!confirmedVulnerability && <Badge label="Validation Required" tone="bg-indigo-100 text-indigo-800" />}
      </div>

      <p className="mt-2 text-xs text-slate-600">
        Aday sinyal ≠ Para. Para / SubmissionCandidate yalnızca doğrulanmış etki + uygunluk sonrası. Reward not
        guaranteed. Target bounty scope tek başına yetmez.
      </p>

      <div className="mt-3 flex flex-wrap gap-2">
        <button
          type="button"
          disabled={busy}
          onClick={() => void loadPreconditions()}
          className="rounded-md bg-indigo-700 px-3 py-1.5 text-sm font-semibold text-white disabled:opacity-50"
        >
          {busy && !open ? '…' : buttonLabel}
        </button>
        {domainAssetId && (
          <span className="self-center text-[11px] text-slate-500">Target: {domainAssetId.slice(0, 8)}…</span>
        )}
      </div>

      {error && <p className="mt-2 text-xs text-rose-700">{error}</p>}

      {open && pre && (
        <div className="mt-4 space-y-3 rounded-xl border border-slate-100 bg-slate-50 p-3 text-sm">
          <div className="text-xs text-slate-600">
            Validator: <strong>{pre.validatorType}</strong> · Risk: {pre.riskLevel} · {pre.automationKind}
          </div>
          <p className="text-xs text-slate-600">{pre.disclaimer}</p>
          {pre.missingItems.length > 0 && (
            <div>
              <div className="text-xs font-semibold text-rose-800">Eksik ön koşullar</div>
              <ul className="mt-1 list-disc pl-5 text-xs text-rose-700">
                {pre.missingItems.map((m) => (
                  <li key={m}>{m}</li>
                ))}
              </ul>
              <p className="mt-2 text-[11px] text-slate-500">
                ScopePolicy ve AuthorizationEvidence API ile kaydedilmeden aktif doğrulama başlamaz (
                <code>/api/findings/validation/scope-policy</code>,{' '}
                <code>/api/findings/validation/authorization-evidence</code>).
              </p>
            </div>
          )}

          {!pre.manualOnly && (
            <>
              <label className="flex items-start gap-2 text-xs text-slate-700">
                <input type="checkbox" checked={approved} onChange={(e) => setApproved(e.target.checked)} />
                Yetkili hedefte güvenli doğrulamayı açıkça onaylıyorum (pasif/read-only veya izinli diferansiyel
                GET). Brute force / bypass yok.
              </label>
              <label className="block text-xs text-slate-600">
                Owned test resource URL (opsiyonel, AccessControl diferansiyel için)
                <input
                  className="mt-1 w-full rounded border px-2 py-1 text-sm"
                  value={ownedResource}
                  onChange={(e) => setOwnedResource(e.target.value)}
                  placeholder="https://…/users/me-test-resource"
                />
              </label>
              <div className="flex gap-2">
                <button
                  type="button"
                  disabled={busy || !pre.canStartAutomatic || !approved}
                  onClick={() => void startValidation()}
                  className="rounded-md bg-emerald-700 px-3 py-1.5 text-xs font-semibold text-white disabled:opacity-50"
                >
                  Doğrulamayı başlat
                </button>
                {run && run.status === 'Running' && (
                  <button
                    type="button"
                    disabled={busy}
                    onClick={() => void stopValidation()}
                    className="rounded-md border border-rose-300 bg-rose-50 px-3 py-1.5 text-xs font-semibold text-rose-800"
                  >
                    Durdur
                  </button>
                )}
              </div>
            </>
          )}

          {run && (
            <div className="rounded-md border border-slate-200 bg-white p-3 text-xs text-slate-700">
              <div>
                Status: <strong>{run.status}</strong> · İstek: {run.actualRequestCount}/{run.maxRequestCount}
              </div>
              {run.result && (
                <dl className="mt-2 grid grid-cols-1 gap-1 md:grid-cols-2">
                  <div>Confirmed: {run.result.confirmedVulnerability ? 'Yes' : 'No'}</div>
                  <div>Impact: {run.result.demonstratedImpact ? 'Yes' : 'No'}</div>
                  <div>SubmissionEligible: {run.result.submissionEligible ? 'Yes' : 'No'}</div>
                  <div>PotentialRewardEligible: {run.result.potentialRewardEligible ? 'Yes' : 'No'}</div>
                  <div className="md:col-span-2">Rec: {run.result.submissionRecommendation}</div>
                  <div className="md:col-span-2 text-amber-800">{run.result.rewardDisclaimer}</div>
                  <div className="md:col-span-2">{run.result.eligibilityReason}</div>
                  <div className="md:col-span-2 whitespace-pre-wrap">{run.result.actualResult}</div>
                </dl>
              )}
            </div>
          )}
        </div>
      )}
    </section>
  );
}
