import Link from 'next/link';
import { requireSession, isSystemAdmin } from '@/lib/session';
import { redirect } from 'next/navigation';

const links = [
  { href: '/hackerone', label: 'Overview' },
  { href: '/hackerone/targets', label: 'Targets' },
  { href: '/hackerone/candidates', label: 'Candidates' },
  { href: '/hackerone/report-builder', label: 'Report Builder' },
  { href: '/hackerone/programs', label: 'Programs' },
  { href: '/hackerone/submissions', label: 'Submissions' },
  { href: '/hackerone/settings', label: 'Settings' }
];

export default async function HackerOneLayout({ children }: { children: React.ReactNode }) {
  const { user } = await requireSession();
  if (!isSystemAdmin(user)) {
    redirect('/dashboard');
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-slate-900">HackerOne</h1>
        <p className="mt-1 max-w-3xl text-sm text-slate-600">
          Targets → Candidate Assessment → 💰 para potansiyeli → Rapor hazırla (EN H1). TR yalnızca
          iç inceleme. Otomatik submit yok.
        </p>
        <nav className="mt-4 flex flex-wrap gap-2 border-b border-slate-200 pb-2">
          {links.map((l) => (
            <Link
              key={l.href}
              href={l.href}
              className="rounded-md px-3 py-1.5 text-sm font-medium text-slate-600 hover:bg-slate-100 hover:text-slate-900"
            >
              {l.label}
            </Link>
          ))}
        </nav>
      </div>
      {children}
    </div>
  );
}
