'use client';

import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useEffect, useState } from 'react';
import { BbEligibleBadge, BbEligibleChime } from '@/components/findings/bb-eligible-badge';
import { formatDateTr, severityColor } from '@/lib/utils';

export interface ScanDetail {
  id: string;
  domainHostName: string;
  scanType: string;
  status: string;
  progressPercentage: number;
  totalSteps: number;
  completedSteps: number;
  currentStep: string | null;
  errorMessage: string | null;
  isRetest: boolean;
  startedAt: string | null;
  completedAt: string | null;
  result: {
    id: string;
    securityScore: number;
    previousSecurityScore: number;
    criticalCount: number;
    highCount: number;
    mediumCount: number;
    lowCount: number;
    infoCount: number;
    executiveSummary: string | null;
    summary: string | null;
    checksTotal: number;
    checksPassed: number;
    checksFailed: number;
  } | null;
}

export interface FindingItem {
  id: string;
  title: string;
  severity: string;
  technicalSeverity?: string;
  bugBountyEligible?: boolean;
  findingClass?: string;
  category: string;
  status: string;
  affectedUrl?: string;
}

interface Props {
  initialScan: ScanDetail;
  initialFindings: FindingItem[];
}

const ACTIVE = new Set(['Queued', 'Running', '0', '1']);

