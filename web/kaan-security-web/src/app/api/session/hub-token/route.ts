import { cookies } from 'next/headers';
import { NextResponse } from 'next/server';
import { config } from '@/lib/config';

export async function GET() {
  const store = await cookies();
  const token = store.get(config.cookieNames.access)?.value ?? null;
  if (!token) {
    return NextResponse.json({ token: null }, { status: 401 });
  }
  return NextResponse.json({ token });
}
