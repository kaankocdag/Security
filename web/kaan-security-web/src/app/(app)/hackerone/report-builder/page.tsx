import { requireSession, isSystemAdmin } from '@/lib/session';
import { redirect } from 'next/navigation';
import { apiFetch } from '@/lib/api';
import { ReportBuilderClient } from './report-builder-client';

export interface Draft {
  id: string;
  findingId: string;
  bugBountyProgramId: string;
  programHandle: string;
  title: string;
  severity: string;
  asset: string;
  weakness: string;
  impact: string;
  stepsToReproduce: string;
  proofOfConcept: string;
  notes?: string | null;
  markdownBody?: string | null;
  turkishMarkdownBody?: string | null;
  reportReadinessScore: number;
  status: string;
  createdAt: string;
  updatedAt?: string | null;
}

export default async function ReportBuilderPage({
  searchParams
}: {
  searchParams: Promise<{ findingId?: string; draftId?: string }>;
}) {
  const { accessToken, user } = await requireSession();
  if (!isSystemAdmin(user)) redirect('/dashboard');
  const sp = await searchParams;

  let drafts: Draft[] = [];
  let draft: Draft | null = null;
  let openUrl = 'https://hackerone.com/amazonvrp';

  try {
    drafts = await apiFetch<Draft[]>('/api/hackerone/drafts', { accessToken, serverSide: true });
    const settings = await apiFetch<{ openReportUrlTemplate: string; defaultBugBountyProgramId?: string }>(
      '/api/hackerone/settings',
      { accessToken, serverSide: true }
    );
    const programs = await apiFetch<{ id: string; handle: string; openReportUrl?: string }[]>(
      '/api/hackerone/programs',
      { accessToken, serverSide: true }
    );
    const def = programs.find((p) => p.id === settings.defaultBugBountyProgramId) ?? programs[0];
    openUrl =
      def?.openReportUrl ||
      settings.openReportUrlTemplate.replace('{handle}', def?.handle || 'amazonvrp');

    if (sp.draftId) {
      draft = await apiFetch<Draft>(`/api/hackerone/drafts/${sp.draftId}`, {
        accessToken,
        serverSide: true
      });
    } else if (sp.findingId) {
      draft = await apiFetch<Draft>('/api/hackerone/drafts', {
        method: 'POST',
        body: { findingId: sp.findingId },
        accessToken,
        serverSide: true
      });
    } else if (drafts[0]) {
      draft = drafts[0];
    }
  } catch {
    // ignore
  }

  return (
    <ReportBuilderClient initialDraft={draft} drafts={drafts} openHackerOneUrl={openUrl} />
  );
}
