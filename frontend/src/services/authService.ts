import { request } from './api';
import type { AdminLoginResponse } from '../types/admission';
import { KEYS, readStore, writeStore } from '../utils/storage';

export const getStudentId = () => readStore(KEYS.studentId);
export const setStudentId = (id: string) => writeStore(KEYS.studentId, id);
export const clearStudentId = () => writeStore(KEYS.studentId, null);

export const getAdminToken = () => readStore(KEYS.adminToken);
export const getAdminName = () => readStore(KEYS.adminName);
export const clearAdmin = () => {
  writeStore(KEYS.adminToken, null);
  writeStore(KEYS.adminName, null);
};

export async function adminLogin(username: string, password: string): Promise<AdminLoginResponse> {
  const res = await request<AdminLoginResponse>('/admin/auth/login', {
    method: 'POST',
    body: { username, password },
  });
  writeStore(KEYS.adminToken, res.token);
  writeStore(KEYS.adminName, res.username);
  return res;
}
