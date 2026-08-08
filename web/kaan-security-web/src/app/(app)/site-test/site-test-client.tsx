'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { Check, ChevronLeft, ChevronRight, Rocket, ShieldCheck } from 'lucide-react';
import { cn } from '@/lib/utils';
import { HelpTip } from '@/components/ui/help-tip';

const steps = ['Proje', 'Domain', 'Tarama türü', 'SystemAdmin onayı', 'Başlat'];

const ENVIRONMENT_OPTIONS = [
  {
    value: 'Production',
    label: 'Canlı (Production)',
    description:
      'Gerçek kullanıcıların kullandığı canlı site. Varsayılan seçimdir; kamuya açık üretim ortamı.'
  },
  {
    value: 'Staging',
    label: 'Ön üretim (Staging)',
    description:
      'Canlıya çıkmadan önceki kopya / önizleme ortamı. Genelde son kontroller için kullanılır.'
  },
  {
    value: 'Testing',
    label: 'Test (Testing)',
    description:
      'QA / entegrasyon test ortamı. Otomatik veya manuel doğrulama için ayrılmış sunucu.'
  },
  {
    value: 'Development',
    label: 'Geliştirme (Development)',
    description:
      'Geliştirici ortamı. Lokal veya paylaşılan dev sunucu; henüz müşteriye açık değildir.'
  }
] as const;

type EnvironmentValue = (typeof ENVIRONMENT_OPTIONS)[number]['value'];

interface WizardState {
  projectId?: string;
  projectName: string;
  environment: EnvironmentValue;
  domainId?: string;
  hostName: string;
  scanType: 'FullPassive' | 'SecurityHeaders' | 'Cookie' | 'InformationDisclosure';
  adminApproved: boolean;
  error?: string | null;
  scanJobId?: string;
}

