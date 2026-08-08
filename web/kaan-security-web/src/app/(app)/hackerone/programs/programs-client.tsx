'use client';

import { useRouter } from 'next/navigation';
import { useState } from 'react';

export function ProgramsClient() {
  const router = useRouter();
  const [msg, setMsg] = useState<string | null>(null);
  const [busy, setBusy] = useState<'programs' | 'scopes' | null>(null);

  async function syncPrograms() {
    setBusy('programs');
    setMsg(null);
    try {
      const res = await fetch('/api/backend/api/hackerone/programs/sync', { method: 'POST' });
      const data = await res.json().catch(() => ({}));
      if (!res.ok) {
        setMsg(data.detail || data.title || 'Sync reddedildi (API kapalı olabilir)');
      } else {
        setMsg(`Program senkronize: ${data.synced}`);
        router.refresh();
      }
    } finally {
      setBusy(null);
    }
  }

  async function syncScopes() {
    setBusy('scopes');
    setMsg(null);
    try {
      const res = await fetch('/api/backend/api/hackerone/domains/sync-scopes', { method: 'POST' });
      const data = await res.json().catch(() => ({}));
      if (!res.ok && res.status !== 202) {
        setMsg(
          data.detail ||
            data.title ||
            'Scope sync başlatılamadı — Settings’te token/identifier kontrol edin (401 = hatalı kimlik).'
        );
      } else {
        setMsg(data.message || `Scope sync kuyruğa alındı${data.jobId ? ` (${data.jobId})` : ''}`);
        router.refresh();
      }
    } finally {
      setBusy(null);
    }
  }

  return (
    <div className="space-y-2">
      <div className="flex flex-wrap items-center gap-2">
        <button
          type="button"
          disabled={busy !== null}
          onClick={syncPrograms}
          className="rounded-md border border-slate-300 bg-white px-3 py-1.5 text-sm font-medium disabled:opacity-50"
        >
          {busy === 'programs' ? 'Sync…' : 'Program listesini sync'}
        </button>
        <button
          type="button"
          disabled={busy !== null}
          onClick={syncScopes}
          className="rounded-md bg-slate-900 px-3 py-1.5 text-sm font-semibold text-white disabled:opacity-50"
        >
          {busy === 'scopes' ? 'Kuyruğa alınıyor…' : 'Tüm scope → Domainler'}
        </button>
        <a href="/domains" className="text-sm text-[color:var(--color-brand-700)] underline">
          Domainler
        </a>
      </div>
      {msg && <p className="text-xs text-slate-600">{msg}</p>}
      <p className="text-xs text-slate-500">
        Scope sync tüm programları ve in-scope domainleri Domains altına ekler (arka plan). Kesin $ tutarı API&apos;de
        yoktur; bounty eligible / currency / max severity özeti yazılır.
      </p>
    </div>
  );
}
