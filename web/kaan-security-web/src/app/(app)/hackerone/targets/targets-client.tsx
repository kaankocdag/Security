'use client';

import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { Fragment, useEffect, useMemo, useRef, useState } from 'react';
import { AuthenticatedScanPanel } from '@/components/hackerone/authenticated-scan-panel';
import { playFunnyFindingAlarm, playMoneyBountyAlarm, unlockAudio } from '@/lib/funny-alarm';

export interface TargetDomain {
  id: string;
  hostName: string;
  securityProjectId: string;
  status: string;
  isVerified: boolean;
  source?: string;
  hackerOneProgramHandle?: string | null;
  hackerOneProgramName?: string | null;
  hackerOneEligibleForBounty?: boolean | null;
  hackerOneOffersBounties?: boolean | null;
  hackerOneCurrency?: string | null;
  hackerOneMaxSeverity?: string | null;
  hackerOneBountySummary?: string | null;
  hackerOneIsWildcard?: boolean;
}

export interface TargetCandidate {
  findingId: string;
  title: string;
  technicalSeverity: string;
  findingClass: string;
  submissionRecommendation: string;
  bugBountyEligible: boolean;
  demonstratedImpact: boolean;
  confirmedVulnerability?: boolean;
  submissionEligible?: boolean;
  potentialRewardEligible?: boolean;
  latestValidationStatus?: string | null;
  domainHostName?: string | null;
  fingerprint?: string | null;
  eligibilityReason?: string | null;
  lastSeenAt: string;
}

interface AssessmentFinding {
  findingId: string;
  title: string;
  severity: string;
  findingClass: string;
  submissionRecommendation: string;
  affectedUrl?: string | null;
  checkCode?: string | null;
  fingerprint?: string | null;
  category?: string | null;
}

interface AssessmentSummary {
  domainAssetId: string;
  hostName: string;
  scanJobId: string;
  scanResultId?: string | null;
  status: string;
  completedAt?: string | null;
  securityScore: number;
  summary?: string | null;
  executiveSummary?: string | null;
  checksTotal: number;
  checksPassed: number;
  checksFailed: number;
  criticalCount: number;
  highCount: number;
  mediumCount: number;
  lowCount: number;
  infoCount: number;
  enginesRun: string[];
  findings: AssessmentFinding[];
}

type FilterMode = 'all' | 'marked' | 'scanned' | 'candidate' | 'money';

interface PersistedState {
  marked: string[];
  scanned: string[];
  moneyDomainIds: string[];
  candidateDomainIds: string[];
  /** domainId → findingIds with money potential */
  moneyFindings: Record<string, string[]>;
  candidateFindings: Record<string, string[]>;
  /** domainId → last ASC scanJobId (rapor indirme) */
  scanJobIds?: Record<string, string>;
}

const STATE_KEY = 'h1-targets-workspace-v1';

function sleep(ms: number) {
  return new Promise((r) => setTimeout(r, ms));
}

function isScannable(d: TargetDomain) {
  if (d.status === 'Archived' || d.status === '3') return false;
  if (d.hackerOneIsWildcard || d.hostName.includes('*')) return false;
  return true;
}

function isTerminal(status: string) {
  return ['Completed', 'Failed', 'Cancelled', 'PartiallyCompleted'].includes(status);
}

function hostKey(h: string) {
  return h.trim().toLowerCase();
}

function isAccessControlCandidate(c: TargetCandidate) {
  return (
    c.fingerprint?.startsWith('asc.access.') === true ||
    c.title.toLowerCase().includes('accesscontrol') ||
    c.title.toLowerCase().includes('access-control') ||
    c.title.toLowerCase().includes('access control')
  );
}

function isXssCandidate(c: TargetCandidate) {
  return (
    c.fingerprint?.includes('xss') === true ||
    c.title.toLowerCase().includes('xss') ||
    c.title.toLowerCase().includes('reflected input')
  );
}

/** Aday sinyal — doğrulama öncesi ManualReview / VulnerabilityCandidate. */
function isCandidateSignal(c: TargetCandidate) {
  if (c.potentialRewardEligible || c.submissionEligible) return false;
  if (c.confirmedVulnerability && c.demonstratedImpact) return false;
  return (
    c.submissionRecommendation === 'ManualReview' ||
    c.findingClass === 'VulnerabilityCandidate' ||
    c.latestValidationStatus === 'CandidateOnly' ||
    c.latestValidationStatus === 'ManualReviewRequired'
  );
}

/**
 * Para sinyali — yalnızca Finding Validation sonrası:
 * Confirmed + DemonstratedImpact + SubmissionEligible/PotentialRewardEligible.
 * Path probe / aday tek başına ASLA para değil. Reward guaranteed değil.
 */
function isMoneyCandidate(c: TargetCandidate, _domain?: TargetDomain) {
  if (c.potentialRewardEligible === true && c.submissionEligible === true) return true;
  if (c.confirmedVulnerability === true && c.demonstratedImpact === true && c.submissionEligible === true) {
    return true;
  }
  return false;
}

function isReportableCandidate(c: TargetCandidate) {
  return (
    c.submissionRecommendation === 'Submit' ||
    c.submissionRecommendation === 'ManualReview' ||
    isCandidateSignal(c) ||
    isMoneyCandidate(c)
  );
}