export function SiteTestWizardClient() {
  const router = useRouter();
  const [step, setStep] = useState(0);
  const [state, setState] = useState<WizardState>({
    projectName: 'Ana Web Sitesi',
    environment: 'Production',
    hostName: '',
    scanType: 'FullPassive',
    adminApproved: false
  });
  const [submitting, setSubmitting] = useState(false);

  const patch = (p: Partial<WizardState>) => setState((s) => ({ ...s, ...p }));

  const canProceed = () => {
    switch (step) {
      case 0:
        return state.projectName.trim().length > 1;
      case 1:
        return /^[a-z0-9.-]+\.[a-z]{2,}$/i.test(state.hostName.trim());
      case 2:
        return !!state.scanType;
      case 3:
        return state.adminApproved;
      case 4:
        return true;
      default:
        return false;
    }
  };

  const createProject = async () => {
    const res = await fetch('/api/backend/api/projects', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        name: state.projectName,
        description: 'PublicPassiveAssessment sihirbazı üzerinden oluşturuldu.',
        environmentType: state.environment
      })
    });
    if (!res.ok) throw new Error('Proje oluşturulamadı.');
    const project = await res.json();
    patch({ projectId: project.id });
    return project.id as string;
  };

  const createDomain = async (projectId: string) => {
    const res = await fetch('/api/backend/api/domains', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        securityProjectId: projectId,
        hostName: state.hostName.trim(),
        includeSubdomains: false
      })
    });
    if (!res.ok) throw new Error('Domain kaydı oluşturulamadı.');
    const domain = await res.json();
    patch({ domainId: domain.id });
    return domain.id as string;
  };

  const startScan = async (domainId: string) => {
    const res = await fetch('/api/backend/api/scans', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        domainAssetId: domainId,
        scanType: state.scanType,
        assessmentMode: 'PublicPassiveAssessment',
        assessmentModeName: 'PublicPassiveAssessment'
      })
    });
    if (!res.ok) {
      const problem = await res.json().catch(() => undefined);
      throw new Error(problem?.detail ?? 'Tarama başlatılamadı.');
    }
    const data = await res.json();
    patch({ scanJobId: data.scanJobId });
    return data.scanJobId as string;
  };

  const handleNext = async () => {
    setSubmitting(true);
    patch({ error: null });
    try {
      if (step === 0 && !state.projectId) {
        await createProject();
      } else if (step === 1 && !state.domainId) {
        const pid = state.projectId ?? (await createProject());
        await createDomain(pid);
      } else if (step === 4 && state.domainId) {
        const jobId = await startScan(state.domainId);
        router.push(`/scans/${jobId}`);
        return;
      }
      setStep((s) => Math.min(4, s + 1));
    } catch (err) {
      patch({ error: err instanceof Error ? err.message : 'Beklenmeyen hata' });
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <div>
        <h1 className="flex items-center text-2xl font-bold text-slate-900">
          PublicPassiveAssessment
          <HelpTip
            text="İnternetteki kamuya açık siteye pasif tarama. Firma izni/DNS doğrulama istemez. SystemAdmin onayı gerekir. Aktif exploit yoktur."
            side="bottom"
          />
        </h1>
        <p className="mt-1 text-sm text-slate-600">
          Yalnızca güvenli GET/HEAD. Domain doğrulama gerekmez. Exploit / payload / brute force yok.
          SSRF koruması zorunludur.
        </p>
      </div>

      <ol className="flex items-center gap-2 overflow-x-auto text-xs">
        {steps.map((label, i) => (
          <li
            key={label}
            className={cn(
              'flex items-center gap-2 rounded-full border px-3 py-1',
              i < step
                ? 'border-emerald-200 bg-emerald-50 text-emerald-800'
                : i === step
                  ? 'border-[color:var(--color-brand-500)] bg-[color:var(--color-brand-50)] text-[color:var(--color-brand-700)]'
                  : 'border-slate-200 bg-white text-slate-500'
            )}
          >
            <span className="flex h-5 w-5 items-center justify-center rounded-full bg-white text-[10px] font-semibold text-slate-700">
              {i < step ? <Check size={12} /> : i + 1}
            </span>
            {label}
          </li>
        ))}
      </ol>

      <div className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
        {step === 0 && (
          <div className="space-y-4">
            <h2 className="text-lg font-semibold">1. Proje</h2>
            <Field label="Proje adı" value={state.projectName} onChange={(v) => patch({ projectName: v })} />
            <label className="block text-sm">
              <span className="mb-1 flex items-center gap-1 font-medium text-slate-700">
                Ortam
                <HelpTip
                  text="Projenin hangi sunucu/hedef sınıfına ait olduğunu belirtir. Tarama tipini değiştirmez; raporlama ve gruplama içindir."
                  side="right"
                />
              </span>
              <select
                className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm"
                value={state.environment}
                onChange={(e) =>
                  patch({ environment: e.target.value as EnvironmentValue })
                }
              >
                {ENVIRONMENT_OPTIONS.map((o) => (
                  <option key={o.value} value={o.value}>
                    {o.label}
                  </option>
                ))}
              </select>
              <p className="mt-2 rounded-md border border-slate-100 bg-slate-50 px-3 py-2 text-xs text-slate-600">
                {
                  ENVIRONMENT_OPTIONS.find((o) => o.value === state.environment)
                    ?.description
                }
              </p>
            </label>
          </div>
        )}
        {step === 1 && (
          <div className="space-y-4">
            <h2 className="text-lg font-semibold">2. Kamuya açık domain</h2>
            <Field
              label="Alan adı"
              value={state.hostName}
              placeholder="example.com"
              onChange={(v) => patch({ hostName: v.trim() })}
            />
          </div>
        )}
        {step === 2 && (
          <div className="space-y-4">
            <h2 className="text-lg font-semibold">3. Pasif kontrol paketi</h2>
            <div className="grid gap-2">
              {(
                [
                  { v: 'FullPassive', l: 'Tam pasif (SSL, header, cookie, redirect, security.txt, …)' },
                  { v: 'SecurityHeaders', l: 'Güvenlik başlıkları' },
                  { v: 'Cookie', l: 'Cookie özellikleri' },
                  { v: 'InformationDisclosure', l: 'Kamuya açık bilgi sızıntısı göstergeleri' }
                ] as const
              ).map((opt) => (
                <label
                  key={opt.v}
                  className={cn(
                    'flex cursor-pointer items-center gap-3 rounded-md border p-3 text-sm',
                    state.scanType === opt.v
                      ? 'border-[color:var(--color-brand-500)] bg-[color:var(--color-brand-50)]'
                      : 'border-slate-200 bg-white'
                  )}
                >
                  <input
                    type="radio"
                    name="scanType"
                    checked={state.scanType === opt.v}
                    onChange={() => patch({ scanType: opt.v })}
                  />
                  {opt.l}
                </label>
              ))}
            </div>
          </div>
        )}
        {step === 3 && (
          <div className="space-y-4">
            <h2 className="text-lg font-semibold">4. SystemAdmin onayı</h2>
            <label className="flex items-start gap-2 text-sm text-slate-700">
              <input
                type="checkbox"
                checked={state.adminApproved}
                onChange={(e) => patch({ adminApproved: e.target.checked })}
                className="mt-1 h-4 w-4 rounded border-slate-300"
              />
              <span>
                PublicPassiveAssessment olduğunu; serbest exploit/payload içermediğini onaylıyorum.
              </span>
            </label>
          </div>
        )}
        {step === 4 && (
          <div className="space-y-4">
            <h2 className="text-lg font-semibold">5. Başlat</h2>
            <SummaryRow label="Mod" value="PublicPassiveAssessment" />
            <SummaryRow label="Proje" value={state.projectName} />
            <SummaryRow
              label="Ortam"
              value={
                ENVIRONMENT_OPTIONS.find((o) => o.value === state.environment)?.label ??
                state.environment
              }
            />
            <SummaryRow label="Domain" value={state.hostName} />
            <SummaryRow label="Paket" value={state.scanType} />
            <div className="rounded-md border border-amber-200 bg-amber-50 p-3 text-xs text-amber-800">
              <ShieldCheck size={14} className="mr-1 inline" />
              Yalnızca GET/HEAD. Form gönderimi, brute force, payload yok.
            </div>
          </div>
        )}
        {state.error && (
          <div className="mt-4 rounded-md border border-rose-200 bg-rose-50 p-2 text-xs text-rose-700">
            {state.error}
          </div>
        )}
      </div>

      <div className="flex items-center justify-between">
        <button
          type="button"
          disabled={step === 0 || submitting}
          onClick={() => setStep((s) => Math.max(0, s - 1))}
          className="flex items-center gap-1 rounded-md border border-slate-200 bg-white px-3 py-2 text-sm text-slate-700 disabled:opacity-50"
        >
          <ChevronLeft size={14} /> Geri
        </button>
        <button
          type="button"
          disabled={!canProceed() || submitting}
          onClick={handleNext}
          className="flex items-center gap-1 rounded-md bg-[color:var(--color-brand-600)] px-4 py-2 text-sm font-semibold text-white shadow-sm hover:bg-[color:var(--color-brand-700)] disabled:opacity-50"
        >
          {step === 4 ? (
            <>
              <Rocket size={14} />
              Pasif taramayı başlat
            </>
          ) : (
            <>
              İleri <ChevronRight size={14} />
            </>
          )}
        </button>
      </div>
    </div>
  );
}

function Field({
  label,
  value,
  onChange,
  placeholder
}: {
  label: string;
  value: string;
  onChange: (v: string) => void;
  placeholder?: string;
}) {
  return (
    <label className="block text-sm">
      <span className="mb-1 block font-medium text-slate-700">{label}</span>
      <input
        value={value}
        placeholder={placeholder}
        onChange={(e) => onChange(e.target.value)}
        className="w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm shadow-sm focus:border-[color:var(--color-brand-600)] focus:outline-none focus:ring-1 focus:ring-[color:var(--color-brand-600)]"
      />
    </label>
  );
}

function SummaryRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-center justify-between rounded-md border border-slate-100 bg-slate-50 px-3 py-2 text-sm">
      <span className="text-slate-500">{label}</span>
      <span className="font-medium text-slate-800">{value}</span>
    </div>
  );
}
