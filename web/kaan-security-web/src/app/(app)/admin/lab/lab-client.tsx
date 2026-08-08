'use client';

import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useState } from 'react';
import type { LabExecutionListItem, LabScenario } from './page';

const CONFIRM_PHRASE = 'LABORATUVAR SENARYOSUNU BASLATMAYI ONAYLIYORUM';

const statusLabel: Record<number, string> = {
  0: 'Yükseltme bekleniyor',
  1: 'Kuyrukta',
  2: 'Çalışıyor',
  3: 'Tamamlandı',
  4: 'Başarısız',
  5: 'İptal',
  6: 'Temizleniyor',
  7: 'Yok edildi'
};

export interface LabTargetSite {
  id: string;
  hostName: string;
  normalizedHostName: string;
  notesTr?: string | null;
  isEnabled: boolean;
  createdAt: string;
}

async function labFetch<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`/api/backend/${path}`, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...(init?.headers ?? {})
    }
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

export function LabClient({
  initialScenarios,
  initialExecutions,
  initialTargets
}: {
  initialScenarios: LabScenario[];
  initialExecutions: LabExecutionListItem[];
  initialTargets: LabTargetSite[];
}) {
  const router = useRouter();
  const [scenarios] = useState(initialScenarios);
  const [executions, setExecutions] = useState(initialExecutions);
  const [targets, setTargets] = useState(initialTargets);
  const [selectedKey, setSelectedKey] = useState(scenarios[0]?.scenarioKey ?? '');
  const [selectedTargetId, setSelectedTargetId] = useState(
    targets.find((t) => t.isEnabled)?.id ?? ''
  );
  const [newHost, setNewHost] = useState('');
  const [password, setPassword] = useState('');
  const [elevationToken, setElevationToken] = useState<string | null>(null);
  const [confirmed, setConfirmed] = useState(false);
  const [phrase, setPhrase] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [info, setInfo] = useState<string | null>(null);

  async function addTarget() {
    setBusy(true);
    setError(null);
    try {
      const site = await labFetch<LabTargetSite>('api/admin/lab/targets', {
        method: 'POST',
        body: JSON.stringify({ hostName: newHost.trim(), notesTr: 'IsolatedSecurityLab allowlist' })
      });
      setTargets((t) => [site, ...t]);
      setSelectedTargetId(site.id);
      setNewHost('');
      setInfo('Hedef allowlist’e eklendi (serbest URL yok — yalnızca hostname).');
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Hedef eklenemedi');
    } finally {
      setBusy(false);
    }
  }

  async function elevate() {
    setBusy(true);
    setError(null);
    try {
      const res = await labFetch<{ elevationToken: string }>(
        'api/admin/lab/elevation',
        { method: 'POST', body: JSON.stringify({ password }) }
      );
      setElevationToken(res.elevationToken);
      setInfo('Yükseltme bileti alındı. IsolatedSecurityLab senaryosunu başlatabilirsiniz.');
      setPassword('');
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Yükseltme başarısız');
    } finally {
      setBusy(false);
    }
  }

  async function start() {
    if (!elevationToken) {
      setError('Önce step-up parola ile yükseltme yapın.');
      return;
    }
    if (!selectedTargetId) {
      setError('Allowlist’ten bir hedef site seçin.');
      return;
    }
    if (!confirmed || phrase.trim() !== CONFIRM_PHRASE) {
      setError('Onay kutusu ve onay ifadesi zorunludur.');
      return;
    }
    setBusy(true);
    setError(null);
    try {
      const res = await labFetch<{ executionId: string }>('api/admin/lab/executions', {
        method: 'POST',
        body: JSON.stringify({
          scenarioKey: selectedKey,
          confirmPhrase: CONFIRM_PHRASE,
          elevationToken,
          labTargetSiteId: selectedTargetId,
          assessmentModeName: 'IsolatedSecurityLab'
        })
      });
      setElevationToken(null);
      setConfirmed(false);
      setPhrase('');
      router.push(`/admin/lab/${res.executionId}`);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Başlatma başarısız');
    } finally {
      setBusy(false);
    }
  }

  async function refreshList() {
    try {
      const list = await labFetch<LabExecutionListItem[]>('api/admin/lab/executions');
      setExecutions(list);
    } catch {
      /* ignore */
    }
  }

  const enabledTargets = targets.filter((t) => t.isEnabled);

  return (
    <div className="space-y-8">
      <section className="space-y-3 rounded-lg border border-slate-200 bg-white p-5">
        <h2 className="text-lg font-semibold text-slate-900">IsolatedSecurityLab</h2>
        <p className="text-sm text-slate-600">
          Yalnızca önceden tanımlı senaryolar + allowlist hedefler. Kullanıcı URL/IP/payload
          giremez. Lab ağı internet çıkışına açıktır (deneme). Yetkili dış değerlendirme için
          AuthorizedExternalAssessment (doğrulanmış domain) kullanılır.
        </p>
      </section>

      <section className="space-y-4 rounded-lg border border-slate-200 bg-white p-5">
        <h2 className="text-lg font-semibold text-slate-900">Allowlist hedefler</h2>
        <div className="flex flex-wrap gap-2">
          <input
            className="min-w-[220px] flex-1 rounded-md border border-slate-300 px-3 py-2 text-sm"
            placeholder="example.com"
            value={newHost}
            onChange={(e) => setNewHost(e.target.value)}
          />
          <button
            type="button"
            disabled={busy || !newHost.trim()}
            onClick={addTarget}
            className="rounded-md bg-slate-800 px-4 py-2 text-sm font-medium text-white disabled:opacity-50"
          >
            Hedef ekle
          </button>
        </div>
        <ul className="space-y-1 text-sm text-slate-700">
          {enabledTargets.length === 0 && (
            <li className="text-slate-500">Henüz hedef yok — önce hostname ekleyin.</li>
          )}
          {enabledTargets.map((t) => (
            <li key={t.id}>
              <code>{t.normalizedHostName}</code>
            </li>
          ))}
        </ul>
      </section>

      <section className="space-y-4 rounded-lg border border-slate-200 bg-white p-5">
        <h2 className="text-lg font-semibold text-slate-900">Senaryo başlat</h2>
        <p className="text-sm text-slate-600">
          Onay ifadesi: <code className="rounded bg-slate-100 px-1 text-xs">{CONFIRM_PHRASE}</code>
        </p>

        <label className="block text-sm">
          <span className="font-medium text-slate-700">Hedef (allowlist)</span>
          <select
            className="mt-1 w-full rounded-md border border-slate-300 px-3 py-2"
            value={selectedTargetId}
            onChange={(e) => setSelectedTargetId(e.target.value)}
          >
            <option value="">Seçin…</option>
            {enabledTargets.map((t) => (
              <option key={t.id} value={t.id}>
                {t.normalizedHostName}
              </option>
            ))}
          </select>
        </label>

        <label className="block text-sm">
          <span className="font-medium text-slate-700">Senaryo</span>
          <select
            className="mt-1 w-full rounded-md border border-slate-300 px-3 py-2"
            value={selectedKey}
            onChange={(e) => setSelectedKey(e.target.value)}
          >
            {scenarios.map((s) => (
              <option key={s.scenarioKey} value={s.scenarioKey}>
                {s.titleTr}
                {s.isFullyImplemented ? '' : ' (iskelet)'}
              </option>
            ))}
          </select>
        </label>

        <div className="grid gap-3 md:grid-cols-2">
          <label className="block text-sm">
            <span className="font-medium text-slate-700">Step-up parola</span>
            <input
              type="password"
              className="mt-1 w-full rounded-md border border-slate-300 px-3 py-2"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              autoComplete="current-password"
            />
          </label>
          <div className="flex items-end">
            <button
              type="button"
              disabled={busy || !password}
              onClick={elevate}
              className="rounded-md bg-slate-800 px-4 py-2 text-sm font-medium text-white disabled:opacity-50"
            >
              Yükseltme bileti al
            </button>
          </div>
        </div>

        <label className="flex items-start gap-2 text-sm text-slate-700">
          <input
            type="checkbox"
            checked={confirmed}
            onChange={(e) => setConfirmed(e.target.checked)}
            className="mt-1"
          />
          <span>
            IsolatedSecurityLab senaryosunu, allowlist hedefte, aktif exploit olmadan
            çalıştıracağımı onaylıyorum.
          </span>
        </label>

        <label className="block text-sm">
          <span className="font-medium text-slate-700">Onay ifadesi</span>
          <input
            className="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 font-mono text-xs"
            value={phrase}
            onChange={(e) => setPhrase(e.target.value)}
            placeholder={CONFIRM_PHRASE}
          />
        </label>

        <button
          type="button"
          disabled={busy || !elevationToken}
          onClick={start}
          className="rounded-md bg-[color:var(--color-brand-600)] px-4 py-2 text-sm font-medium text-white disabled:opacity-50"
        >
          Laboratuvarı başlat
        </button>

        {error && <p className="text-sm text-red-600">{error}</p>}
        {info && <p className="text-sm text-emerald-700">{info}</p>}
      </section>

      <section className="space-y-3">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-semibold text-slate-900">Geçmiş oturumlar</h2>
          <button
            type="button"
            onClick={refreshList}
            className="text-sm text-[color:var(--color-brand-700)] hover:underline"
          >
            Yenile
          </button>
        </div>
        <div className="overflow-hidden rounded-lg border border-slate-200 bg-white">
          <table className="min-w-full text-left text-sm">
            <thead className="bg-slate-50 text-slate-600">
              <tr>
                <th className="px-3 py-2 font-medium">Senaryo</th>
                <th className="px-3 py-2 font-medium">Hedef</th>
                <th className="px-3 py-2 font-medium">Durum</th>
                <th className="px-3 py-2 font-medium" />
              </tr>
            </thead>
            <tbody>
              {executions.length === 0 && (
                <tr>
                  <td colSpan={4} className="px-3 py-6 text-center text-slate-500">
                    Henüz lab oturumu yok.
                  </td>
                </tr>
              )}
              {executions.map((e) => (
                <tr key={e.id} className="border-t border-slate-100">
                  <td className="px-3 py-2">{e.scenarioTitleTr}</td>
                  <td className="px-3 py-2 font-mono text-xs">{e.targetHostName}</td>
                  <td className="px-3 py-2">{statusLabel[e.status] ?? String(e.status)}</td>
                  <td className="px-3 py-2 text-right">
                    <Link
                      href={`/admin/lab/${e.id}`}
                      className="text-[color:var(--color-brand-700)] hover:underline"
                    >
                      Detay
                    </Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  );
}
