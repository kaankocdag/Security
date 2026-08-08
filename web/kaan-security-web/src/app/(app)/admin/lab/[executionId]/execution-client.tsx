'use client';

import Link from 'next/link';
import { useCallback, useEffect, useState } from 'react';

interface Step {
  stepKind: number;
  stepOrder: number;
  titleTr: string;
  status: number;
  summaryTr?: string | null;
  startedAt?: string | null;
  completedAt?: string | null;
}

interface Comparison {
  initialTestFailed: boolean;
  retestSucceeded: boolean;
  vulnerableScore: number;
  patchedScore: number;
  riskTr: string;
  whyTr: string;
  fixTr: string;
  summaryTr: string;
}

interface Detail {
  id: string;
  scenarioKey: string;
  scenarioTitleTr: string;
  targetHostName?: string;
  status: number;
  runtimeMode: number;
  auditCorrelationId: string;
  elevatedByEmail: string;
  createdAt: string;
  startedAt?: string | null;
  completedAt?: string | null;
  failureReasonTr?: string | null;
  steps: Step[];
  comparison?: Comparison | null;
}

interface LogItem {
  id: string;
  level: string;
  messageTr: string;
  loggedAt: string;
}

const stepStatus: Record<number, string> = {
  0: 'Bekliyor',
  1: 'Çalışıyor',
  2: 'Tamam',
  3: 'Hata',
  4: 'Atlandı'
};

const execStatus: Record<number, string> = {
  1: 'Kuyrukta',
  2: 'Çalışıyor',
  3: 'Tamamlandı',
  4: 'Başarısız',
  5: 'İptal',
  6: 'Temizleniyor',
  7: 'Yok edildi'
};

async function labFetch<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`/api/backend/${path}`, {
    ...init,
    headers: { 'Content-Type': 'application/json', ...(init?.headers ?? {}) }
  });
  if (!res.ok) {
    let detail = res.statusText;
    try {
      const body = await res.json();
      detail = body.detail || body.title || detail;
    } catch {
      /* ignore */
    }
    throw new Error(detail);
  }
  if (res.status === 204) return undefined as T;
  return res.json();
}

export function LabExecutionClient({ executionId }: { executionId: string }) {
  const [detail, setDetail] = useState<Detail | null>(null);
  const [logs, setLogs] = useState<LogItem[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const load = useCallback(async () => {
    try {
      const [d, l] = await Promise.all([
        labFetch<Detail>(`api/admin/lab/executions/${executionId}`),
        labFetch<LogItem[]>(`api/admin/lab/executions/${executionId}/logs`)
      ]);
      setDetail(d);
      setLogs(l);
      setError(null);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Yüklenemedi');
    }
  }, [executionId]);

  useEffect(() => {
    void load();
    const id = window.setInterval(() => {
      void load();
    }, 2000);
    return () => window.clearInterval(id);
  }, [load]);

  async function cancel() {
    setBusy(true);
    try {
      await labFetch(`api/admin/lab/executions/${executionId}/cancel`, {
        method: 'POST',
        body: JSON.stringify({ reasonTr: 'Acil durdur (UI)' })
      });
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'İptal başarısız');
    } finally {
      setBusy(false);
    }
  }

  if (!detail && !error) {
    return <p className="text-sm text-slate-600">Yükleniyor…</p>;
  }

  const running = detail?.status === 1 || detail?.status === 2 || detail?.status === 6;

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <Link href="/admin/lab" className="text-sm text-[color:var(--color-brand-700)] hover:underline">
            ← Laboratuvar listesi
          </Link>
          <h1 className="mt-2 text-2xl font-bold text-slate-900">
            {detail?.scenarioTitleTr ?? 'Lab oturumu'}
          </h1>
          <p className="mt-1 text-sm text-slate-600">
            Hedef: <code className="text-xs">{detail?.targetHostName ?? '—'}</code> · Durum:{' '}
            {detail ? execStatus[detail.status] ?? detail.status : '—'} · Runtime:{' '}
            {detail?.runtimeMode === 1 ? 'Docker' : 'Mock'} · Correlation:{' '}
            <code className="text-xs">{detail?.auditCorrelationId}</code>
          </p>
        </div>
        {running && (
          <button
            type="button"
            disabled={busy}
            onClick={cancel}
            className="rounded-md bg-red-600 px-4 py-2 text-sm font-medium text-white disabled:opacity-50"
          >
            Acil durdur
          </button>
        )}
      </div>

      {error && <p className="text-sm text-red-600">{error}</p>}
      {detail?.failureReasonTr && (
        <p className="text-sm text-red-700">{detail.failureReasonTr}</p>
      )}

      <section className="rounded-lg border border-slate-200 bg-white p-5">
        <h2 className="mb-3 text-lg font-semibold">10 adımlı ilerleme</h2>
        <ol className="space-y-2">
          {(detail?.steps ?? []).map((s) => (
            <li
              key={s.stepOrder}
              className="flex flex-col rounded-md border border-slate-100 px-3 py-2 md:flex-row md:items-center md:justify-between"
            >
              <div>
                <span className="font-medium text-slate-800">
                  {s.stepOrder}. {s.titleTr}
                </span>
                {s.summaryTr && (
                  <p className="mt-0.5 text-sm text-slate-600">{s.summaryTr}</p>
                )}
              </div>
              <span className="mt-1 text-xs font-medium uppercase tracking-wide text-slate-500 md:mt-0">
                {stepStatus[s.status] ?? s.status}
              </span>
            </li>
          ))}
        </ol>
      </section>

      {detail?.comparison && (
        <section className="space-y-2 rounded-lg border border-emerald-200 bg-emerald-50/40 p-5">
          <h2 className="text-lg font-semibold text-slate-900">Karşılaştırma</h2>
          <ul className="space-y-1 text-sm text-slate-700">
            <li>
              İlk test: {detail.comparison.initialTestFailed ? 'Başarısız' : 'Başarılı'} (skor{' '}
              {detail.comparison.vulnerableScore})
            </li>
            <li>
              Yeniden test: {detail.comparison.retestSucceeded ? 'Başarılı' : 'Başarısız'} (skor{' '}
              {detail.comparison.patchedScore})
            </li>
            <li>
              <strong>Risk:</strong> {detail.comparison.riskTr}
            </li>
            <li>
              <strong>Neden:</strong> {detail.comparison.whyTr}
            </li>
            <li>
              <strong>Düzeltme:</strong> {detail.comparison.fixTr}
            </li>
            <li>{detail.comparison.summaryTr}</li>
          </ul>
        </section>
      )}

      <section className="rounded-lg border border-slate-200 bg-white p-5">
        <h2 className="mb-3 text-lg font-semibold">Sanitize loglar</h2>
        <ul className="max-h-64 space-y-1 overflow-y-auto font-mono text-xs text-slate-600">
          {logs.length === 0 && <li>Henüz log yok.</li>}
          {logs.map((l) => (
            <li key={l.id}>
              [{new Date(l.loggedAt).toLocaleTimeString('tr-TR')}] {l.level}: {l.messageTr}
            </li>
          ))}
        </ul>
      </section>
    </div>
  );
}
