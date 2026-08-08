'use client';

import { useState, useTransition } from 'react';
import { formatDateTr } from '@/lib/utils';

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

interface Props {
  initialUsers: PendingUser[];
  initialCompanies: PendingCompany[];
}

export function PendingListsClient({ initialUsers, initialCompanies }: Props) {
  const [users, setUsers] = useState(initialUsers);
  const [companies, setCompanies] = useState(initialCompanies);
  const [pending, startTransition] = useTransition();
  const [error, setError] = useState<string | null>(null);

  const call = async (path: string, body?: unknown) => {
    setError(null);
    const res = await fetch(`/api/backend/${path}`, {
      method: 'POST',
      headers: body ? { 'Content-Type': 'application/json' } : undefined,
      body: body ? JSON.stringify(body) : undefined
    });
    if (!res.ok) {
      const problem = await res.json().catch(() => undefined);
      setError(problem?.detail ?? 'İşlem başarısız.');
      throw new Error('failed');
    }
  };

  const approveUser = (id: string) =>
    startTransition(async () => {
      try {
        await call(`api/admin/users/${id}/approve`, { note: null });
        setUsers((u) => u.filter((x) => x.userId !== id));
      } catch {
        // handled
      }
    });

  const rejectUser = (id: string) => {
    const reason = window.prompt('Ret sebebini girin:', 'Yetkisiz başvuru.');
    if (!reason) return;
    startTransition(async () => {
      try {
        await call(`api/admin/users/${id}/reject`, { reason });
        setUsers((u) => u.filter((x) => x.userId !== id));
      } catch {
        // handled
      }
    });
  };

  const approveCompany = (id: string) =>
    startTransition(async () => {
      try {
        await call(`api/admin/companies/${id}/approve`);
        setCompanies((c) => c.filter((x) => x.companyId !== id));
      } catch {
        // handled
      }
    });

  return (
    <div className="space-y-6">
      {error && (
        <div className="rounded-md border border-rose-200 bg-rose-50 p-3 text-xs text-rose-700">
          {error}
        </div>
      )}
      <section className="rounded-2xl border border-slate-200 bg-white p-4 shadow-sm">
        <h2 className="text-sm font-semibold text-slate-800">Kullanıcı başvuruları</h2>
        <table className="mt-3 w-full text-sm">
          <thead className="text-left text-xs uppercase text-slate-500">
            <tr>
              <th className="py-2">Ad</th>
              <th className="py-2">E-posta</th>
              <th className="py-2">Firma</th>
              <th className="py-2">Tarih</th>
              <th className="py-2 text-right">İşlem</th>
            </tr>
          </thead>
          <tbody>
            {users.length === 0 ? (
              <tr>
                <td colSpan={5} className="py-4 text-center text-slate-500">
                  Onay bekleyen kullanıcı yok.
                </td>
              </tr>
            ) : (
              users.map((u) => (
                <tr key={u.userId} className="border-t border-slate-100">
                  <td className="py-2">{u.fullName}</td>
                  <td className="py-2 text-slate-600">{u.email}</td>
                  <td className="py-2 text-slate-600">{u.companyName ?? '—'}</td>
                  <td className="py-2 text-slate-500">{formatDateTr(u.createdAt)}</td>
                  <td className="py-2 text-right">
                    <button
                      disabled={pending}
                      onClick={() => approveUser(u.userId)}
                      className="mr-2 rounded-md bg-emerald-600 px-3 py-1 text-xs font-semibold text-white hover:bg-emerald-700 disabled:opacity-50"
                    >
                      Onayla
                    </button>
                    <button
                      disabled={pending}
                      onClick={() => rejectUser(u.userId)}
                      className="rounded-md border border-slate-200 bg-white px-3 py-1 text-xs font-semibold text-slate-700 hover:bg-slate-50 disabled:opacity-50"
                    >
                      Reddet
                    </button>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </section>

      <section className="rounded-2xl border border-slate-200 bg-white p-4 shadow-sm">
        <h2 className="text-sm font-semibold text-slate-800">Firma başvuruları</h2>
        <table className="mt-3 w-full text-sm">
          <thead className="text-left text-xs uppercase text-slate-500">
            <tr>
              <th className="py-2">Firma</th>
              <th className="py-2">İletişim</th>
              <th className="py-2">Tarih</th>
              <th className="py-2 text-right">İşlem</th>
            </tr>
          </thead>
          <tbody>
            {companies.length === 0 ? (
              <tr>
                <td colSpan={4} className="py-4 text-center text-slate-500">
                  Onay bekleyen firma yok.
                </td>
              </tr>
            ) : (
              companies.map((c) => (
                <tr key={c.companyId} className="border-t border-slate-100">
                  <td className="py-2">{c.name}</td>
                  <td className="py-2 text-slate-600">
                    {c.contactName} · {c.contactEmail}
                  </td>
                  <td className="py-2 text-slate-500">{formatDateTr(c.createdAt)}</td>
                  <td className="py-2 text-right">
                    <button
                      disabled={pending}
                      onClick={() => approveCompany(c.companyId)}
                      className="rounded-md bg-emerald-600 px-3 py-1 text-xs font-semibold text-white hover:bg-emerald-700 disabled:opacity-50"
                    >
                      Onayla
                    </button>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </section>
    </div>
  );
}
