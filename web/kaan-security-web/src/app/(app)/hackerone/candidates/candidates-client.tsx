'use client';

import { useRouter } from 'next/navigation';
import { useMemo, useState } from 'react';
import type { Candidate } from './page';

export interface DomainOption {
  id: string;
  hostName: string;
  isVerified: boolean;
}

export function CandidatesClient({
  initial,
  domains
}: {
  initial: Candidate[];
  domains: DomainOption[];
}) {
  const router = useRouter();
  const verified = useMemo(() => domains.filter((d) => d.isVerified), [domains]);
  const [selectedId, setSelectedId] = useState(verified[0]?.id ?? '');
  const [hostName, setHostName] = useState(verified[0]?.hostName ?? '');
  const [busy, setBusy] = useState(false);
  const [msg, setMsg] = useState<string | null>(null);

  function onSelectDomain(id: string) {
    setSelectedId(id);
    const match = domains.find((d) => d.id === id);
    if (match) {
      setHostName(match.hostName);
    }
  }

  async function startCandidate() {
    const host = hostName.trim();
    if (!host && !selectedId) {
      setMsg('Domain adı yazın veya listeden seçin (doğrulanmış olmalı).');
      return;
    }
    setBusy(true);
    setMsg(null);
    try {
      const body: { hostName?: string; domainAssetId?: string } = {};
      if (host) {
        body.hostName = host;
      } else if (selectedId) {
        body.domainAssetId = selectedId;
      }

      const res = await fetch('/api/backend/api/hackerone/candidate-assessment', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
        body: JSON.stringify(body)
      });
      const data = await res.json().catch(() => ({}));
      if (!res.ok) {
        setMsg(data.detail || data.title || 'Başlatılamadı');
      } else {
        setMsg(`Candidate assessment kuyruğa alındı. Tarama: ${data.scanJobId}`);
        router.refresh();
      }
    } catch {
      setMsg('İstek başarısız');
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="rounded-lg border border-slate-200 bg-slate-50/80 px-4 py-3">
      <div className="text-sm font-medium text-slate-800">
        Application Security Candidate ({initial.length} listelenen)
      </div>
      <p className="mt-1 text-xs text-slate-500">
        Domain adı yazın (örn. amazon.com) veya doğrulanmış domain seçin. Güvenli motorlar çalışır;
        agresif payload yok. Domain önce platformda ekli ve doğrulanmış olmalı.
      </p>
      <div className="mt-2 flex flex-wrap gap-2">
        {verified.length > 0 && (
          <select
            className="min-w-[200px] rounded border border-slate-300 bg-white px-2 py-1.5 text-sm"
            value={selectedId}
            onChange={(e) => onSelectDomain(e.target.value)}
          >
            <option value="">Domain seç…</option>
            {verified.map((d) => (
              <option key={d.id} value={d.id}>
                {d.hostName}
              </option>
            ))}
          </select>
        )}
        <input
          className="min-w-[220px] flex-1 rounded border border-slate-300 px-2 py-1.5 text-sm"
          placeholder="örn. amazon.com"
          value={hostName}
          onChange={(e) => {
            setHostName(e.target.value);
            setSelectedId('');
          }}
        />
        <button
          type="button"
          disabled={busy}
          onClick={startCandidate}
          className="rounded-md border border-slate-300 bg-white px-3 py-1.5 text-sm font-medium disabled:opacity-50"
        >
          {busy ? 'Başlatılıyor…' : 'Candidate assessment başlat'}
        </button>
      </div>
      {verified.length === 0 && (
        <p className="mt-2 text-xs text-amber-700">
          Doğrulanmış domain yok. Domainler sayfasından ekleyip manuel/otomatik doğrulayın.
        </p>
      )}
      {msg && <p className="mt-2 text-xs text-slate-600">{msg}</p>}
    </div>
  );
}
