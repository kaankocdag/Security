import { requireSession, isSystemAdmin } from '@/lib/session';
import { redirect } from 'next/navigation';
import { LabExecutionClient } from './execution-client';

export default async function LabExecutionPage({
  params
}: {
  params: Promise<{ executionId: string }>;
}) {
  const { user } = await requireSession();
  if (!isSystemAdmin(user)) {
    redirect('/dashboard');
  }
  const { executionId } = await params;
  return <LabExecutionClient executionId={executionId} />;
}
