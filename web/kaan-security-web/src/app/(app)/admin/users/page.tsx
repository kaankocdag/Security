import { requireSession, isSystemAdmin } from '@/lib/session';
import { redirect } from 'next/navigation';
import { apiFetch } from '@/lib/api';
import { PendingListsClient } from './pending-lists-client';

interface PendingUser {
  userId: string;
  email: string;
  fullName: string;
  companyName?: string | null;
  status: string;
  createdAt: string;
}
interface PendingCompany {
  companyId: string;
  name: string;
  contactName: string;
  contactEmail: string;
  status: string;
  createdAt: string;
}

export default async function AdminUsersPage() {
  const { accessToken, user } = await requireSession();
  if (!isSystemAdmin(user)) {
    redirect('/dashboard');
  }
  let users: PendingUser[] = [];
  let companies: PendingCompany[] = [];
  try {
    [users, companies] = await Promise.all([
      apiFetch<PendingUser[]>('/api/admin/users/pending', { accessToken, serverSide: true }),
      apiFetch<PendingCompany[]>('/api/admin/companies/pending', {
        accessToken,
        serverSide: true
      })
    ]);
  } catch {
    // ignore
  }
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-slate-900">Üye ve Firma Onayları</h1>
        <p className="mt-1 text-sm text-slate-600">
          Onay bekleyen kayıtları buradan yönetin. Tüm işlemler denetim kaydına yazılır.
        </p>
      </div>
      <PendingListsClient initialUsers={users} initialCompanies={companies} />
    </div>
  );
}
