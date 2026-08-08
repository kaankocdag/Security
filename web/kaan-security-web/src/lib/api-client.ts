const API_BASE_URL =
  typeof window === 'undefined'
    ? process.env.KAAN_API_BASE_URL || 'http://localhost:5089'
    : '/api-proxy';

export interface ApiError extends Error {
  status: number;
  errorCode?: string;
  problem?: unknown;
}

async function request<T>(
  path: string,
  init: RequestInit & { authToken?: string | null } = {}
): Promise<T> {
  const headers = new Headers(init.headers ?? {});
  headers.set('Accept', 'application/json');
  if (init.body && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json');
  }
  if (init.authToken) {
    headers.set('Authorization', `Bearer ${init.authToken}`);
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...init,
    headers,
    cache: 'no-store'
  });

  if (!response.ok) {
    const problem = await response.json().catch(() => undefined);
    const error = new Error(problem?.detail ?? response.statusText) as ApiError;
    error.status = response.status;
    error.errorCode = problem?.errorCode;
    error.problem = problem;
    throw error;
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export const apiClient = {
  get: <T>(path: string, options?: RequestInit & { authToken?: string | null }) =>
    request<T>(path, { ...options, method: 'GET' }),
  post: <T>(path: string, body?: unknown, options?: RequestInit & { authToken?: string | null }) =>
    request<T>(path, {
      ...options,
      method: 'POST',
      body: body ? JSON.stringify(body) : undefined
    }),
  put: <T>(path: string, body?: unknown, options?: RequestInit & { authToken?: string | null }) =>
    request<T>(path, {
      ...options,
      method: 'PUT',
      body: body ? JSON.stringify(body) : undefined
    }),
  delete: <T>(path: string, options?: RequestInit & { authToken?: string | null }) =>
    request<T>(path, { ...options, method: 'DELETE' })
};