export function ScanDetailClient({ initialScan, initialFindings }: Props) {
  const router = useRouter();
  const [scan, setScan] = useState(initialScan);
  const [findings, setFindings] = useState(initialFindings);

  useEffect(() => {
    setScan(initialScan);
    setFindings(initialFindings);
  }, [initialScan, initialFindings]);

  useEffect(() => {
    if (!ACTIVE.has(scan.status)) {
      return;
    }

    let cancelled = false;
    const tick = async () => {
      try {
        const res = await fetch(`/api/backend/api/scans/${scan.id}/progress`, { cache: 'no-store' });
        if (!res.ok || cancelled) return;
        const p = (await res.json()) as {
          status: string;
          progressPercentage: number;
          currentStep: string | null;
          completedSteps: number;
          totalSteps: number;
        };
        setScan((prev) => ({
          ...prev,
          status: p.status,
          progressPercentage: p.progressPercentage,
          currentStep: p.currentStep,
          completedSteps: p.completedSteps,
          totalSteps: p.totalSteps
        }));
        if (!ACTIVE.has(p.status)) {
          router.refresh();
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
  }, [scan.id, scan.status, router]);

  return (
    <div className="space-y-6">
      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">{scan.domainHostName}</h1>
          <div className="mt-1 text-xs text-slate-500">
            {scan.scanType} · {scan.isRetest ? 'Yeniden test' : 'İlk tarama'}
          </div>
        </div>
        <div className="flex flex-col items-end gap-2">
          {scan.result && (
            <>
              <div className="flex flex-wrap justify-end gap-1.5">
                <a
                  href={`/api/backend/api/reports/${scan.id}?format=html&lang=tr`}
                  target="_blank"
                  className="rounded-md border border-slate-200 bg-white px-2.5 py-1.5 text-xs font-semibold text-slate-700 hover:bg-slate-50"
                  rel="noreferrer"
                >
                  HTML · TR
                </a>
                <a
                  href={`/api/backend/api/reports/${scan.id}?format=html&lang=en`}
                  target="_blank"
                  className="rounded-md border border-slate-200 bg-white px-2.5 py-1.5 text-xs font-semibold text-slate-700 hover:bg-slate-50"
                  rel="noreferrer"
                >
                  HTML · EN
                </a>
              </div>
              <div className="flex flex-wrap justify-end gap-1.5">
                <a
                  href={`/api/backend/api/reports/${scan.id}?format=txt&lang=tr`}
                  target="_blank"
                  className="rounded-md border border-slate-800 bg-slate-800 px-2.5 py-1.5 text-xs font-semibold text-white hover:bg-slate-900"
                  rel="noreferrer"
                >
                  TXT firmaya · TR
                </a>
                <a
                  href={`/api/backend/api/reports/${scan.id}?format=txt&lang=en`}
                  target="_blank"
                  className="rounded-md border border-slate-800 bg-slate-800 px-2.5 py-1.5 text-xs font-semibold text-white hover:bg-slate-900"
                  rel="noreferrer"
                >
                  TXT vendor · EN
                </a>
              </div>
            </>
          )}
        </div>
      </div>

      <section className="grid grid-cols-1 gap-4 md:grid-cols-4">
        <Card label="Durum" value={scan.status} />
        <Card
          label="İlerleme"
          value={`%${scan.progressPercentage}`}
          hint={scan.currentStep ?? undefined}
        />
        <Card label="Adımlar" value={`${scan.completedSteps}/${scan.totalSteps}`} />
        <Card
          label="Puan"
          value={scan.result ? `${scan.result.securityScore}/100` : '—'}
          hint={
            scan.result && scan.result.previousSecurityScore > 0
              ? `Önce: ${scan.result.previousSecurityScore}/100`
              : undefined
          }
        />
      </section>

      {scan.result && (
        <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
          <h2 className="text-sm font-semibold text-slate-800">Yönetici Özeti</h2>
          <p className="mt-2 whitespace-pre-line text-sm text-slate-700">
            {scan.result.executiveSummary ?? scan.result.summary ?? 'Özet bulunamadı.'}
          </p>
          <div className="mt-4 grid grid-cols-2 gap-3 md:grid-cols-5">
            <StatChip label="Kritik" value={scan.result.criticalCount} color="red" />
            <StatChip label="Yüksek" value={scan.result.highCount} color="orange" />
            <StatChip label="Orta" value={scan.result.mediumCount} color="amber" />
            <StatChip label="Düşük" value={scan.result.lowCount} color="yellow" />
            <StatChip label="Bilgi" value={scan.result.infoCount} color="slate" />
          </div>
        </section>
      )}

      <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
        <div className="mb-3 flex items-center justify-between">
          <h2 className="text-sm font-semibold text-slate-800">Bulgular</h2>
          <Link href="/findings" className="text-xs text-[color:var(--color-brand-600)]">
            Tüm bulgular
          </Link>
        </div>
        <BbEligibleChime play={findings.some((f) => f.bugBountyEligible)} />
        {findings.some((f) => f.bugBountyEligible) && (
          <div className="mb-3">
            <BbEligibleBadge eligible />
          </div>
        )}
        {findings.length === 0 ? (
          <p className="text-sm text-slate-500">
            {scan.result ? 'Bu taramaya ait bulgu yok veya henüz yüklenmedi.' : 'Tarama henüz tamamlanmadı.'}
          </p>
        ) : (
          <ul className="space-y-2">
            {findings.map((f) => (
              <li
                key={f.id}
                className={`flex items-center justify-between rounded-md border p-3 text-sm ${
                  f.bugBountyEligible
                    ? 'border-emerald-400 bg-emerald-50 text-emerald-950'
                    : severityColor(f.technicalSeverity ?? f.severity)
                }`}
              >
                <div>
                  <div className="flex flex-wrap items-center gap-2 font-semibold">
                    {f.title}
                    <BbEligibleBadge eligible={f.bugBountyEligible} compact />
                  </div>
                  <div className="text-xs opacity-80">
                    {f.findingClass ?? f.category} · {f.affectedUrl ?? '—'}
                  </div>
                </div>
                <Link
                  href={`/findings/${f.id}`}
                  className="text-xs font-semibold hover:underline"
                >
                  Detay →
                </Link>
              </li>
            ))}
          </ul>
        )}
      </section>

      {scan.errorMessage && (
        <div className="rounded-md border border-rose-200 bg-rose-50 p-3 text-sm text-rose-700">
          {scan.errorMessage}
        </div>
      )}
      <div className="text-xs text-slate-500">
        Başladı: {formatDateTr(scan.startedAt)} · Tamamlandı: {formatDateTr(scan.completedAt)}
      </div>
    </div>
  );
}

function Card({ label, value, hint }: { label: string; value: string; hint?: string }) {
  return (
    <div className="rounded-2xl border border-slate-200 bg-white p-4 shadow-sm">
      <div className="text-xs text-slate-500">{label}</div>
      <div className="mt-1 text-lg font-semibold text-slate-800">{value}</div>
      {hint && <div className="mt-0.5 text-[11px] text-slate-500">{hint}</div>}
    </div>
  );
}

function StatChip({
  label,
  value,
  color
}: {
  label: string;
  value: number;
  color: 'red' | 'orange' | 'amber' | 'yellow' | 'slate';
}) {
  const map: Record<string, string> = {
    red: 'bg-red-50 text-red-700 border-red-200',
    orange: 'bg-orange-50 text-orange-700 border-orange-200',
    amber: 'bg-amber-50 text-amber-700 border-amber-200',
    yellow: 'bg-yellow-50 text-yellow-700 border-yellow-200',
    slate: 'bg-slate-50 text-slate-700 border-slate-200'
  };
  return (
    <div className={`rounded-md border p-2 text-center ${map[color]}`}>
      <div className="text-xs">{label}</div>
      <div className="text-lg font-bold">{value}</div>
    </div>
  );
}
