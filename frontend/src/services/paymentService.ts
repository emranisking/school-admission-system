import { request } from './api';
import type { HoldDto } from '../types/admission';

/** Pays the admission fee and confirms the reservation. */
export const payForHold = (holdId: string) =>
  request<HoldDto>(`/holds/${holdId}/confirm`, { method: 'POST', auth: 'student' });
