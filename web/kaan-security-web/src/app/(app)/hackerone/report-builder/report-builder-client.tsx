'use client';

import { useRouter } from 'next/navigation';
import { useMemo, useState } from 'react';
import type { Draft } from './page';

async function copyText(text: string) {
  await navigator.clipboard.writeText(text);
}

export function ReportBuilderClient({
  initialDraft,
  drafts,
  openHackerOneUrl
}: {
  initialDraft: Draft | null;
  drafts: Draft[];
  openHackerOneUrl: string;
}) {
  const router = useRouter();
  const [draft, setDraft] = useState<Draft | null>(initialDraft);
  const [msg, setMsg] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [reportTab, setReportTab] = useState<'en' | 'tr'>('en');
  const [preview, setPreview] = useState<string | null>(null);

  const fields = useMemo(
    () =>
      draft
        ? [
            ['Title', draft.title],
            ['Severity', draft.severity],
            ['Asset', draft.asset],
            ['Weakness', draft.weakness],
            ['Impact', draft.impact],
            ['Steps', draft.stepsToReproduce],
            ['PoC', draft.proofOfConcept],
            ['Notes', draft.notes || '']
          ]
        : [],
    [draft]
  );

  async function saveField(key: string, value: string) {
    if (!draft) return;
    setBusy(true);
    try {
      const body: Record<string, string> = {};
      const map: Record<string, string> = {
        Title: 'title',
        Severity: 'severity',
        Asset: 'asset',
        Weakness: 'weakness',
        Impact: 'impact',
        Steps: 'stepsToReproduce',
        PoC: 'proofOfConcept',
        Notes: 'notes'
      };
      body[map[key]] = value;
      const res = await fetch(`/api/backend/api/hackerone/drafts/${draft.id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
        body: JSON.stringify(body)
      });
      if (res.ok) {
        const updated = (await res.json()) as Draft;
        setDraft(updated);
        setMsg('Kaydedildi (EN alanlar; markdown yenilenir)');
      }
    } finally {
      setBusy(false);
    }
  }

  async function loadMarkdown(lang: 'en' | 'tr') {
    if (!draft) return null;
    const res = await fetch(
      `/api/backend/api/hackerone/drafts/${draft.id}/markdown?language=${lang}`
    );
    if (!res.ok) {
      setMsg('Markdown alınamadı');
      return null;
    }
    const data = (await res.json()) as {
      markdown: string;
      reportReadinessScore: number;
      language?: string;
      turkishMarkdown?: string | null;
    };
    setDraft({
      ...draft,
      markdownBody: lang === 'en' ? data.markdown : draft.markdownBody,
      turkishMarkdownBody: lang === 'tr' ? data.markdown : data.turkishMarkdown ?? draft.turkishMarkdownBody,
      reportReadinessScore: data.reportReadinessScore
    });
    setPreview(data.markdown);
    return data.markdown;
  }

  async function copyReport(lang: 'en' | 'tr') {
    const md = await loadMarkdown(lang);
    if (!md) return;
    await copyText(md);
    setReportTab(lang);
    setMsg(
      lang === 'en'
        ? 'EN FULL REPORT (HackerOne) panoya kopyalandı'
        : 'TR rapor panoya kopyalandı (yalnızca iç inceleme — H1’e yapıştırma)'
    );
  }

  async function submitConfirmed() {
    if (!draft) return;
    setBusy(true);
    setMsg(null);
    try {
      const res = await fetch(`/api/backend/api/hackerone/drafts/${draft.id}/submit`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
        body: JSON.stringify({ explicitConfirm: true })
      });
      const data = await res.json().catch(() => ({}));
      if (!res.ok) {
        setMsg(data.detail || data.title || 'Gönderim reddedildi');
      } else {
        setMsg(`Gönderildi: ${data.externalReportUrl || data.externalReportId}`);
        router.refresh();
      }
    } finally {
      setBusy(false);
      setConfirmOpen(false);
    }
  }

  return (
    <div className="space-y-4">
      <div className="rounded-md border border-sky-200 bg-sky-50 px-3 py-2 text-xs text-sky-900">
        <strong>EN (en-US)</strong> = HackerOne’a kopyala / gönder. <strong>TR</strong> = yalnızca iç
        inceleme; HackerOne formuna TR yapıştırma.
      </div>

      <div className="flex flex-wrap items-center gap-2">
        <select
          className="rounded border border-slate-300 px-2 py-1.5 text-sm"
          value={draft?.id || ''}
          onChange={(e) => {
            const id = e.target.value;
            if (id) router.push(`/hackerone/report-builder?draftId=${id}`);
          }}
        >
          <option value="">Taslak seç…</option>
          {drafts.map((d) => (
            <option key={d.id} value={d.id}>
              {d.title.slice(0, 60)} ({d.reportReadinessScore})
            </option>
          ))}
        </select>
        {draft && (
          <span className="text-xs text-slate-500">
            Readiness: {draft.reportReadinessScore} · {draft.status} · {draft.programHandle}
          </span>
        )}
      </div>

      {!draft && (
        <p className="text-sm text-slate-500">
          Targets’tan tara → Candidates’tan bulgu seç → Report Builder. Veya mevcut taslak seçin.
        </p>
      )}

      {draft && (
        <>
          <div className="flex gap-1 text-xs">
            <button
              type="button"
              onClick={() => {
                setReportTab('en');
                void loadMarkdown('en');
              }}
              className={`rounded-md border px-3 py-1.5 font-medium ${
                reportTab === 'en' ? 'border-slate-800 bg-slate-800 text-white' : 'bg-white'
              }`}
            >
              EN (HackerOne)
            </button>
            <button
              type="button"
              onClick={() => {
                setReportTab('tr');
                void loadMarkdown('tr');
              }}
              className={`rounded-md border px-3 py-1.5 font-medium ${
                reportTab === 'tr' ? 'border-slate-800 bg-slate-800 text-white' : 'bg-white'
              }`}
            >
              TR (iç inceleme)
            </button>
          </div>

          {preview && (
            <pre className="max-h-64 overflow-auto rounded-lg border border-slate-200 bg-slate-50 p-3 text-xs whitespace-pre-wrap">
              {preview}
            </pre>
          )}

          <div className="space-y-3">
            <p className="text-xs text-slate-500">
              Aşağıdaki alanlar EN HackerOne taslağıdır (düzenlenebilir). TR rapor ayrı üretilir.
            </p>
            {fields.map(([label, value]) => (
              <div key={label} className="rounded-lg border border-slate-200 bg-white/80 p-3">
                <div className="mb-1 flex items-center justify-between gap-2">
                  <label className="text-xs font-semibold uppercase tracking-wide text-slate-500">
                    {label}
                  </label>
                  <button
                    type="button"
                    className="text-xs font-medium text-[color:var(--color-brand-700)]"
                    onClick={() => copyText(value).then(() => setMsg(`${label} kopyalandı`))}
                  >
                    Copy
                  </button>
                </div>
                <textarea
                  className="w-full rounded border border-slate-200 px-2 py-1.5 text-sm"
                  rows={
                    label === 'Title' || label === 'Severity' || label === 'Asset' || label === 'Weakness'
                      ? 2
                      : 5
                  }
                  defaultValue={value}
                  key={`${draft.id}-${label}-${value.slice(0, 20)}`}
                  onBlur={(e) => {
                    if (e.target.value !== value) saveField(label, e.target.value);
                  }}
                />
              </div>
            ))}
          </div>

          <div className="flex flex-wrap gap-2">
            <button
              type="button"
              disabled={busy}
              onClick={() => copyReport('en')}
              className="rounded-md bg-[color:var(--color-brand-600)] px-4 py-2 text-sm font-medium text-white"
            >
              COPY EN (HackerOne)
            </button>
            <button
              type="button"
              disabled={busy}
              onClick={() => copyReport('tr')}
              className="rounded-md border border-slate-300 bg-white px-4 py-2 text-sm font-medium"
            >
              COPY TR (iç)
            </button>
            <a
              href={openHackerOneUrl}
              target="_blank"
              rel="noreferrer"
              className="rounded-md border border-slate-300 bg-white px-4 py-2 text-sm font-medium"
            >
              OPEN HACKERONE
            </a>
            <button
              type="button"
              disabled={busy}
              onClick={() => setConfirmOpen(true)}
              className="rounded-md border border-amber-400 bg-amber-50 px-4 py-2 text-sm font-medium text-amber-900 disabled:opacity-50"
            >
              API Submit (kapılı)
            </button>
          </div>
        </>
      )}

      {msg && <p className="text-sm text-slate-600">{msg}</p>}

      {confirmOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
          <div className="max-w-md rounded-lg bg-white p-5 shadow-lg">
            <h2 className="text-lg font-semibold text-slate-900">HackerOne API gönderimi</h2>
            <p className="mt-2 text-sm text-slate-600">
              Yalnızca <strong>EN</strong> rapor gönderilir. TR metin HackerOne’a gitmez. Emin
              misiniz?
            </p>
            <div className="mt-4 flex justify-end gap-2">
              <button
                type="button"
                className="rounded-md border px-3 py-1.5 text-sm"
                onClick={() => setConfirmOpen(false)}
              >
                İptal
              </button>
              <button
                type="button"
                disabled={busy}
                className="rounded-md bg-amber-600 px-3 py-1.5 text-sm font-medium text-white"
                onClick={submitConfirmed}
              >
                Evet, EN gönder
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
