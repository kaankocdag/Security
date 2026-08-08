import Link from 'next/link';
import { apiFetch } from '@/lib/api';
import { requireSession } from '@/lib/session';
import { formatDateTr } from '@/lib/utils';

interface ProjectListItem {
  id: string;
  name: string;
  description?: string;
  environmentType: string;
  status: string;
  domainCount: number;
  openFindingCount: number;
  createdAt: string;
}

const ENV_LABELS: Record<string, string> = {
  Production: 'Canlı',
  Staging: 'Ön üretim',
  Testing: 'Test',
  Development: 'Geliştirme'
};

export default async function ProjectsPage() {
  const { accessToken } = await requireSession();
  let projects: ProjectListItem[] = [];
  try {
    projects = await apiFetch<ProjectListItem[]>('/api/projects', {
      accessToken,
      serverSide: true
    });
  } catch {
    projects = [];
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-slate-900">Projeler</h1>
        <Link
          href="/site-test"
          className="rounded-md bg-[color:var(--color-brand-600)] px-3 py-2 text-sm font-semibold text-white hover:bg-[color:var(--color-brand-700)]"
        >
          Yeni proje (sihirbaz)
        </Link>
      </div>
      <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-left text-xs uppercase text-slate-500">
            <tr>
              <th className="px-4 py-2">Ad</th>
              <th className="px-4 py-2">Ortam</th>
              <th className="px-4 py-2">Durum</th>
              <th className="px-4 py-2">Domain</th>
              <th className="px-4 py-2">Açık Bulgu</th>
              <th className="px-4 py-2">Oluşturulma</th>
            </tr>
          </thead>
          <tbody>
            {projects.length === 0 ? (
              <tr>
                <td colSpan={6} className="px-4 py-6 text-center text-slate-500">
                  Henüz proje yok.
                </td>
              </tr>
            ) : (
              projects.map((p) => (
                <tr key={p.id} className="border-t border-slate-100 hover:bg-slate-50">
                  <td className="px-4 py-2 font-medium text-slate-800">{p.name}</td>
                  <td className="px-4 py-2">
                    {ENV_LABELS[p.environmentType] ?? p.environmentType}
                  </td>
                  <td className="px-4 py-2">{p.status}</td>
                  <td className="px-4 py-2">{p.domainCount}</td>
                  <td className="px-4 py-2">{p.openFindingCount}</td>
                  <td className="px-4 py-2 text-slate-600">{formatDateTr(p.createdAt)}</td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
