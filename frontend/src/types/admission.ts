import type { PaymentDto } from './payment';

export type HoldStatus = 'HELD' | 'CONFIRMED' | 'EXPIRED' | 'CANCELLED';

export const HOLD_STATUSES: HoldStatus[] = ['HELD', 'CONFIRMED', 'EXPIRED', 'CANCELLED'];

export interface HoldDto {
  holdId: string;
  classId: string;
  className: string;
  academicYear: string | number;
  shift: string;
  studentId: string;
  status: HoldStatus;
  createdAt: string;
  expiresAt: string;
  confirmedAt: string | null;
  cancelledAt: string | null;
  remainingSeconds: number;
  serverTime: string;
  payment: PaymentDto | null;
}

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export interface DashboardDto {
  totalClasses: number;
  totalSeats: number;
  availableSeats: number;
  heldSeats: number;
  confirmedAdmissions: number;
  expiredReservations: number;
  cancelledApplications: number;
  recentApplications: HoldDto[];
}

export interface AdminLoginResponse {
  token: string;
  expiresAt: string;
  username: string;
}

export interface ApplicationQuery {
  search?: string;
  status?: HoldStatus | 'ALL' | '';
  classId?: string;
  page?: number;
  pageSize?: number;
}

/** Clock-skew offset in ms: add to Date.now() to get server time. */
export function serverOffsetFrom(hold: Pick<HoldDto, 'serverTime'> | undefined | null): number {
  if (!hold) return 0;
  const t = Date.parse(hold.serverTime);
  return Number.isNaN(t) ? 0 : t - Date.now();
}
