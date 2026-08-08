'use client';

import { useEffect, useRef, useState, useTransition } from 'react';
import { useRouter } from 'next/navigation';
import { playFunnyFindingAlarm, unlockAudio } from '@/lib/funny-alarm';
import { formatDateTr } from '@/lib/utils';

export interface DomainListItem {
  id: string;
  hostName: string;
  securityProjectId: string;
  status: string;
  isVerified: boolean;
  verifiedAt: string | null;
  createdAt: string;
  source?: string;
  hackerOneProgramHandle?: string | null;
  hackerOneProgramName?: string | null;
  hackerOneEligibleForBounty?: boolean | null;
  hackerOneOffersBounties?: boolean | null;
  hackerOneCurrency?: string | null;
  hackerOneMaxSeverity?: string | null;
  hackerOneBountySummary?: string | null;
  hackerOneIsWildcard?: boolean;
  hackerOneAssetType?: string | null;
}

interface Props {
  initialDomains: DomainListItem[];
  isSystemAdmin: boolean;
}

function sleep(ms: number) {
  return new Promise((r) => setTimeout(r, ms));
}

function isArchived(d: DomainListItem) {
  return d.status === 'Archived' || d.status === '3';
}

function isScannable(d: DomainListItem) {
  if (isArchived(d)) return false;
  if (d.hackerOneIsWildcard) return false;
  if (d.hostName.includes('*')) return false;
  return true;
}

function isTerminalScanStatus(status: string) {
  return ['Completed', 'Failed', 'Cancelled', 'PartiallyCompleted'].includes(status);
}

function hasInterestingFindings(result: {
  criticalCount?: number;
  highCount?: number;
  mediumCount?: number;
  lowCount?: number;
  checksFailed?: number;
} | null | undefined) {
  if (!result) return false;
  const interesting =
    (result.criticalCount ?? 0) + (result.highCount ?? 0) + (result.mediumCount ?? 0);
  return interesting > 0 || (result.checksFailed ?? 0) > 0;
}

