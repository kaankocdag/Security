'use client';

import Link from 'next/link';
import { useCallback, useEffect, useState, useTransition } from 'react';
import { ShieldAlert } from 'lucide-react';
import { severityColor } from '@/lib/utils';

interface Props {
  findingId: string;
  isSystemAdmin: boolean;
  domainAssetId?: string | null;
  domainHostName?: string | null;
  domainIsVerified: boolean;
}

interface ProgressState {
  scanJobId: string;
  status: string;
  progressPercentage: number;
  currentStep: string | null;
  completedSteps: number;
  totalSteps: number;
}

interface ScanResultView {
  id: string;
  securityScore: number;
  executiveSummary: string | null;
  summary: string | null;
  criticalCount: number;
  highCount: number;
  mediumCount: number;
  lowCount: number;
  infoCount: number;
}

interface FindingRow {
  id: string;
  title: string;
  severity: string;
  category: string;
  affectedUrl?: string;
}

const ACTIVE = new Set(['Queued', 'Running', '0', '1']);

function storageKey(findingId: string) {
  return `aea-scan:${findingId}`;
}

export function AuthorizedExternalPanel({
  findingId,
  isSystemAdmin,
  domainAssetId,
  domainHostName,
  domainIsVerified
}: Props) {
  const [pending, startTransition] = useTransition();
  const [error, setError] = useState<string | null>(null);
  const [confirmed, setConfirmed] = useState(false);
  const [progress, setProgress] = useState<ProgressState | null>(null);
  const [result, setResult] = useState<ScanResultView | null>(null);
  const [findings, setFindings] = useState<FindingRow[]>([]);

  const loadCompleted = useCallback(async (scanJobId: string) => {
    const detailRes = await fetch(`/api/backend/api/scans/${scanJobId}`, { cache: 'no-store' });
    if (!detailRes.ok) return;
    const detail = (await detailRes.json()) as {
      status: string;
      progressPercentage: number;
      currentStep: string | null;
      completedSteps: number;
      totalSteps: number;
      result: ScanResultView | null;
    };
    setProgress({
      scanJobId,
      status: detail.status,
      progressPercentage: detail.progressPercentage,
      currentStep: detail.currentStep,
      completedSteps: detail.completedSteps,
      totalSteps: detail.totalSteps
    });
    setResult(detail.result);
    if (detail.result?.id) {
      const fr = await fetch(`/api/backend/api/findings?scanResultId=${detail.result.id}`, {
        cache: 'no-store'
      });
      if (fr.ok) {
        setFindings((await fr.json()) as FindingRow[]);
      }
    }
  }, []);

  // Sayfa yenilenince son AEA taramasını geri yükle
  useEffect(() => {
    if (!isSystemAdmin) return;
    try {
      const saved = sessionStorage.getItem(storageKey(findingId));
      if (!saved) return;
      void loadCompleted(saved);
    } catch {
      // ignore
    }
  }, [findingId, isSystemAdmin, loadCompleted]);

  // Aktif tarama poll
  useEffect(() => {
    if (!progress || !ACTIVE.has(progress.status)) return;
    let cancelled = false;
    const tick = async () => {
      try {
        const res = await fetch(`/api/backend/api/scans/${progress.scanJobId}/progress`, {
          cache: 'no-store'
        });
        if (!res.ok || cancelled) return;
        const p = (await res.json()) as {
          status: string;
          progressPercentage: number;
          currentStep: string | null;
          completedSteps: number;
          totalSteps: number;
        };
        setProgress((prev) =>
          prev
            ? {
                ...prev,
                status: p.status,
                progressPercentage: p.progressPercentage,
                currentStep: p.currentStep,
                completedSteps: p.completedSteps,
                totalSteps: p.totalSteps
              }
            : prev
        );
        if (!ACTIVE.has(p.status)) {
          await loadCompleted(progress.scanJobId);
        }
      } catch {
        // yok say
      }
    };
    void tick();
    const id = window.setInterval(tick, 2000);
    return () => {
      cancelled = true;
      window.clearInterval(id);
    };
  }, [progress?.scanJobId, progress?.status, loadCompleted]);

  if (!isSystemAdmin) {
    return null;
  }

  const start = () => {
    if (!domainAssetId || !domainIsVerified || !confirmed) return;
    startTransition(async () => {
      setError(null);
      setResult(null);
      setFindings([]);
      try {
        const res = await fetch('/api/backend/api/scans', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            domainAssetId,
            scanType: 'FullPassive',
            assessmentMode: 'AuthorizedExternalAssessment',
            assessmentModeName: 'AuthorizedExternalAssessment'
          })
        });
        if (!res.ok) {
          const problem = await res.json().catch(() => undefined);
          setError(problem?.detail ?? 'Yetkili dış değerlendirme başlatılamadı.');
          return;
        }
        const data = (await res.json()) as { scanJobId: string };
        try {
          sessionStorage.setItem(storageKey(findingId), data.scanJobId);
        } catch {
          // ignore
        }
        setProgress({
          scanJobId: data.scanJobId,
          status: 'Queued',
          progressPercentage: 0,
          currentStep: 'Kuyrukta',
          completedSteps: 0,
          totalSteps: 0
        });
      } catch {
        setError('Ağ hatası — tarama başlatılamadı.');
      }
    });
  };

  const running = progress != null && ACTIVE.has(progress.status);

  return (
    <section className="rounded-2xl border border-slate-300 bg-slate-50 p-5 shadow-sm">
      <h2 className="flex items-center gap-2 text-sm font-semibold text-slate-800">
        <ShieldAlert size={16} />
        Yetkili dış değerlendirme (SystemAdmin)
      </h2>
      <p className="mt-2 text-xs leading-relaxed text-slate-600">
        <strong>AuthorizedExternalAssessment</strong> — yalnızca doğrulanmış domainlerde. Serbest
        exploit, payload yükleme veya tahrip edici saldırı yoktur; mevcut güvenlik kontrol paketi
        hedefe GET/HEAD ile yeniden çalıştırılır. Sonuç bu panelde görünür.
      </p>
      <dl className="mt-3 grid gap-1 text-xs text-slate-700 sm:grid-cols-2">
        <div>
          Domain: <strong>{domainHostName ?? '—'}</strong>
        </div>
        <div>
          Doğrulama:{' '}
          <strong className={domainIsVerified ? 'text-emerald-700' : 'text-amber-700'}>
            {domainIsVerified ? 'Doğrulanmış' : 'Doğrulanmamış'}
          </strong>
        </div>
      </dl>

      {!domainIsVerified && (
        <p className="mt-3 rounded-md border border-amber-200 bg-amber-50 px-3 py-2 text-xs text-amber-900">
          Bu mod için önce{' '}
          <Link href="/domains" className="font-semibold underline">
            Domainler
          </Link>{' '}
          sayfasından manuel veya otomatik doğrulama yapın.
        </p>
      )}

      {domainIsVerified && domainAssetId && (
        <div className="mt-3 space-y-3">
          <label className="flex items-start gap-2 text-xs text-slate-700">
            <input
              type="checkbox"
              checked={confirmed}
              onChange={(e) => setConfirmed(e.target.checked)}
              className="mt-0.5 h-4 w-4 rounded border-slate-300"
              disabled={running}
            />
            <span>
              Domain sahipliğinin doğrulandığını; bu değerlendirmenin eğitim/operasyonel amaçlı
              olduğunu ve serbest exploit içermediğini onaylıyorum.
            </span>
          </label>
          <button
            type="button"
            disabled={!confirmed || pending || running}
            onClick={start}
            className="rounded-md bg-slate-800 px-3 py-2 text-sm font-semibold text-white hover:bg-slate-900 disabled:opacity-50"
          >
            {pending ? 'Başlatılıyor…' : running ? 'Değerlendirme sürüyor…' : 'Yetkili dış değerlendirmeyi başlat'}
          </button>
        </div>
      )}

      {error && (
        <div className="mt-3 rounded-md border border-rose-200 bg-rose-50 px-3 py-2 text-xs text-rose-700">
          {error}
        </div>
      )}

      {progress && (
        <div className="mt-4 space-y-3 rounded-xl border border-slate-200 bg-white p-4">
          <div className="flex flex-wrap items-center justify-between gap-2 text-sm">
            <span className="font-semibold text-slate-800">Değerlendirme durumu</span>
            <Link
              href={`/scans/${progress.scanJobId}`}
              className="text-xs font-semibold text-[color:var(--color-brand-700)] hover:underline"
            >
              Tam tarama sayfası →
            </Link>
          </div>
          <div className="grid grid-cols-2 gap-2 text-xs sm:grid-cols-4">
            <Stat label="Durum" value={progress.status} />
            <Stat label="İlerleme" value={`%${progress.progressPercentage}`} />
            <Stat
              label="Adımlar"
              value={`${progress.completedSteps}/${progress.totalSteps || '—'}`}
            />
            <Stat label="Adım" value={progress.currentStep ?? '—'} />
          </div>
          {running && (
            <div className="h-1.5 overflow-hidden rounded-full bg-slate-100">
              <div
                className="h-full rounded-full bg-[color:var(--color-brand-600)] transition-all"
                style={{ width: `${Math.min(100, progress.progressPercentage)}%` }}
              />
            </div>
          )}
        </div>
      )}

      {result && !running && (
        <div className="mt-4 space-y-3 rounded-xl border border-emerald-200 bg-emerald-50/50 p-4">
          <div className="flex flex-wrap items-baseline justify-between gap-2">
            <h3 className="text-sm font-semibold text-slate-800">Sonuç (bu sayfada)</h3>
            <span className="text-lg font-bold text-slate-900">{result.securityScore}/100</span>
          </div>
          <p className="text-sm text-slate-700">
            {result.executiveSummary ?? result.summary ?? 'Özet yok.'}
          </p>
          <div className="flex flex-wrap gap-2 text-xs">
            <Chip label="Kritik" value={result.criticalCount} />
            <Chip label="Yüksek" value={result.highCount} />
            <Chip label="Orta" value={result.mediumCount} />
            <Chip label="Düşük" value={result.lowCount} />
            <Chip label="Bilgi" value={result.infoCount} />
          </div>
          {findings.length > 0 ? (
            <ul className="mt-2 space-y-2">
              {findings.map((f) => (
                <li
                  key={f.id}
                  className={`flex items-center justify-between rounded-md border p-2 text-sm ${severityColor(f.severity)}`}
                >
                  <div>
                    <div className="font-semibold">{f.title}</div>
                    <div className="text-xs opacity-80">
                      {f.category} · {f.affectedUrl ?? '—'}
                    </div>
                  </div>
                  <Link href={`/findings/${f.id}`} className="text-xs font-semibold hover:underline">
                    Detay →
                  </Link>
                </li>
              ))}
            </ul>
          ) : (
            <p className="text-xs text-slate-500">Bu değerlendirmede bulgu üretilmedi.</p>
          )}
        </div>
      )}
    </section>
  );
}

function Stat({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-md border border-slate-100 bg-slate-50 px-2 py-1.5">
      <div className="text-[10px] uppercase text-slate-500">{label}</div>
      <div className="truncate font-medium text-slate-800">{value}</div>
    </div>
  );
}

function Chip({ label, value }: { label: string; value: number }) {
  return (
    <span className="rounded-full border border-slate-200 bg-white px-2 py-0.5 text-slate-700">
      {label}: <strong>{value}</strong>
    </span>
  );
}
