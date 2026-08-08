import { cookies } from 'next/headers';
import { NextRequest, NextResponse } from 'next/server';
import { config } from '@/lib/config';

async function proxy(request: NextRequest, method: string, path: string[]) {
  const store = await cookies();
  const accessToken = store.get(config.cookieNames.access)?.value;
  const url = new URL(request.url);
  const target = `${config.apiBaseUrl}/${path.join('/')}${url.search}`;

  const headers = new Headers(request.headers);
  headers.delete('host');
  headers.delete('cookie');
  headers.delete('content-length');
  if (accessToken) {
    headers.set('Authorization', `Bearer ${accessToken}`);
  }

  const init: RequestInit = {
    method,
    headers,
    cache: 'no-store'
  };
  if (method !== 'GET' && method !== 'HEAD') {
    init.body = request.body;
    // @ts-expect-error node-fetch experimental duplex
    init.duplex = 'half';
  }

  const backendResponse = await fetch(target, init);
  const responseHeaders = new Headers(backendResponse.headers);
  responseHeaders.delete('content-encoding');
  responseHeaders.delete('transfer-encoding');
  return new NextResponse(backendResponse.body, {
    status: backendResponse.status,
    statusText: backendResponse.statusText,
    headers: responseHeaders
  });
}

export async function GET(request: NextRequest, ctx: { params: Promise<{ path: string[] }> }) {
  const { path } = await ctx.params;
  return proxy(request, 'GET', path);
}
export async function POST(request: NextRequest, ctx: { params: Promise<{ path: string[] }> }) {
  const { path } = await ctx.params;
  return proxy(request, 'POST', path);
}
export async function PUT(request: NextRequest, ctx: { params: Promise<{ path: string[] }> }) {
  const { path } = await ctx.params;
  return proxy(request, 'PUT', path);
}
export async function DELETE(request: NextRequest, ctx: { params: Promise<{ path: string[] }> }) {
  const { path } = await ctx.params;
  return proxy(request, 'DELETE', path);
}
