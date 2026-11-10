export const KEYS = {
  studentId: 'sa.studentId',
  adminToken: 'sa.adminToken',
  adminName: 'sa.adminName',
} as const;

export function readStore(key: string): string | null {
  try {
    return window.localStorage.getItem(key);
  } catch {
    return null;
  }
}

export function writeStore(key: string, value: string | null): void {
  try {
    if (value === null) window.localStorage.removeItem(key);
    else window.localStorage.setItem(key, value);
  } catch {
    /* storage unavailable */
  }
}
