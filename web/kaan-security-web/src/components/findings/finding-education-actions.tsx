'use client';

import { useEffect, useId, useState, type ReactNode } from 'react';
import Link from 'next/link';
import { BookOpen, Play, X } from 'lucide-react';
import { getFindingEducation, type FindingEducationEntry } from '@/lib/finding-education';

interface Props {
  fingerprint?: string | null;
}

type ModalKind = 'play' | 'attack' | null;

export function FindingEducationActions({ fingerprint }: Props) {
  const entry = getFindingEducation(fingerprint);
  const [modal, setModal] = useState<ModalKind>(null);
  const titleId = useId();

  useEffect(() => {
    if (!modal) return;
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setModal(null);
    };
    document.addEventListener('keydown', onKey);
    const prev = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    return () => {
      document.removeEventListener('keydown', onKey);
      document.body.style.overflow = prev;
    };
  }, [modal]);

  if (!entry) return null;

  return (
    <>
      <section className="rounded-2xl border border-amber-200 bg-amber-50/60 p-5 shadow-sm">
        <h2 className="text-sm font-semibold text-slate-800">Eğitim araçları</h2>
        <p className="mt-1 text-xs text-slate-600">
          Hedef siteye istek atılmaz. Yalnızca platform içi uyarı simülasyonu ve eğitim anlatımı.
        </p>
        <div className="mt-3 flex flex-wrap gap-2">
          <button
            type="button"
            onClick={() => setModal('play')}
            className="inline-flex items-center gap-2 rounded-md bg-amber-600 px-3 py-2 text-sm font-semibold text-white hover:bg-amber-700"
          >
            <Play size={14} fill="currentColor" />
            Uyarı demosu
          </button>
          <button
            type="button"
            onClick={() => setModal('attack')}
            className="inline-flex items-center gap-2 rounded-md border border-slate-300 bg-white px-3 py-2 text-sm font-semibold text-slate-800 hover:bg-slate-50"
          >
            <BookOpen size={14} />
            Nasıl atak yapılır?
          </button>
        </div>
      </section>

      {modal === 'play' && (
        <Lightbox titleId={titleId} title={entry.playDemo.title} onClose={() => setModal(null)}>
          <PlayDemoBody entry={entry} />
        </Lightbox>
      )}
      {modal === 'attack' && (
        <Lightbox titleId={titleId} title="Nasıl atak yapılır?" onClose={() => setModal(null)}>
          <AttackExplainerBody entry={entry} />
        </Lightbox>
      )}
    </>
  );
}

function Lightbox({
  titleId,
  title,
  onClose,
  children
}: {
  titleId: string;
  title: string;
  onClose: () => void;
  children: ReactNode;
}) {
  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 p-4"
      role="dialog"
      aria-modal="true"
      aria-labelledby={titleId}
      onClick={onClose}
    >
      <div
        className="relative max-h-[85vh] w-full max-w-2xl overflow-y-auto rounded-2xl border border-slate-200 bg-white shadow-xl"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="sticky top-0 flex items-center justify-between border-b border-slate-100 bg-white px-5 py-3">
          <h3 id={titleId} className="text-base font-semibold text-slate-900">
            {title}
          </h3>
          <button
            type="button"
            onClick={onClose}
            className="rounded-md p-1.5 text-slate-500 hover:bg-slate-100 hover:text-slate-800"
            aria-label="Kapat"
          >
            <X size={18} />
          </button>
        </div>
        <div className="px-5 py-4">{children}</div>
      </div>
    </div>
  );
}

function PlayDemoBody({ entry }: { entry: FindingEducationEntry }) {
  const demo = entry.playDemo;
  return (
    <div className="space-y-4">
      <div className="rounded-md border border-amber-200 bg-amber-50 px-3 py-2 text-xs text-amber-900">
        {demo.warningBanner}
      </div>
      <div className="grid gap-3 md:grid-cols-2">
        <div className="rounded-xl border border-rose-200 bg-rose-50 p-4">
          <div className="text-xs font-semibold uppercase tracking-wide text-rose-700">
            {demo.withoutLabel}
          </div>
          <p className="mt-2 text-sm text-rose-950">{demo.withoutBody}</p>
          <div className="mt-3 rounded-md border border-rose-300 bg-white/80 px-3 py-2 text-xs font-medium text-rose-800">
            Uyarı bandı (simülasyon) — tahrip yok, yalnızca bilgilendirme.
          </div>
        </div>
        <div className="rounded-xl border border-emerald-200 bg-emerald-50 p-4">
          <div className="text-xs font-semibold uppercase tracking-wide text-emerald-700">
            {demo.withLabel}
          </div>
          <p className="mt-2 text-sm text-emerald-950">{demo.withBody}</p>
          <div className="mt-3 rounded-md border border-emerald-300 bg-white/80 px-3 py-2 text-xs font-medium text-emerald-800">
            Koruma etkin — aynı senaryo engellenirdi.
          </div>
        </div>
      </div>
    </div>
  );
}

function AttackExplainerBody({ entry }: { entry: FindingEducationEntry }) {
  return (
    <div className="space-y-4">
      <p className="text-xs text-slate-500">
        Genel eğitim anlatımıdır. Canlı payload, exploit veya hedefe yönelik saldırı adımı içermez.
      </p>
      {entry.attackExplainer.map((section) => (
        <div key={section.heading}>
          <h4 className="text-sm font-semibold text-slate-800">{section.heading}</h4>
          <p className="mt-1 text-sm leading-relaxed text-slate-700">{section.body}</p>
        </div>
      ))}
      {entry.knowledgeSlug && (
        <Link
          href={`/knowledge/article/${entry.knowledgeSlug}`}
          className="inline-flex text-sm font-semibold text-[color:var(--color-brand-700)] hover:underline"
        >
          Bilgi bankasında oku →
        </Link>
      )}
    </div>
  );
}
