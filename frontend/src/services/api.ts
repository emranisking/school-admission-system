import { ApiError } from '../utils/errors';
import { KEYS, readStore, writeStore } from '../utils/storage';

export const API_BASE: string = import.meta.env.VITE_API_BASE_URL ?? '/api';

export type AuthKind = 'student' | 'admin' | 'none';

export const AUTH_INVALID_EVENT = 'sa:auth-invalid';

interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';
  body?: unknown;
  auth?: AuthKind;
  query?: Record<string, string | number | undefined | null>;
  signal?: AbortSignal;
}

function buildUrl(path: string, query?: RequestOptions['query']): string {
  const base = API_BASE.replace(/\/+$/, '');
  const params = new URLSearchParams();
  if (query) {
    for (const [k, v] of Object.entries(query)) {
      if (v === undefined || v === null || v === '') continue;
      params.set(k, String(v));
    }
  }
  const qs = params.toString();
  return `${base}${path}${qs ? `?${qs}` : ''}`;
}

function invalidateAuth(kind: 'student' | 'admin') {
  if (kind === 'admin') {
    writeStore(KEYS.adminToken, null);
    writeStore(KEYS.adminName, null);
  } else {
    writeStore(KEYS.studentId, null);
  }
  window.dispatchEvent(new CustomEvent(AUTH_INVALID_EVENT, { detail: kind }));
}

export async function request<T>(path: string, opts: RequestOptions = {}): Promise<T> {
  const { method = 'GET', body, auth = 'none', query, signal } = opts;
  const headers: Record<string, string> = { Accept: 'application/json' };
  if (body !== undefined) headers['Content-Type'] = 'application/json';
  if (auth === 'student') {
    const id = readStore(KEYS.studentId);
    if (id) headers['X-Student-Id'] = id;
  } else if (auth === 'admin') {
    const token = readStore(KEYS.adminToken);
    if (token) headers['Authorization'] = `Bearer ${token}`;
  }

  let res: Response;
  try {
    res = await fetch(buildUrl(path, query), {
      method,
      headers,
      body: body !== undefined ? JSON.stringify(body) : undefined,
      signal,
    });
  } catch (e) {
    if (e instanceof DOMException && e.name === 'AbortError') throw e;
    throw new ApiError('NETWORK', 'Network error', 0);
  }

  const text = await res.text();
  let data: unknown = undefined;
  if (text) {
    try {
      data = JSON.parse(text);
    } catch {
      data = undefined;
    }
  }

  if (!res.ok) {
    const err = (data as { error?: { code?: string; message?: string } } | undefined)?.error;
    const code = err?.code ?? (res.status === 401 ? 'UNAUTHORIZED' : `HTTP_${res.status}`);
    if (auth === 'admin' && res.status === 401 && path !== '/admin/auth/login') invalidateAuth('admin');
    if (auth === 'student' && code === 'INVALID_STUDENT_ID') invalidateAuth('student');
    throw new ApiError(code, err?.message ?? res.statusText, res.status);
  }
  return data as T;
}