export function DomainsTableClient({ initialDomains, isSystemAdmin }: Props) {
  const router = useRouter();
  const [domains, setDomains] = useState(initialDomains);
  const [pendingId, setPendingId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [syncMsg, setSyncMsg] = useState<string | null>(null);
  const [syncBusy, setSyncBusy] = useState(false);
  const [pending, startTransition] = useTransition();
  const [filter, setFilter] = useState<'all' | 'hackerone' | 'manual'>('all');

  const [scanBusy, setScanBusy] = useState(false);
  const [scanningIds, setScanningIds] = useState<Set<string>>(() => new Set());
  const [scannedIds, setScannedIds] = useState<Set<string>>(() => new Set());
  const [hitIds, setHitIds] = useState<Set<string>>(() => new Set());
  const [scanMsg, setScanMsg] = useState<string | null>(null);
  const [concurrency, setConcurrency] = useState(3);
  const stopScanRef = useRef(false);
  const statsRef = useRef({ ok: 0, fail: 0, hits: 0, done: 0, total: 0 });

  useEffect(() => {
    setDomains(initialDomains);
  }, [initialDomains]);

  const setVerification = (id: string, isVerified: boolean) => {
    startTransition(async () => {
      setError(null);
      setPendingId(id);
      try {
        const res = await fetch(`/api/backend/api/domains/${id}/verification/manual`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            isVerified,
            note: isVerified ? 'SystemAdmin manuel onay' : 'SystemAdmin doğrulamayı kaldırdı'
          })
        });
        if (!res.ok) {
          const problem = await res.json().catch(() => undefined);
          setError(problem?.detail ?? 'Doğrulama güncellenemedi.');
          return;
        }
        const data = (await res.json()) as {
          isVerified: boolean;
          status: string;
          verifiedAt: string | null;
        };
        setDomains((list) =>
          list.map((d) =>
            d.id === id
              ? {
                  ...d,
                  isVerified: data.isVerified,
                  status: data.status,
                  verifiedAt: data.verifiedAt
                }
              : d
          )
        );
        router.refresh();
      } catch {
        setError('Ağ hatası — doğrulama güncellenemedi.');
      } finally {
        setPendingId(null);
      }
    });
  };

  async function syncHackerOneScopes() {
    setSyncBusy(true);
    setSyncMsg(null);
    setError(null);
    try {
      const res = await fetch('/api/backend/api/hackerone/domains/sync-scopes', { method: 'POST' });
      const data = await res.json().catch(() => ({}));
      if (!res.ok && res.status !== 202) {
        setError(
          data.detail ||
            data.title ||
            'HackerOne scope sync başlatılamadı. Settings’te kullanıcı adı + token kontrol edin.'
        );
        return;
      }
      setSyncMsg(
        data.message ||
          `Kuyruğa alındı${data.jobId ? ` (${data.jobId})` : ''}. Domainler arka planda dolar — sayfayı yenileyin.`
      );
      router.refresh();
    } catch {
      setError('Ağ hatası — sync başlatılamadı.');
    } finally {
      setSyncBusy(false);
    }
  }

  function refreshScanMsg() {
    const s = statsRef.current;
    setScanMsg(
      `Paralel tarama · biten ${s.done}/${s.total} · OK ${s.ok} · hata ${s.fail} · bulgu ${s.hits} · eşzamanlı ${concurrency}`
    );
  }

  async function waitForScanComplete(scanJobId: string): Promise<'done' | 'failed' | 'stopped'> {
    for (let i = 0; i < 180; i++) {
      if (stopScanRef.current) return 'stopped';
      const res = await fetch(`/api/backend/api/scans/${scanJobId}/progress`);
      if (!res.ok) {
        await sleep(2000);
        continue;
      }
      const p = (await res.json()) as { status: string; progressPercentage: number };
      if (isTerminalScanStatus(p.status)) {
        return p.status === 'Failed' || p.status === 'Cancelled' ? 'failed' : 'done';
      }
      await sleep(2500);
    }
    return 'failed';
  }

  async function checkFindingsAndAlarm(scanJobId: string, domainId: string, hostName: string) {
    try {
      const res = await fetch(`/api/backend/api/scans/${scanJobId}`);
      if (!res.ok) return;
      const detail = (await res.json()) as {
        result?: {
          criticalCount?: number;
          highCount?: number;
          mediumCount?: number;
          lowCount?: number;
          checksFailed?: number;
        } | null;
      };
      if (!hasInterestingFindings(detail.result)) return;

      setHitIds((prev) => new Set(prev).add(domainId));
      statsRef.current.hits += 1;
      refreshScanMsg();
      setScanMsg((m) => `${m ?? ''} · 🚨 bulgu: ${hostName}`);
      await playFunnyFindingAlarm();
    } catch {
      // alarm opsiyonel
    }
  }

  async function scanOneDomain(d: DomainListItem) {
    setScanningIds((prev) => new Set(prev).add(d.id));
    try {
      if (stopScanRef.current) return;

      const res = await fetch('/api/backend/api/scans', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          domainAssetId: d.id,
          scanType: 'FullPassive',
          assessmentMode: 'PublicPassiveAssessment'
        })
      });
      const data = await res.json().catch(() => ({}));
      if (!res.ok) {
        statsRef.current.fail += 1;
        setError(`${d.hostName}: ${data.detail || data.title || 'tarama başlatılamadı'}`);
        return;
      }

      const scanJobId = data.scanJobId as string;
      const outcome = await waitForScanComplete(scanJobId);
      if (outcome === 'done') {
        statsRef.current.ok += 1;
        await checkFindingsAndAlarm(scanJobId, d.id, d.hostName);
      } else if (outcome === 'failed') {
        statsRef.current.fail += 1;
      }
    } catch {
      statsRef.current.fail += 1;
      setError(`${d.hostName}: ağ hatası`);
    } finally {
      statsRef.current.done += 1;
      setScanningIds((prev) => {
        const next = new Set(prev);
        next.delete(d.id);
        return next;
      });
      setScannedIds((prev) => new Set(prev).add(d.id));
      refreshScanMsg();
    }
  }

  async function runParallelScans() {
    const queue = domains.filter((d) => {
      if (filter === 'hackerone' && d.source !== 'HackerOne') return false;
      if (filter === 'manual' && d.source === 'HackerOne') return false;
      return isScannable(d);
    });

    if (queue.length === 0) {
      setError('Taranacak domain yok (wildcard / arşiv atlanır). Filtreyi kontrol edin.');
      return;
    }

    const workers = Math.min(Math.max(1, concurrency), 10);
    stopScanRef.current = false;
    setScanBusy(true);
    setError(null);
    setScannedIds(new Set());
    setHitIds(new Set());
    setScanningIds(new Set());
    statsRef.current = { ok: 0, fail: 0, hits: 0, done: 0, total: queue.length };
    setScanMsg(`Paralel tarama başlıyor · ${queue.length} domain · ${workers} eşzamanlı`);

    // Tarayıcı ses kilidini kullanıcı tıklamasında aç (alarm sadece bulguda)
    void unlockAudio();

    let index = 0;
    async function worker() {
      while (!stopScanRef.current) {
        const i = index++;
        if (i >= queue.length) break;
        await scanOneDomain(queue[i]!);
      }
    }

    await Promise.all(Array.from({ length: workers }, () => worker()));

    setScanningIds(new Set());
    setScanBusy(false);
    const s = statsRef.current;
    if (stopScanRef.current) {
      setScanMsg(`Durduruldu · biten ${s.done}/${s.total} · bulgu ${s.hits}`);
    } else {
      setScanMsg(`Paralel tarama bitti · OK ${s.ok} · hata ${s.fail} · bulgu ${s.hits}`);
    }
    router.refresh();
  }

  function stopScans() {
    stopScanRef.current = true;
    setScanMsg('Durdurma istenildi — çalışan taramalar bitsin, yenileri başlamasın…');
  }

  const visible = domains.filter((d) => {
    if (filter === 'hackerone') return d.source === 'HackerOne';
    if (filter === 'manual') return d.source !== 'HackerOne';
    return true;
  });

  const scannableCount = visible.filter(isScannable).length;
  const colCount = isSystemAdmin ? 7 : 6;

  return (
    <div className="space-y-3">
      {error && (
        <div className="rounded-md border border-rose-200 bg-rose-50 px-3 py-2 text-xs text-rose-700">
          {error}
        </div>
      )}
      {syncMsg && (
        <div className="rounded-md border border-sky-200 bg-sky-50 px-3 py-2 text-xs text-sky-800">
          {syncMsg}
        </div>
      )}
      {scanMsg && (
        <div className="rounded-md border border-emerald-200 bg-emerald-50 px-3 py-2 text-xs text-emerald-900">
          {scanMsg}
        </div>
      )}
      <div className="flex flex-wrap items-center gap-2">
        {isSystemAdmin && (
          <>
            <button
              type="button"
              disabled={syncBusy || scanBusy}
              onClick={syncHackerOneScopes}
              className="rounded-md bg-slate-900 px-3 py-1.5 text-sm font-semibold text-white hover:bg-slate-800 disabled:opacity-50"
            >
              {syncBusy ? 'Kuyruğa alınıyor…' : 'Tüm HackerOne programlarını senkronize et'}
            </button>
            <label className="flex items-center gap-1 text-xs text-slate-600">
              Eşzamanlı
              <input
                type="number"
                min={1}
                max={10}
                disabled={scanBusy}
                value={concurrency}
                onChange={(e) => setConcurrency(Math.min(10, Math.max(1, Number(e.target.value) || 1)))}
                className="w-14 rounded border border-slate-200 px-1.5 py-1 text-sm disabled:opacity-50"
              />
            </label>
            {!scanBusy ? (
              <button
                type="button"
                disabled={syncBusy || scannableCount === 0}
                onClick={runParallelScans}
                className="rounded-md bg-emerald-700 px-3 py-1.5 text-sm font-semibold text-white hover:bg-emerald-800 disabled:opacity-50"
              >
                Paralel tara ({scannableCount})
              </button>
            ) : (
              <button
                type="button"
                onClick={stopScans}
                className="rounded-md border border-rose-300 bg-rose-50 px-3 py-1.5 text-sm font-semibold text-rose-800 hover:bg-rose-100"
              >
                Taramayı durdur
              </button>
            )}
            <button
              type="button"
              onClick={() => void playFunnyFindingAlarm()}
              className="rounded-md border border-amber-300 bg-amber-50 px-2 py-1.5 text-xs font-medium text-amber-900 hover:bg-amber-100"
              title="Alarm sesini test et"
            >
              🔔 Alarm test
            </button>
          </>
        )}
        <div className="flex gap-1 text-xs">
          {(
            [
              ['all', 'Tümü'],
              ['hackerone', 'HackerOne'],
              ['manual', 'Manuel']
            ] as const
          ).map(([key, label]) => (
            <button
              key={key}
              type="button"
              disabled={scanBusy}
              onClick={() => setFilter(key)}
              className={`rounded-md border px-2 py-1 disabled:opacity-50 ${
                filter === key
                  ? 'border-slate-800 bg-slate-800 text-white'
                  : 'border-slate-200 bg-white text-slate-600 hover:bg-slate-50'
              }`}
            >
              {label}
            </button>
          ))}
        </div>
        <span className="text-xs text-slate-500">{visible.length} kayıt</span>
      </div>
      {isSystemAdmin && (
        <p className="text-xs text-slate-500">
          Paralel tara: seçilen eşzamanlı sayıda PublicPassive tarama. Aktif satırlar yeşil çerçeve; bulgu
          (Critical/High/Medium veya failed check) olursa satır turuncu + komik alarm. Wildcard/arşiv atlanır.
        </p>
      )}
      <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-left text-xs uppercase text-slate-500">
            <tr>
              <th className="px-4 py-2">Alan</th>
              <th className="px-4 py-2">Kaynak</th>
              <th className="px-4 py-2">Bounty / özet</th>
              <th className="px-4 py-2">Durum</th>
              <th className="px-4 py-2">Doğrulama</th>
              <th className="px-4 py-2">Kayıt</th>
              {isSystemAdmin && <th className="px-4 py-2 text-right">Yönetim</th>}
            </tr>
          </thead>
          <tbody>
            {visible.length === 0 ? (
              <tr>
                <td colSpan={colCount} className="px-4 py-6 text-center text-slate-500">
                  Henüz domain yok.
                </td>
              </tr>
            ) : (
              visible.map((d) => {
                const busy = pending && pendingId === d.id;
                const archived = isArchived(d);
                const isH1 = d.source === 'HackerOne';
                const isScanning = scanningIds.has(d.id);
                const wasScanned = scannedIds.has(d.id);
                const isHit = hitIds.has(d.id);
                return (
                  <tr
                    key={d.id}
                    className={[
                      'border-t border-slate-100 transition-colors',
                      isHit
                        ? 'relative z-10 bg-amber-50 outline outline-2 outline-offset-[-2px] outline-amber-500 ring-2 ring-amber-300/50'
                        : isScanning
                          ? 'relative z-10 bg-emerald-50 outline outline-2 outline-offset-[-2px] outline-emerald-600 ring-2 ring-emerald-400/40'
                          : wasScanned
                            ? 'bg-slate-50/80'
                            : 'hover:bg-slate-50'
                    ].join(' ')}
                  >
                    <td className="px-4 py-2">
                      <div className="font-medium text-slate-800">
                        {d.hostName}
                        {d.hackerOneIsWildcard && (
                          <span className="ml-1 text-xs font-normal text-amber-700">wildcard</span>
                        )}
                        {isScanning && (
                          <span className="ml-2 rounded bg-emerald-600 px-1.5 py-0.5 text-[10px] font-semibold uppercase tracking-wide text-white">
                            taranıyor
                          </span>
                        )}
                        {isHit && !isScanning && (
                          <span className="ml-2 rounded bg-amber-500 px-1.5 py-0.5 text-[10px] font-semibold uppercase tracking-wide text-white">
                            bulgu!
                          </span>
                        )}
                        {wasScanned && !isScanning && !isHit && (
                          <span className="ml-2 text-[10px] font-medium uppercase text-slate-400">
                            tarandı
                          </span>
                        )}
                      </div>
                      {isH1 && d.hackerOneProgramName && (
                        <div className="text-xs text-slate-500">
                          {d.hackerOneProgramName}
                          {d.hackerOneProgramHandle ? ` (${d.hackerOneProgramHandle})` : ''}
                        </div>
                      )}
                    </td>
                    <td className="px-4 py-2">
                      {isH1 ? (
                        <span className="rounded bg-orange-100 px-2 py-0.5 text-xs font-medium text-orange-900">
                          HackerOne
                        </span>
                      ) : (
                        <span className="rounded bg-slate-100 px-2 py-0.5 text-xs text-slate-600">Manuel</span>
                      )}
                    </td>
                    <td className="max-w-xs px-4 py-2 text-xs text-slate-700">
                      {isH1 ? (
                        <div className="space-y-0.5">
                          <div>
                            {d.hackerOneEligibleForBounty ? (
                              <span className="font-medium text-emerald-700">Bounty eligible</span>
                            ) : (
                              <span className="text-slate-500">Bounty eligible değil</span>
                            )}
                            {d.hackerOneOffersBounties != null && (
                              <span className="text-slate-500">
                                {' '}
                                · program {d.hackerOneOffersBounties ? 'ödüyor' : 'ödemiyor'}
                                {d.hackerOneCurrency ? ` (${d.hackerOneCurrency})` : ''}
                              </span>
                            )}
                          </div>
                          {d.hackerOneMaxSeverity && (
                            <div className="text-slate-500">max severity: {d.hackerOneMaxSeverity}</div>
                          )}
                          {d.hackerOneBountySummary && (
                            <div className="line-clamp-2 text-slate-500" title={d.hackerOneBountySummary}>
                              {d.hackerOneBountySummary}
                            </div>
                          )}
                        </div>
                      ) : (
                        <span className="text-slate-400">—</span>
                      )}
                    </td>
                    <td className="px-4 py-2">{d.status}</td>
                    <td className="px-4 py-2">
                      {d.isVerified ? (
                        <span className="rounded-full bg-emerald-100 px-2 py-0.5 text-xs text-emerald-800">
                          Doğrulandı · {formatDateTr(d.verifiedAt)}
                        </span>
                      ) : (
                        <span className="rounded-full bg-amber-100 px-2 py-0.5 text-xs text-amber-800">
                          Beklemede
                        </span>
                      )}
                    </td>
                    <td className="px-4 py-2 text-slate-600">{formatDateTr(d.createdAt)}</td>
                    {isSystemAdmin && (
                      <td className="px-4 py-2 text-right">
                        {archived ? (
                          <span className="text-xs text-slate-400">Arşivli</span>
                        ) : d.isVerified ? (
                          <button
                            type="button"
                            disabled={busy || scanBusy}
                            onClick={() => setVerification(d.id, false)}
                            className="rounded-md border border-slate-200 px-2 py-1 text-xs font-medium text-slate-700 hover:bg-slate-100 disabled:opacity-50"
                          >
                            {busy ? '…' : 'Doğrulamayı kaldır'}
                          </button>
                        ) : (
                          <button
                            type="button"
                            disabled={busy || scanBusy}
                            onClick={() => setVerification(d.id, true)}
                            className="rounded-md bg-emerald-600 px-2 py-1 text-xs font-semibold text-white hover:bg-emerald-700 disabled:opacity-50"
                          >
                            {busy ? '…' : 'Manuel doğrula'}
                          </button>
                        )}
                      </td>
                    )}
                  </tr>
                );
              })
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