function loadState(): PersistedState {
  try {
    const raw = localStorage.getItem(STATE_KEY);
    if (!raw) {
      return {
        marked: [],
        scanned: [],
        moneyDomainIds: [],
        candidateDomainIds: [],
        moneyFindings: {},
        candidateFindings: {},
        scanJobIds: {}
      };
    }
    const parsed = JSON.parse(raw) as PersistedState;
    return {
      marked: parsed.marked || [],
      scanned: parsed.scanned || [],
      moneyDomainIds: parsed.moneyDomainIds || [],
      candidateDomainIds: parsed.candidateDomainIds || [],
      moneyFindings: parsed.moneyFindings || {},
      candidateFindings: parsed.candidateFindings || {},
      scanJobIds: parsed.scanJobIds || {}
    };
  } catch {
    return {
      marked: [],
      scanned: [],
      moneyDomainIds: [],
      candidateDomainIds: [],
      moneyFindings: {},
      candidateFindings: {},
      scanJobIds: {}
    };
  }
}

function saveState(s: PersistedState) {
  localStorage.setItem(STATE_KEY, JSON.stringify(s));
}

export function HackerOneTargetsClient({
  initialDomains,
  initialCandidates
}: {
  initialDomains: TargetDomain[];
  initialCandidates: TargetCandidate[];
}) {
  const router = useRouter();
  const [domains, setDomains] = useState(initialDomains);
  const [candidates, setCandidates] = useState(initialCandidates);
  const [marked, setMarked] = useState<Set<string>>(() => new Set());
  const [scannedIds, setScannedIds] = useState<Set<string>>(() => new Set());
  const [moneyIds, setMoneyIds] = useState<Set<string>>(() => new Set());
  const [candidateIds, setCandidateIds] = useState<Set<string>>(() => new Set());
  const [moneyFindings, setMoneyFindings] = useState<Record<string, string[]>>({});
  const [candidateFindings, setCandidateFindings] = useState<Record<string, string[]>>({});
  const [scanningIds, setScanningIds] = useState<Set<string>>(() => new Set());
  const [jackpotId, setJackpotId] = useState<string | null>(null);
  const [concurrency, setConcurrency] = useState(2);
  const [scanBusy, setScanBusy] = useState(false);
  const [syncBusy, setSyncBusy] = useState(false);
  const [msg, setMsg] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [filter, setFilter] = useState<FilterMode>('all');
  const [onlyBountyEligible, setOnlyBountyEligible] = useState(true);
  const [batchSize, setBatchSize] = useState(10);
  const [skipScanned, setSkipScanned] = useState(true);
  const [manualHost, setManualHost] = useState('');
  const [manualAuthorized, setManualAuthorized] = useState(false);
  const [manualBusy, setManualBusy] = useState(false);
  const [scanJobIds, setScanJobIds] = useState<Record<string, string>>({});
  const [assessments, setAssessments] = useState<Record<string, AssessmentSummary>>({});
  const [expandedId, setExpandedId] = useState<string | null>(null);
  const stopRef = useRef(false);
  const statsRef = useRef({ ok: 0, fail: 0, money: 0, candidate: 0, done: 0, total: 0 });

  useEffect(() => {
    setDomains(initialDomains);
  }, [initialDomains]);

  useEffect(() => {
    setCandidates(initialCandidates);
  }, [initialCandidates]);

  // Restore + merge server candidates
  useEffect(() => {
    const s = loadState();
    setMarked(new Set(s.marked));
    setScannedIds(new Set(s.scanned));
    setMoneyIds(new Set(s.moneyDomainIds));
    setCandidateIds(new Set(s.candidateDomainIds));
    setMoneyFindings(s.moneyFindings || {});
    setCandidateFindings(s.candidateFindings || {});
    setScanJobIds(s.scanJobIds || {});
  }, []);

  async function loadAssessment(domainId: string): Promise<AssessmentSummary | null> {
    try {
      const res = await fetch(`/api/backend/api/hackerone/targets/${domainId}/latest-assessment`);
      if (!res.ok) return null;
      const data = (await res.json()) as AssessmentSummary;
      setAssessments((prev) => ({ ...prev, [domainId]: data }));
      setScanJobIds((prev) => {
        const next = { ...prev, [domainId]: data.scanJobId };
        const s = loadState();
        saveState({
          ...s,
          scanJobIds: next
        });
        return next;
      });
      return data;
    } catch {
      return null;
    }
  }

  async function downloadSecurityReport(scanJobId: string, hostName: string, format: 'html' | 'txt' = 'html') {
    try {
      const res = await fetch(`/api/backend/api/reports/${scanJobId}?format=${format}&lang=tr`);
      if (!res.ok) {
        const data = await res.json().catch(() => ({}));
        setError(data.detail || data.title || 'Rapor indirilemedi');
        return;
      }
      const blob = await res.blob();
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `kaan-security-${hostName.replace(/[^a-zA-Z0-9.-]/g, '_')}.${format}`;
      a.click();
      URL.revokeObjectURL(url);
      setMsg(`Güvenlik raporu indirildi: ${hostName}`);
    } catch {
      setError('Rapor indirme ağ hatası');
    }
  }

  // Taranmış hedefler için özetleri yükle (sayfa yenilenince “neler tarandı” görünsün)
  useEffect(() => {
    const ids = [...scannedIds].slice(0, 40);
    if (ids.length === 0) return;
    let cancelled = false;
    void (async () => {
      for (const id of ids) {
        if (cancelled) break;
        if (assessments[id]) continue;
        await loadAssessment(id);
      }
    })();
    return () => {
      cancelled = true;
    };
    // assessments kasıtlı bağımlılık değil — sonsuz döngü önlenir
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [scannedIds]);

  useEffect(() => {
    if (domains.length === 0) return;
    const byHost = new Map(domains.map((d) => [hostKey(d.hostName), d]));
    const s = loadState();
    const nextMoney = new Set<string>();
    const nextCandidate = new Set<string>();
    const nextMoneyFindings: Record<string, string[]> = {};
    const nextCandidateFindings: Record<string, string[]> = {};
    for (const c of candidates) {
      if (!c.domainHostName) continue;
      const d = byHost.get(hostKey(c.domainHostName));
      if (!d) continue;
      if (isMoneyCandidate(c, d)) {
        nextMoney.add(d.id);
        const list = nextMoneyFindings[d.id] ? [...nextMoneyFindings[d.id]!] : [];
        if (!list.includes(c.findingId)) list.push(c.findingId);
        nextMoneyFindings[d.id] = list;
      } else if (isCandidateSignal(c)) {
        nextCandidate.add(d.id);
        const list = nextCandidateFindings[d.id] ? [...nextCandidateFindings[d.id]!] : [];
        if (!list.includes(c.findingId)) list.push(c.findingId);
        nextCandidateFindings[d.id] = list;
      }
    }
    setMoneyIds(nextMoney);
    setCandidateIds(nextCandidate);
    setMoneyFindings(nextMoneyFindings);
    setCandidateFindings(nextCandidateFindings);
    saveState({
      marked: s.marked,
      scanned: s.scanned,
      moneyDomainIds: [...nextMoney],
      candidateDomainIds: [...nextCandidate],
      moneyFindings: nextMoneyFindings,
      candidateFindings: nextCandidateFindings,
      scanJobIds: s.scanJobIds || {}
    });
  }, [candidates, domains]);

  function persist(
    m: Set<string>,
    scanned: Set<string>,
    money: Set<string>,
    findings: Record<string, string[]>,
    cand: Set<string> = candidateIds,
    candFindings: Record<string, string[]> = candidateFindings,
    jobs: Record<string, string> = scanJobIds
  ) {
    saveState({
      marked: [...m],
      scanned: [...scanned],
      moneyDomainIds: [...money],
      candidateDomainIds: [...cand],
      moneyFindings: findings,
      candidateFindings: candFindings,
      scanJobIds: jobs
    });
  }

  function toggleMark(id: string) {
    const next = new Set(marked);
    if (next.has(id)) next.delete(id);
    else next.add(id);
    setMarked(next);
    persist(next, scannedIds, moneyIds, moneyFindings);
  }

  const baseList = useMemo(() => {
    let list = domains.filter(isScannable);
    if (onlyBountyEligible) {
      list = list.filter(
        (d) => d.hackerOneProgramHandle === 'manual' || (d.hackerOneEligibleForBounty && d.hackerOneOffersBounties)
      );
    }
    return list;
  }, [domains, onlyBountyEligible]);

  const visible = useMemo(() => {
    switch (filter) {
      case 'marked':
        return baseList.filter((d) => marked.has(d.id));
      case 'scanned':
        return baseList.filter((d) => scannedIds.has(d.id));
      case 'candidate':
        return baseList.filter((d) => candidateIds.has(d.id));
      case 'money':
        return baseList.filter((d) => moneyIds.has(d.id));
      default:
        return baseList;
    }
  }, [baseList, filter, marked, scannedIds, moneyIds, candidateIds]);

  const markedQueue = baseList.filter((d) => marked.has(d.id));

  async function refreshCandidates(): Promise<TargetCandidate[]> {
    const res = await fetch('/api/backend/api/hackerone/candidates');
    if (!res.ok) return candidates;
    const list = (await res.json()) as TargetCandidate[];
    setCandidates(list);
    return list;
  }

  async function addManualTarget() {
    const host = manualHost.trim();
    if (!host) {
      setError('Bir alan adı girin (örn. example.com).');
      return;
    }
    if (!manualAuthorized) {
      setError('Bu hedefi test etmeye yetkili olduğunuzu onaylayın.');
      return;
    }
    setManualBusy(true);
    setError(null);
    try {
      const res = await fetch('/api/backend/api/hackerone/targets/manual', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ hostName: host, authorizedConfirmed: true })
      });
      const data = await res.json().catch(() => ({}));
      if (!res.ok) {
        setError(data.detail || data.title || 'Hedef eklenemedi');
        return;
      }
      setMsg(`Hedef eklendi: ${host}. Satırdaki “Girişli Tarama” ile giriş yapıp tarayabilirsiniz.`);
      setManualHost('');
      setManualAuthorized(false);
      router.refresh();
    } catch {
      setError('Hedef eklenirken ağ hatası');
    } finally {
      setManualBusy(false);
    }
  }

  async function syncScopes() {
    setSyncBusy(true);
    setError(null);
    try {
      const res = await fetch('/api/backend/api/hackerone/domains/sync-scopes', { method: 'POST' });
      const data = await res.json().catch(() => ({}));
      if (!res.ok && res.status !== 202) {
        setError(data.detail || data.title || 'Sync başlatılamadı');
        return;
      }
      setMsg(data.message || 'Scope sync kuyruğa alındı');
      router.refresh();
    } finally {
      setSyncBusy(false);
    }
  }

  async function waitScan(scanJobId: string) {
    for (let i = 0; i < 200; i++) {
      if (stopRef.current) return 'stopped' as const;
      const res = await fetch(`/api/backend/api/scans/${scanJobId}/progress`);
      if (!res.ok) {
        await sleep(2000);
        continue;
      }
      const p = (await res.json()) as { status: string; progressPercentage: number; currentStep?: string };
      setMsg(`${p.status} %${p.progressPercentage}${p.currentStep ? ` · ${p.currentStep}` : ''}`);
      if (isTerminal(p.status)) {
        return p.status === 'Failed' || p.status === 'Cancelled' ? ('failed' as const) : ('done' as const);
      }
      await sleep(2500);
    }
    return 'failed' as const;
  }

  async function runCandidateAssessment(d: TargetDomain) {
    setScanningIds((prev) => new Set(prev).add(d.id));
    try {
      if (stopRef.current) return;

      if (!d.isVerified) {
        // H1 sync already marks verified; try manual verify if admin path needed — skip with error
        statsRef.current.fail += 1;
        setError(`${d.hostName}: doğrulanmamış — ASC çalışmaz`);
        return;
      }

      const res = await fetch('/api/backend/api/hackerone/candidate-assessment', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ domainAssetId: d.id, hostName: d.hostName })
      });
      const data = await res.json().catch(() => ({}));
      if (!res.ok) {
        statsRef.current.fail += 1;
        setError(`${d.hostName}: ${data.detail || data.title || 'ASC başlatılamadı'}`);
        return;
      }

      const scanJobId = data.scanJobId as string;
      const nextJobs = { ...scanJobIds, [d.id]: scanJobId };
      setScanJobIds(nextJobs);

      const outcome = await waitScan(scanJobId);
      if (outcome !== 'done') {
        if (outcome === 'failed') statsRef.current.fail += 1;
        return;
      }

      statsRef.current.ok += 1;
      // ASC bitince adayları + tarama özetini çek
      await sleep(1500);
      const list = await refreshCandidates();
      const assessment = await loadAssessment(d.id);
      const host = hostKey(d.hostName);
      const moneyMatches = list.filter(
        (c) => c.domainHostName && hostKey(c.domainHostName) === host && isMoneyCandidate(c, d)
      );
      const candidateMatches = list.filter(
        (c) => c.domainHostName && hostKey(c.domainHostName) === host && isCandidateSignal(c)
      );

      const nextScanned = new Set(scannedIds).add(d.id);
      setScannedIds(nextScanned);
      setExpandedId(d.id);

      if (moneyMatches.length > 0) {
        const nextMoney = new Set(moneyIds).add(d.id);
        const nextFindings = {
          ...moneyFindings,
          [d.id]: moneyMatches.map((m) => m.findingId)
        };
        setMoneyIds(nextMoney);
        setMoneyFindings(nextFindings);
        persist(marked, nextScanned, nextMoney, nextFindings, candidateIds, candidateFindings, nextJobs);
        statsRef.current.money += 1;
        setJackpotId(d.id);
        setMsg(`💰 PARA (doğrulanmış uygunluk): ${d.hostName} · ${moneyMatches.length}`);
        await playMoneyBountyAlarm();
        setTimeout(() => setJackpotId((cur) => (cur === d.id ? null : cur)), 6000);
      } else if (candidateMatches.length > 0) {
        const nextCand = new Set(candidateIds).add(d.id);
        const nextCandFindings = {
          ...candidateFindings,
          [d.id]: candidateMatches.map((m) => m.findingId)
        };
        setCandidateIds(nextCand);
        setCandidateFindings(nextCandFindings);
        persist(marked, nextScanned, moneyIds, moneyFindings, nextCand, nextCandFindings, nextJobs);
        statsRef.current.candidate += 1;
        setMsg(`Aday sinyal: ${d.hostName} · doğrulama gerekli · ${candidateMatches.length}`);
        await playFunnyFindingAlarm();
      } else {
        persist(marked, nextScanned, moneyIds, moneyFindings, candidateIds, candidateFindings, nextJobs);
        const obs = assessment?.findings.length ?? 0;
        setMsg(
          obs > 0
            ? `Tarama bitti: ${d.hostName} · ${obs} gözlem (aday/para yok) · rapor indirilebilir`
            : `Tarama bitti: ${d.hostName} · aday bulgu yok · güvenlik raporu yine indirilebilir`
        );
      }
    } catch {
      statsRef.current.fail += 1;
      setError(`${d.hostName}: ağ hatası`);
    } finally {
      statsRef.current.done += 1;
      setScanningIds((prev) => {
        const n = new Set(prev);
        n.delete(d.id);
        return n;
      });
      const s = statsRef.current;
      setMsg(`ASC · ${s.done}/${s.total} · OK ${s.ok} · Aday ${s.candidate} · Para ${s.money}`);
    }
  }

  async function runMarkedAssessments() {
    if (markedQueue.length === 0) {
      setError(
        onlyBountyEligible
          ? 'İşaretli bounty-eligible hedef yok. Checkbox ile işaretleyin veya filtreyi gevşetin.'
          : 'İşaretli hedef yok.'
      );
      return;
    }

    await runAssessments(markedQueue);
  }

  function pickBatch(mode: 'sequential' | 'random', size: number): TargetDomain[] {
    let pool = baseList.filter((d) => d.isVerified);
    if (skipScanned) {
      pool = pool.filter((d) => !scannedIds.has(d.id));
    }
    if (mode === 'random') {
      pool = [...pool];
      for (let i = pool.length - 1; i > 0; i--) {
        const j = Math.floor(Math.random() * (i + 1));
        [pool[i], pool[j]] = [pool[j]!, pool[i]!];
      }
    }
    return pool.slice(0, size);
  }

  async function runBatch(mode: 'sequential' | 'random', size: number) {
    const queue = pickBatch(mode, size);
    if (queue.length === 0) {
      setError(
        skipScanned
          ? 'Taranmamış ve doğrulanmış hedef kalmadı. “Taranmışları atla”yı kapatmayı deneyin.'
          : 'Doğrulanmış hedef bulunamadı.'
      );
      return;
    }

    const next = new Set(marked);
    for (const d of queue) next.add(d.id);
    setMarked(next);
    persist(next, scannedIds, moneyIds, moneyFindings);
    await runAssessments(queue);
  }

  async function runAssessments(queue: TargetDomain[]) {
    const workers = Math.min(Math.max(1, concurrency), 5);
    stopRef.current = false;
    setScanBusy(true);
    setError(null);
    statsRef.current = { ok: 0, fail: 0, money: 0, candidate: 0, done: 0, total: queue.length };
    setMsg(`${queue.length} hedef · Candidate Assessment · ${workers} eşzamanlı`);
    void unlockAudio();

    let index = 0;
    async function worker() {
      while (!stopRef.current) {
        const i = index++;
        if (i >= queue.length) break;
        await runCandidateAssessment(queue[i]!);
      }
    }
    await Promise.all(Array.from({ length: workers }, () => worker()));
    setScanBusy(false);
    setScanningIds(new Set());
    const s = statsRef.current;
    setMsg(
      stopRef.current
        ? `Durduruldu · Aday ${s.candidate} · Para ${s.money}`
        : `Bitti · OK ${s.ok} · Aday ${s.candidate} · Para ${s.money}`
    );
    if (s.money > 0) setFilter('money');
    else if (s.candidate > 0) setFilter('candidate');
    router.refresh();
  }

  function markVisible() {
    const next = new Set(marked);
    for (const d of visible) next.add(d.id);
    setMarked(next);
    persist(next, scannedIds, moneyIds, moneyFindings);
  }

  function clearMarks() {
    const next = new Set<string>();
    setMarked(next);
    persist(next, scannedIds, moneyIds, moneyFindings);
  }

  function findingsFor(domainId: string): TargetCandidate[] {
    const d = domains.find((x) => x.id === domainId);
    if (!d) return [];
    const host = hostKey(d.hostName);
    const fromServer = candidates.filter(
      (c) => c.domainHostName && hostKey(c.domainHostName) === host && isReportableCandidate(c)
    );
    const moneyIdsForDomain = new Set(moneyFindings[domainId] || []);
    const extra = candidates.filter((c) => moneyIdsForDomain.has(c.findingId));
    const map = new Map<string, TargetCandidate>();
    for (const c of [...fromServer, ...extra]) map.set(c.findingId, c);
    return [...map.values()];
  }

  return (
    <div className="space-y-3">
      {error && (
        <div className="rounded-md border border-rose-200 bg-rose-50 px-3 py-2 text-xs text-rose-700">{error}</div>
      )}
      {msg && (
        <div className="rounded-md border border-amber-200 bg-amber-50 px-3 py-2 text-xs text-amber-950">{msg}</div>
      )}

      <div className="flex flex-wrap items-center gap-2">
        <button
          type="button"
          disabled={syncBusy || scanBusy}
          onClick={syncScopes}
          className="rounded-md border border-slate-300 bg-white px-3 py-1.5 text-sm disabled:opacity-50"
        >
          {syncBusy ? 'Sync…' : 'H1 scope sync'}
        </button>
        <label className="flex items-center gap-1 text-xs text-slate-600">
          Eşzamanlı ASC
          <input
            type="number"
            min={1}
            max={5}
            disabled={scanBusy}
            value={concurrency}
            onChange={(e) => setConcurrency(Math.min(5, Math.max(1, Number(e.target.value) || 1)))}
            className="w-12 rounded border px-1 py-1 text-sm"
          />
        </label>
        <label className="flex items-center gap-1 text-xs text-slate-600">
          <input
            type="checkbox"
            checked={onlyBountyEligible}
            disabled={scanBusy}
            onChange={(e) => setOnlyBountyEligible(e.target.checked)}
          />
          Program ödüyor + eligible
        </label>
        <button type="button" disabled={scanBusy} onClick={markVisible} className="rounded-md border px-2 py-1 text-xs">
          Görünenleri işaretle
        </button>
        <button type="button" disabled={scanBusy} onClick={clearMarks} className="rounded-md border px-2 py-1 text-xs">
          İşaret temizle
        </button>
        {!scanBusy ? (
          <button
            type="button"
            disabled={markedQueue.length === 0}
            onClick={runMarkedAssessments}
            className="rounded-md bg-emerald-700 px-3 py-1.5 text-sm font-semibold text-white disabled:opacity-50"
          >
            Candidate Assessment ({markedQueue.length})
          </button>
        ) : (
          <button
            type="button"
            onClick={() => {
              stopRef.current = true;
              setMsg('Durduruluyor…');
            }}
            className="rounded-md border border-rose-300 bg-rose-50 px-3 py-1.5 text-sm font-semibold text-rose-800"
          >
            Durdur
          </button>
        )}
        <button
          type="button"
          onClick={() => void playMoneyBountyAlarm()}
          className="rounded-md border border-amber-400 bg-amber-50 px-2 py-1 text-xs"
        >
          💰 Jackpot test
        </button>
      </div>

      <div className="flex flex-wrap items-center gap-2 rounded-md border border-sky-200 bg-sky-50 px-3 py-2">
        <span className="text-xs font-semibold text-sky-800">Manuel hedef ekle</span>
        <input
          type="text"
          disabled={manualBusy}
          value={manualHost}
          onChange={(e) => setManualHost(e.target.value)}
          placeholder="ornek.com"
          className="min-w-[200px] flex-1 rounded border px-2 py-1 text-sm"
        />
        <label className="flex items-center gap-1 text-xs text-slate-600">
          <input
            type="checkbox"
            checked={manualAuthorized}
            disabled={manualBusy}
            onChange={(e) => setManualAuthorized(e.target.checked)}
          />
          Test etmeye yetkiliyim
        </label>
        <button
          type="button"
          disabled={manualBusy || !manualHost.trim() || !manualAuthorized}
          onClick={() => void addManualTarget()}
          className="rounded-md bg-sky-700 px-3 py-1.5 text-sm font-semibold text-white disabled:opacity-50"
        >
          {manualBusy ? 'Ekleniyor…' : 'Hedef Ekle'}
        </button>
        <span className="text-[11px] text-slate-500">
          Eklenen hedef doğrulanmış gelir; satırdaki “Girişli Tarama” ile giriş yapıp tarayabilirsiniz.
        </span>
      </div>

      <div className="flex flex-wrap items-center gap-2 rounded-md border border-slate-200 bg-slate-50 px-3 py-2">
        <span className="text-xs font-semibold text-slate-700">Toplu tarama</span>
        <button
          type="button"
          disabled={scanBusy}
          onClick={() => void runBatch('sequential', 5)}
          className="rounded-md border border-emerald-300 bg-white px-2 py-1 text-xs font-medium text-emerald-800 disabled:opacity-50"
        >
          Sıradaki 5
        </button>
        <button
          type="button"
          disabled={scanBusy}
          onClick={() => void runBatch('sequential', 10)}
          className="rounded-md border border-emerald-300 bg-white px-2 py-1 text-xs font-medium text-emerald-800 disabled:opacity-50"
        >
          Sıradaki 10
        </button>
        <button
          type="button"
          disabled={scanBusy}
          onClick={() => void runBatch('random', 10)}
          className="rounded-md border border-indigo-300 bg-white px-2 py-1 text-xs font-medium text-indigo-800 disabled:opacity-50"
        >
          Rastgele 10
        </button>
        <label className="flex items-center gap-1 text-xs text-slate-600">
          Adet
          <input
            type="number"
            min={1}
            max={200}
            disabled={scanBusy}
            value={batchSize}
            onChange={(e) => setBatchSize(Math.min(200, Math.max(1, Number(e.target.value) || 1)))}
            className="w-16 rounded border px-1 py-1 text-sm"
          />
        </label>
        <button
          type="button"
          disabled={scanBusy}
          onClick={() => void runBatch('sequential', batchSize)}
          className="rounded-md border px-2 py-1 text-xs disabled:opacity-50"
        >
          Sıradaki {batchSize}
        </button>
        <button
          type="button"
          disabled={scanBusy}
          onClick={() => void runBatch('random', batchSize)}
          className="rounded-md border px-2 py-1 text-xs disabled:opacity-50"
        >
          Rastgele {batchSize}
        </button>
        <label className="flex items-center gap-1 text-xs text-slate-600">
          <input
            type="checkbox"
            checked={skipScanned}
            disabled={scanBusy}
            onChange={(e) => setSkipScanned(e.target.checked)}
          />
          Taranmışları atla
        </label>
        <span className="text-[11px] text-slate-500">
          Havuz: {baseList.filter((d) => d.isVerified && (!skipScanned || !scannedIds.has(d.id))).length} doğrulanmış hedef
        </span>
      </div>

      <div className="flex flex-wrap gap-1 text-xs">
        {(
          [
            ['all', `Tümü (${baseList.length})`],
            ['marked', `İşaretli (${marked.size})`],
            ['scanned', `Taranan (${scannedIds.size})`],
            ['candidate', `Aday (${candidateIds.size})`],
            ['money', `Para (${moneyIds.size})`]
          ] as const
        ).map(([key, label]) => (
          <button
            key={key}
            type="button"
            disabled={scanBusy}
            onClick={() => setFilter(key)}
            className={`rounded-md border px-2 py-1 ${
              filter === key ? 'border-amber-700 bg-amber-700 text-white' : 'bg-white text-slate-600'
            }`}
          >
            {label}
          </button>
        ))}
      </div>

      <p className="text-xs text-slate-500">
        Yeşil = taranan · <span className="font-semibold text-sky-700">Aday</span> = doğrulama gerekli ·{' '}
        <span className="font-semibold text-amber-700">Para</span> = doğrulanmış + submission eligible ·{' '}
        <span className="font-semibold text-slate-600">çöp</span> = tarandı, aday/para yok (yine de rapor
        indirebilirsiniz). Satırdaki “Ne tarandı?” ile motor/gözlem listesini açın.
      </p>

      <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-left text-xs uppercase text-slate-500">
            <tr>
              <th className="w-10 px-3 py-2">✓</th>
              <th className="px-3 py-2">Host</th>
              <th className="px-3 py-2">Program / bounty</th>
              <th className="px-3 py-2">Durum</th>
              <th className="px-3 py-2 text-right">Aksiyon</th>
            </tr>
          </thead>
          <tbody>
            {visible.length === 0 ? (
              <tr>
                <td colSpan={5} className="px-4 py-6 text-center text-slate-500">
                  Kayıt yok — sync / filtre / işaret kontrol edin.
                </td>
              </tr>
            ) : (
              visible.map((d) => {
                const scanning = scanningIds.has(d.id);
                const money = moneyIds.has(d.id);
                const candidate = candidateIds.has(d.id) && !money;
                const scanned = scannedIds.has(d.id);
                const jackpot = jackpotId === d.id;
                const findings = findingsFor(d.id);
                const assessment = assessments[d.id];
                const scanJobId = scanJobIds[d.id] || assessment?.scanJobId;
                const expanded = expandedId === d.id;
                return (
                  <Fragment key={d.id}>
                  <tr
                    className={[
                      'border-t border-slate-100 transition-all',
                      jackpot || money
                        ? 'bg-amber-50 outline outline-2 outline-offset-[-2px] outline-amber-500 shadow-[0_0_24px_rgba(245,158,11,0.35)]'
                        : candidate
                          ? 'bg-sky-50 outline outline-2 outline-offset-[-2px] outline-sky-500'
                          : scanning || scanned
                            ? 'bg-emerald-50 outline outline-2 outline-offset-[-2px] outline-emerald-600'
                            : '',
                      jackpot ? 'animate-pulse' : ''
                    ].join(' ')}
                  >
                    <td className="px-3 py-2">
                      <input
                        type="checkbox"
                        checked={marked.has(d.id)}
                        disabled={scanBusy}
                        onChange={() => toggleMark(d.id)}
                      />
                    </td>
                    <td className="px-3 py-2">
                      <div className="font-medium text-slate-800">
                        {d.hostName}
                        {scanning && (
                          <span className="ml-2 rounded bg-emerald-600 px-1.5 py-0.5 text-[10px] font-semibold uppercase text-white">
                            ASC
                          </span>
                        )}
                        {scanned && !scanning && (
                          <span className="ml-2 rounded bg-emerald-100 px-1.5 py-0.5 text-[10px] font-semibold uppercase text-emerald-800">
                            taranan
                          </span>
                        )}
                        {candidate && (
                          <span className="ml-2 rounded bg-sky-600 px-1.5 py-0.5 text-[10px] font-bold uppercase tracking-wide text-white">
                            Aday
                          </span>
                        )}
                        {money && (
                          <span className="ml-2 rounded bg-amber-500 px-1.5 py-0.5 text-[10px] font-bold uppercase tracking-wide text-white">
                            Para
                          </span>
                        )}
                        {scanned && !scanning && !money && !candidate && (
                          <span
                            className="ml-2 rounded bg-slate-700 px-1.5 py-0.5 text-[10px] font-bold uppercase tracking-wide text-white"
                            title="Tarandı — aday/para yok"
                          >
                            çöp
                          </span>
                        )}
                      </div>
                      {findings[0] && (
                        <div className="mt-0.5 text-xs text-slate-600 line-clamp-1">{findings[0].title}</div>
                      )}
                      {assessment && (
                        <div className="mt-0.5 text-[11px] text-slate-500">
                          Motor: {assessment.enginesRun.slice(0, 4).join(', ') || '—'}
                          {assessment.enginesRun.length > 4 ? '…' : ''} · gözlem {assessment.findings.length} · puan{' '}
                          {assessment.securityScore}
                        </div>
                      )}
                    </td>
                    <td className="px-3 py-2 text-xs text-slate-600">
                      <div>{d.hackerOneProgramName || '—'}</div>
                      <div>
                        {d.hackerOneOffersBounties ? (
                          <span className="text-emerald-700">ödüyor{d.hackerOneCurrency ? ` (${d.hackerOneCurrency})` : ''}</span>
                        ) : (
                          <span className="text-slate-400">VDP</span>
                        )}
                        {d.hackerOneEligibleForBounty ? ' · eligible' : ' · eligible değil'}
                        {d.hackerOneMaxSeverity ? ` · max ${d.hackerOneMaxSeverity}` : ''}
                      </div>
                    </td>
                    <td className="px-3 py-2 text-xs text-slate-500">
                      {d.isVerified ? 'doğrulandı' : 'doğrulanmadı'} · {d.status}
                    </td>
                    <td className="px-3 py-2 text-right">
                      <div className="flex flex-col items-end gap-1">
                        {findings.map((f) => (
                          <Link
                            key={f.findingId}
                            href={`/hackerone/report-builder?findingId=${f.findingId}`}
                            className="rounded-md bg-amber-600 px-2 py-1 text-[11px] font-semibold text-white hover:bg-amber-700"
                          >
                            H1 rapor hazırla
                          </Link>
                        ))}
                        {money && findings.length === 0 && (
                          <Link
                            href="/hackerone/candidates"
                            className="text-[11px] text-amber-800 underline"
                          >
                            Candidates’a bak
                          </Link>
                        )}
                        {(scanned || scanJobId) && (
                          <button
                            type="button"
                            onClick={() => {
                              setExpandedId(expanded ? null : d.id);
                              if (!assessment) void loadAssessment(d.id);
                            }}
                            className="rounded-md border border-slate-300 bg-white px-2 py-1 text-[11px] text-slate-700"
                          >
                            {expanded ? 'Özeti gizle' : 'Ne tarandı?'}
                          </button>
                        )}
                        {scanJobId && (
                          <button
                            type="button"
                            onClick={() => void downloadSecurityReport(scanJobId, d.hostName, 'html')}
                            className="rounded-md bg-slate-800 px-2 py-1 text-[11px] font-semibold text-white"
                          >
                            Güvenlik raporu
                          </button>
                        )}
                        {!money && !scanning && (
                          <button
                            type="button"
                            disabled={scanBusy}
                            onClick={() => void runCandidateAssessment(d)}
                            className="rounded-md border border-slate-200 px-2 py-1 text-[11px] text-slate-700 disabled:opacity-50"
                          >
                            Tek ASC
                          </button>
                        )}
                        <AuthenticatedScanPanel targetId={d.id} hostName={d.hostName} />
                      </div>
                    </td>
                  </tr>
                  {expanded && (
                    <tr className="border-t border-slate-100 bg-slate-50">
                      <td colSpan={5} className="px-4 py-3 text-xs text-slate-700">
                        {!assessment ? (
                          <div className="text-slate-500">Tarama özeti yükleniyor…</div>
                        ) : (
                          <div className="space-y-2">
                            <div className="font-semibold text-slate-800">
                              Tarama özeti — {assessment.hostName}
                              <span className="ml-2 font-normal text-slate-500">
                                {assessment.completedAt
                                  ? new Date(assessment.completedAt).toLocaleString('tr-TR')
                                  : assessment.status}
                              </span>
                            </div>
                            <p className="text-slate-600">{assessment.summary || assessment.executiveSummary}</p>
                            <div className="flex flex-wrap gap-1">
                              <span className="rounded bg-white px-2 py-0.5 border">Motorlar ({assessment.enginesRun.length})</span>
                              {assessment.enginesRun.map((e) => (
                                <span key={e} className="rounded border border-emerald-200 bg-emerald-50 px-2 py-0.5 font-mono text-[11px]">
                                  {e}
                                </span>
                              ))}
                            </div>
                            <div className="text-slate-600">
                              Puan {assessment.securityScore}/100 · kontroller {assessment.checksPassed}/
                              {assessment.checksTotal}
                              {assessment.checksFailed > 0 ? ` · motor hata ${assessment.checksFailed}` : ''} · C
                              {assessment.criticalCount}/H{assessment.highCount}/M{assessment.mediumCount}/L
                              {assessment.lowCount}/I{assessment.infoCount}
                            </div>
                            {assessment.findings.length === 0 ? (
                              <p className="rounded border border-slate-200 bg-white px-2 py-1 text-slate-500">
                                Bu taramada aday bulgu üretilmedi. Yine de güvenlik raporu indirebilirsiniz — motorların
                                çalıştığı ve gözlem olmadığı belgelenir.
                              </p>
                            ) : (
                              <ul className="max-h-48 space-y-1 overflow-y-auto rounded border bg-white p-2">
                                {assessment.findings.map((f) => (
                                  <li key={f.findingId} className="flex flex-wrap items-baseline justify-between gap-2 border-b border-slate-50 py-1 last:border-0">
                                    <div>
                                      <span className="font-medium">{f.title}</span>
                                      <span className="ml-2 text-slate-500">
                                        {f.severity} · {f.submissionRecommendation}
                                        {f.checkCode ? ` · ${f.checkCode}` : ''}
                                      </span>
                                      {f.affectedUrl && (
                                        <div className="font-mono text-[10px] text-slate-400 break-all">{f.affectedUrl}</div>
                                      )}
                                    </div>
                                    {(f.submissionRecommendation === 'ManualReview' ||
                                      f.submissionRecommendation === 'Submit') && (
                                      <Link
                                        href={`/hackerone/report-builder?findingId=${f.findingId}`}
                                        className="shrink-0 text-amber-700 underline"
                                      >
                                        H1 taslak
                                      </Link>
                                    )}
                                  </li>
                                ))}
                              </ul>
                            )}
                            <div className="flex flex-wrap gap-2">
                              <button
                                type="button"
                                onClick={() => void downloadSecurityReport(assessment.scanJobId, d.hostName, 'html')}
                                className="rounded bg-slate-800 px-2 py-1 font-semibold text-white"
                              >
                                HTML güvenlik raporu
                              </button>
                              <button
                                type="button"
                                onClick={() => void downloadSecurityReport(assessment.scanJobId, d.hostName, 'txt')}
                                className="rounded border px-2 py-1"
                              >
                                TXT rapor
                              </button>
                              <Link href={`/scans/${assessment.scanJobId}`} className="rounded border px-2 py-1 underline">
                                Tarama detayı
                              </Link>
                            </div>
                          </div>
                        )}
                      </td>
                    </tr>
                  )}
                  </Fragment>
                );
              })
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
