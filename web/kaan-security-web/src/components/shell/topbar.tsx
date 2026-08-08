import Link from 'next/link';
import { logoutAction } from '@/lib/auth-actions';
import { CircleHelp, LogOut, User } from 'lucide-react';
import type { SessionUser } from '@/lib/session';
import { HelpTip } from '@/components/ui/help-tip';

export function TopBar({ user }: { user: SessionUser }) {
  return (
    <header className="flex items-center justify-between border-b border-slate-200 bg-white/70 px-4 py-3">
      <div>
        <div className="flex items-center text-xs uppercase tracking-widest text-slate-500">
          {user.companyName ?? 'Firma atanmamış'}
          <HelpTip
            text="Oturumunuzun bağlı olduğu firma. SystemAdmin seed sonrası Demo Teknoloji’ye bağlanır; claim için çıkış→giriş yapın."
            side="bottom"
          />
        </div>
        <div className="text-sm font-semibold text-slate-800">
          Hoş geldin, {user.fullName ?? user.email}
        </div>
        <div className="mt-0.5 text-[11px] text-slate-500">
          Durum:{' '}
          <span
            className={
              user.membershipStatus === 'Approved'
                ? 'text-emerald-600'
                : user.membershipStatus === 'Pending'
                  ? 'text-amber-600'
                  : 'text-rose-600'
            }
          >
            {statusLabel(user.membershipStatus)}
          </span>
          {user.roles?.length ? ' · ' + user.roles.join(', ') : ''}
        </div>
      </div>
      <div className="flex items-center gap-3">
        <Link
          href="/help"
          className="flex items-center gap-1 rounded-md border border-slate-200 bg-white px-3 py-1.5 text-xs font-medium text-slate-700 hover:bg-slate-50"
        >
          <CircleHelp size={14} />
          Rehber
          <HelpTip text="Admin ve kullanıcı için adım adım kullanım rehberi." side="bottom" />
        </Link>
        <div className="hidden items-center gap-2 rounded-full border border-slate-200 bg-white px-3 py-1.5 text-xs text-slate-600 md:flex">
          <User size={14} />
          {user.email}
        </div>
        <form action={logoutAction}>
          <button
            type="submit"
            className="flex items-center gap-1 rounded-md border border-slate-200 bg-white px-3 py-1.5 text-xs font-medium text-slate-700 hover:bg-slate-50"
          >
            <LogOut size={14} />
            Çıkış
            <HelpTip text="Oturumu kapatır; çerezleri temizler." side="bottom" />
          </button>
        </form>
      </div>
    </header>
  );
}

function statusLabel(status: string) {
  switch (status) {
    case 'Approved':
      return 'Onaylı';
    case 'Pending':
      return 'Onay bekliyor';
    case 'Rejected':
      return 'Reddedildi';
    case 'Suspended':
      return 'Askıya alındı';
    default:
      return status;
  }
}
