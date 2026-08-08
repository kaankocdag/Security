import Link from 'next/link';
import { apiFetch } from '@/lib/api';
import { requireSession, isSystemAdmin } from '@/lib/session';
import { DomainsTableClient, type DomainListItem } from './domains-table-client';

export default async function DomainsPage() {
  const { accessToken, user } = await requireSession();
  const admin = isSystemAdmin(user);
  let domains: DomainListItem[] = [];
  try {
    domains = await apiFetch<DomainListItem[]>('/api/domains', {
      accessToken,
      serverSide: true
    });
  } catch {
    domains = [];
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-slate-900">Domainler</h1>
        <Link
          href="/site-test"
          className="rounded-md bg-[color:var(--color-brand-600)] px-3 py-2 text-sm font-semibold text-white hover:bg-[color:var(--color-brand-700)]"
        >
          Yeni domain ekle
        </Link>
      </div>
      <DomainsTableClient initialDomains={domains} isSystemAdmin={admin} />
    </div>
  );
}
