'use server';

import { cookies } from 'next/headers';
import { redirect } from 'next/navigation';
import { config } from './config';
import { apiFetch } from './api';
import type { SessionUser } from './session';

/** Backend AuthResponse (camelCase JSON) */
interface BackendAuthResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
  tokenType: string;
  user: {
    id: string;
    email: string;
    fullName: string;
    companyId?: string | null;
    companyName?: string | null;
    membershipStatus: string;
    roles: string[];
    avatarPath?: string | null;
  };
}

function toSessionUser(user: BackendAuthResponse['user']): SessionUser {
  return {
    userId: user.id,
    email: user.email,
    fullName: user.fullName,
    companyId: user.companyId,
    companyName: user.companyName,
    membershipStatus: user.membershipStatus,
    roles: user.roles ?? []
  };
}

async function persistSession(response: BackendAuthResponse) {
  const store = await cookies();
  const secure = process.env.NODE_ENV === 'production';
  const sessionUser = toSessionUser(response.user);

  store.set(config.cookieNames.access, response.accessToken, {
    httpOnly: true,
    sameSite: 'lax',
    secure,
    path: '/',
    expires: new Date(response.accessTokenExpiresAt)
  });
  store.set(config.cookieNames.refresh, response.refreshToken, {
    httpOnly: true,
    sameSite: 'lax',
    secure,
    path: '/',
    expires: new Date(response.refreshTokenExpiresAt)
  });
  store.set(config.cookieNames.user, encodeURIComponent(JSON.stringify(sessionUser)), {
    httpOnly: false,
    sameSite: 'lax',
    secure,
    path: '/',
    expires: new Date(response.refreshTokenExpiresAt)
  });
}

function friendlyError(err: unknown): string {
  if (err instanceof Error) {
    const msg = err.message || '';
    if (
      msg === 'fetch failed' ||
      msg.includes('ECONNREFUSED') ||
      msg.includes('ENOTFOUND') ||
      msg.includes('other side closed')
    ) {
      return `API'ye ulaşılamadı (${config.apiBaseUrl}). Visual Studio'da Kaan.SecurityPlatform.Api çalışıyor mu?`;
    }
    return msg;
  }
  return 'Giriş başarısız.';
}

export async function loginAction(formData: FormData): Promise<{ error?: string } | never> {
  const email = String(formData.get('email') || '').trim();
  const password = String(formData.get('password') || '');
  if (!email || !password) {
    return { error: 'E-posta ve şifre zorunludur.' };
  }
  try {
    const response = await apiFetch<BackendAuthResponse>('/api/auth/login', {
      method: 'POST',
      body: { email, password },
      serverSide: true
    });
    if (!response?.accessToken || !response.user) {
      return { error: 'Sunucu beklenmeyen bir yanıt döndürdü.' };
    }
    await persistSession(response);
  } catch (err) {
    return { error: friendlyError(err) };
  }
  redirect('/dashboard');
}

export async function registerAction(formData: FormData): Promise<{ error?: string; info?: string } | never> {
  const email = String(formData.get('email') || '').trim();
  const password = String(formData.get('password') || '');
  const fullName = String(formData.get('fullName') || '').trim();
  const companyName = String(formData.get('companyName') || '').trim();
  const companyDomain = String(formData.get('companyDomain') || '').trim();

  if (!email || !password || !fullName || !companyName) {
    return { error: 'Tüm zorunlu alanları doldurun.' };
  }

  const parts = fullName.split(/\s+/);
  const firstName = parts[0] ?? fullName;
  const lastName = parts.slice(1).join(' ') || '-';

  try {
    await apiFetch('/api/auth/register', {
      method: 'POST',
      body: {
        firstName,
        lastName,
        email,
        password,
        companyName,
        companyWebsiteUrl: companyDomain ? `https://${companyDomain}` : null,
        acceptTerms: true
      },
      serverSide: true
    });
  } catch (err) {
    return { error: friendlyError(err) };
  }
  return {
    info:
      'Başvurunuz alındı. Hesabınız Kaan Security ekibi tarafından onaylandığında bilgilendirileceksiniz.'
  };
}

export async function logoutAction() {
  const store = await cookies();
  const refresh = store.get(config.cookieNames.refresh)?.value;
  if (refresh) {
    try {
      await apiFetch('/api/auth/revoke', {
        method: 'POST',
        body: { refreshToken: refresh },
        serverSide: true
      });
    } catch {
      // ignore
    }
  }
  store.delete(config.cookieNames.access);
  store.delete(config.cookieNames.refresh);
  store.delete(config.cookieNames.user);
  redirect('/login');
}
