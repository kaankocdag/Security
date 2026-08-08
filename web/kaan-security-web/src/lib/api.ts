import { config } from './config';

export interface ApiError extends Error {
  status: number;
  errorCode?: string;
  problem?: unknown;
}

function getBase(serverSide: boolean): string {
  return serverSide ? config.apiBaseUrl : config.publicApiBase;
}

export async function apiFetch<T>(
  path: string,
  options: {
    method?: 'GET' | 'POST' | 'PUT' | 'DELETE';
    body?: unknown;
    accessToken?: string | null;
    formData?: FormData;
    serverSide?: boolean;
    signal?: AbortSignal;
    cache?: RequestCache;
  } = {}
): Promise<T> {
  const {
    method = 'GET',
    body,
    accessToken,
    formData,
    serverSide = typeof window === 'undefined',
    signal,
    cache = 'no-store'
  } = options;
  const headers = new Headers();
  headers.set('Accept', 'application/json');
  if (accessToken) {
    headers.set('Authorization', `Bearer ${accessToken}`);
  }
  let payload: BodyInit | undefined = undefined;
  if (formData) {
    payload = formData;
  } else if (body !== undefined) {
    headers.set('Content-Type', 'application/json');
    payload = JSON.stringify(body);
  }

  const url = `${getBase(serverSide)}${path.startsWith('/') ? path : `/${path}`}`;
  let response: Response;
  try {
    response = await fetch(url, {
      method,
      headers,
      body: payload,
      cache,
      signal
    });
  } catch (cause) {
    const detail = cause instanceof Error ? cause.message : 'network_error';
    const err: ApiError = Object.assign(
      new Error(`fetch failed (${url}): ${detail}`),
      { status: 0, errorCode: 'network_error' }
    );
    throw err;
  }

  if (!response.ok) {
    const problem = await response.json().catch(() => undefined);
    const err: ApiError = Object.assign(new Error(problem?.detail ?? response.statusText), {
      status: response.status,
      errorCode: problem?.errorCode ?? problem?.title,
      problem
    });
    throw err;
  }

  if (response.status === 204) {
    return undefined as T;
  }

  const contentType = response.headers.get('content-type') || '';
  if (contentType.includes('application/json')) {
    return (await response.json()) as T;
  }
  return (await response.text()) as unknown as T;
}
