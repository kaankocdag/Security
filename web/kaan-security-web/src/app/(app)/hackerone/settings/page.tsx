import { requireSession, isSystemAdmin } from '@/lib/session';
import { redirect } from 'next/navigation';
import { apiFetch } from '@/lib/api';
import { SettingsClient, type Settings, type ScanProfile } from './settings-client';

export default async function HackerOneSettingsPage() {
  const { accessToken, user } = await requireSession();
  if (!isSystemAdmin(user)) redirect('/dashboard');

  let settings: Settings | null = null;
  let profiles: ScanProfile[] = [];
  try {
    settings = await apiFetch<Settings>('/api/hackerone/settings', { accessToken, serverSide: true });
    profiles = await apiFetch<ScanProfile[]>('/api/hackerone/scan-profiles', { accessToken, serverSide: true });
  } catch {
    // ignore
  }

  return <SettingsClient initialSettings={settings} profiles={profiles} />;
}
