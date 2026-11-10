export const STUDENT_ID_LENGTH = 17;

export function normalizeStudentId(raw: string): string {
  return raw.replace(/\s+/g, '');
}

export function validateStudentId(raw: string): string | null {
  const v = normalizeStudentId(raw);
  if (!v) return 'Enter the 17-digit birth registration number.';
  if (/\D/.test(v)) return 'Use digits only. Remove letters and symbols.';
  if (v.length !== STUDENT_ID_LENGTH) return `Enter exactly 17 digits. You entered ${v.length}.`;
  return null;
}

export function maskStudentId(id: string): string {
  if (id.length <= 6) return id;
  return `${'•'.repeat(id.length - 6)} ${id.slice(-6)}`;
}

export type HoldUnit = 'minutes' | 'seconds';

export function toSeconds(value: number, unit: HoldUnit): number {
  return Math.round(unit === 'minutes' ? value * 60 : value);
}

export function fromSeconds(seconds: number): { value: number; unit: HoldUnit } {
  if (seconds >= 60 && seconds % 60 === 0) return { value: seconds / 60, unit: 'minutes' };
  return { value: seconds, unit: 'seconds' };
}

export function validateHoldDuration(value: string, unit: HoldUnit): string | null {
  if (value.trim() === '') return 'Enter a duration.';
  const n = Number(value);
  if (!Number.isFinite(n) || n <= 0) return 'Duration must be greater than zero.';
  const s = toSeconds(n, unit);
  if (s < 1 || s > 86400) return 'Duration must be between 1 second and 24 hours.';
  return null;
}

export function validateSeats(value: string): string | null {
  if (value.trim() === '') return 'Enter the number of seats.';
  const n = Number(value);
  if (!Number.isInteger(n)) return 'Seats must be a whole number.';
  if (n < 0 || n > 100000) return 'Seats must be between 0 and 100,000.';
  return null;
}

export function validateClassName(value: string): string | null {
  const v = value.trim();
  if (!v) return 'Enter a class name, for example "Class 6".';
  if (v.length > 100) return 'Keep the class name under 100 characters.';
  return null;
}

export function validateAcademicYear(value: string): string | null {
  const v = value.trim();
  if (!v) return 'Enter an academic year, for example 2026.';
  if (v.length > 20) return 'Keep the academic year under 20 characters.';
  return null;
}
