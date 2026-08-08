import { requireSession, isSystemAdmin } from '@/lib/session';
import { redirect } from 'next/navigation';
import { apiFetch } from '@/lib/api';
import { LabClient, type LabTargetSite } from './lab-client';
import { HelpTip } from '@/components/ui/help-tip';

export interface LabScenario {
  scenarioKey: string;
  titleTr: string;
  summaryTr: string;
  riskCategory: number;
  isFullyImplemented: boolean;
  displayOrder: number;
}

export interface LabExecutionListItem {
  id: string;
  scenarioKey: string;
  scenarioTitleTr: string;
  targetHostName: string;
  status: number;
  runtimeMode: number;
  auditCorrelationId: string;
  createdAt: string;
  completedAt?: string | null;
}

export default async function AdminLabPage() {
  const { accessToken, user } = await requireSession();
  if (!isSystemAdmin(user)) {
    redirect('/dashboard');
  }

  let scenarios: LabScenario[] = [];
  let executions: LabExecutionListItem[] = [];
  let targets: LabTargetSite[] = [];
  try {
    [scenarios, executions, targets] = await Promise.all([
      apiFetch<LabScenario[]>('/api/admin/lab/scenarios', { accessToken, serverSide: true }),
      apiFetch<LabExecutionListItem[]>('/api/admin/lab/executions', {
        accessToken,
        serverSide: true
      }),
      apiFetch<LabTargetSite[]>('/api/admin/lab/targets', { accessToken, serverSide: true })
    ]);
  } catch {
    // ignore
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="flex items-center text-2xl font-bold text-slate-900">
          IsolatedSecurityLab
          <HelpTip text="Önce hedef ekleyin → yükselme bileti → onay ifadesi → senaryo. Serbest URL yok; acil durdur var." />
        </h1>
        <p className="mt-1 max-w-3xl text-sm text-slate-600">
          Allowlist hedeflerde, imzalı senaryolarla kontrollü laboratuvar. Serbest URL/IP/payload
          yok. Lab internet çıkışı açık (deneme). Doğrulanmış domainlerdeki yetkili dış
          değerlendirme için AuthorizedExternalAssessment (bulgu detayı) kullanılır.{' '}
          <a href="/help" className="text-[color:var(--color-brand-700)] underline">
            Rehber
          </a>
        </p>
      </div>
      <LabClient
        initialScenarios={scenarios}
        initialExecutions={executions}
        initialTargets={targets}
      />
    </div>
  );
}
