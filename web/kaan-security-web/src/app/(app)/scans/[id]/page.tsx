import { notFound } from 'next/navigation';
import { apiFetch } from '@/lib/api';
import { requireSession } from '@/lib/session';
import {
  ScanDetailClient,
  type FindingItem,
  type ScanDetail
} from './scan-detail-client';

export default async function ScanDetailPage({
  params
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const { accessToken } = await requireSession();
  let scan: ScanDetail;
  try {
    scan = await apiFetch<ScanDetail>(`/api/scans/${id}`, {
      accessToken,
      serverSide: true
    });
  } catch {
    notFound();
  }

  let findings: FindingItem[] = [];
  if (scan.result?.id) {
    try {
      findings = await apiFetch<FindingItem[]>(`/api/findings?scanResultId=${scan.result.id}`, {
        accessToken,
        serverSide: true
      });
    } catch {
      findings = [];
    }
  }

  return <ScanDetailClient initialScan={scan} initialFindings={findings} />;
}
