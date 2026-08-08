import { NextRequest, NextResponse } from 'next/server';
import { config as appConfig } from './lib/config';

const PROTECTED_PREFIXES = [
  '/dashboard',
  '/projects',
  '/domains',
  '/scans',
  '/findings',
  '/reports',
  '/knowledge',
  '/site-test',
  '/admin',
  '/notifications',
  '/help'
];

const PENDING_ALLOWED = ['/dashboard', '/help'];

export function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl;
  const requiresAuth = PROTECTED_PREFIXES.some((prefix) => pathname.startsWith(prefix));
  if (!requiresAuth) return NextResponse.next();

  const accessCookie = request.cookies.get(appConfig.cookieNames.access);
  const userCookie = request.cookies.get(appConfig.cookieNames.user);

  if (!accessCookie || !userCookie) {
    const url = request.nextUrl.clone();
    url.pathname = '/login';
    url.searchParams.set('redirect', pathname);
    const response = NextResponse.redirect(url);
    // Yarım kalmış oturumu temizle (yalnızca user çerezi varsa döngü oluşmasın)
    if (!accessCookie && userCookie) {
      response.cookies.delete(appConfig.cookieNames.user);
      response.cookies.delete(appConfig.cookieNames.refresh);
    }
    return response;
  }

  try {
    const user = JSON.parse(decodeURIComponent(userCookie.value)) as {
      membershipStatus?: string;
    };
    if (user.membershipStatus !== 'Approved' && !PENDING_ALLOWED.includes(pathname)) {
      const url = request.nextUrl.clone();
      url.pathname = '/dashboard';
      return NextResponse.redirect(url);
    }
  } catch {
    // cookie corrupted
  }

  return NextResponse.next();
}

export const config = {
  matcher: [
    '/dashboard/:path*',
    '/projects/:path*',
    '/domains/:path*',
    '/scans/:path*',
    '/findings/:path*',
    '/reports/:path*',
    '/knowledge/:path*',
    '/site-test/:path*',
    '/admin/:path*',
    '/notifications/:path*',
    '/help/:path*'
  ]
};
