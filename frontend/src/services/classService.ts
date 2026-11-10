import { request } from './api';
import type {
  ClassDto,
  CreateClassRequest,
  HoldDurationResponse,
  UpdateClassDetailsRequest,
} from '../types/class';

export const listClasses = (signal?: AbortSignal) => request<ClassDto[]>('/classes', { signal });
export const getClass = (id: string) => request<ClassDto>(`/classes/${id}`);

export const adminListClasses = (signal?: AbortSignal) =>
  request<ClassDto[]>('/admin/classes', { auth: 'admin', signal });

export const createClass = (body: CreateClassRequest) =>
  request<ClassDto>('/classes', { method: 'POST', body, auth: 'admin' });

export const updateClassDetails = (id: string, body: UpdateClassDetailsRequest) =>
  request<ClassDto>(`/admin/classes/${id}`, { method: 'PUT', body, auth: 'admin' });

export const updateCapacity = (id: string, totalSeats: number) =>
  request<ClassDto>(`/admin/classes/${id}/capacity`, { method: 'PATCH', body: { totalSeats }, auth: 'admin' });

export const updateHoldDuration = (id: string, holdDurationSeconds: number) =>
  request<HoldDurationResponse>(`/admin/classes/${id}/hold-duration`, {
    method: 'PATCH',
    body: { holdDurationSeconds },
    auth: 'admin',
  });

export const setAdmissionStatus = (id: string, admissionOpen: boolean) =>
  request<ClassDto>(`/admin/classes/${id}/admission-status`, {
    method: 'PATCH',
    body: { admissionOpen },
    auth: 'admin',
  });
