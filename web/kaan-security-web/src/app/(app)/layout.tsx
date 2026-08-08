import { redirect } from 'next/navigation';
import { getSession, isSystemAdmin } from '@/lib/session';
import { Sidebar } from '@/components/shell/sidebar';
import { TopBar } from '@/components/shell/topbar';
import { QueryProvider } from '@/components/query-provider';
import { ActivityConsole } from '@/components/activity-console/activity-console';

export default async function AppLayout({ children }: { children: React.ReactNode }) {
  const { user, accessToken } = await getSession();
  if (!user || !accessToken) {
    redirect('/login');
  }
  const admin = isSystemAdmin(user);
  return (
    <QueryProvider>
      <div className="flex min-h-screen">
        <Sidebar isSystemAdmin={admin} />
        <div className="flex flex-1 flex-col">
          <TopBar user={user} />
          <main className="flex-1 px-6 py-6">{children}</main>
        </div>
      </div>
      <ActivityConsole userId={user.userId} companyId={user.companyId} isSystemAdmin={admin} />
    </QueryProvider>
  );
}
