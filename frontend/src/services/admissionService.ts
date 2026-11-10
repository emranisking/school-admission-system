import { request } from './api';
import type {
  ApplicationQuery,
  DashboardDto,
  HoldDto,
  PagedResult,
} from '../types/admission';

export const createHold = (classId: string) =>
  request<HoldDto>(`/classes/${classId}/holds`, { method: 'POST', auth: 'student' });

export const getHold = (holdId: string, signal?: AbortSignal) =>
  request<HoldDto>(`/holds/${holdId}`, { auth: 'student', signal });

export const cancelHold = (holdId: string) =>
  request<HoldDto>(`/holds/${holdId}`, { method: 'DELETE', auth: 'student' });

export const listMyHolds = (signal?: AbortSignal) =>
  request<HoldDto[]>('/me/holds', { auth: 'student', signal });

export const getDashboard = (signal?: AbortSignal) =>
  request<DashboardDto>('/admin/dashboard', { auth: 'admin', signal });

export const listApplications = (q: ApplicationQuery, signal?: AbortSignal) =>
  request<PagedResult<HoldDto>>('/admin/applications', {
    auth: 'admin',
    signal,
    query: {
      search: q.search?.trim(),
      status: q.status && q.status !== 'ALL' ? q.status : undefined,
      classId: q.classId,
      page: q.page,
      pageSize: q.pageSize,
    },
  });
