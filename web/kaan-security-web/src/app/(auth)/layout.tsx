import { getSession } from '@/lib/session';
import { redirect } from 'next/navigation';
import Link from 'next/link';
import { ShieldCheck } from 'lucide-react';

export default async function AuthLayout({ children }: { children: React.ReactNode }) {
  const { user, accessToken } = await getSession();

  // Yalnızca tam oturumda yönlendir. Eksik access + kalan user çerezi
  // login ↔ dashboard döngüsü yaratır ve tarayıcı boş sayfa gösterir.
  if (user && accessToken) {
    redirect('/dashboard');
  }

  return (
    <div className="min-h-screen bg-slate-50">
      <header className="mx-auto flex max-w-6xl items-center justify-between px-6 py-4">
        <Link href="/" className="flex items-center gap-2 text-sm font-semibold text-slate-800">
          <ShieldCheck className="text-[color:var(--color-brand-600)]" size={20} />
          Kaan Security Platform
        </Link>
        <div className="text-xs text-slate-500">Türkçe · Zarar vermeyen kontroller</div>
      </header>
      <div className="mx-auto flex max-w-md flex-col gap-6 px-6 py-10">{children}</div>
    </div>
  );
}
