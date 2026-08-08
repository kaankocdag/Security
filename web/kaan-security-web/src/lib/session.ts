import 'server-only';
import { cookies } from 'next/headers';
import { config } from './config';

export interface SessionUser {
  userId: string;
  email: string;
  fullName?: string;
  companyId?: string | null;
  companyName?: string | null;
  membershipStatus: string;
  roles: string[];
}

export async function getSession(): Promise<{
  accessToken: string | null;
  user: SessionUser | null;
}> {
  const store = await cookies();
  const accessToken = store.get(config.cookieNames.access)?.value ?? null;
  const rawUser = store.get(config.cookieNames.user)?.value;
  let user: SessionUser | null = null;
  if (rawUser) {
    try {
      user = JSON.parse(decodeURIComponent(rawUser)) as SessionUser;
    } catch {
      user = null;
    }
  }
  return { accessToken, user };
}

export async function requireSession(): Promise<{ accessToken: string; user: SessionUser }> {
  const session = await getSession();
  if (!session.accessToken || !session.user) {
    throw new Error('UNAUTHENTICATED');
  }
  return { accessToken: session.accessToken, user: session.user };
}

export function isSystemAdmin(user: SessionUser | null): boolean {
  return user?.roles?.includes('SystemAdmin') ?? false;
}
