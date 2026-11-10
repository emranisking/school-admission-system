import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { useNavigate } from 'react-router-dom';
import { ToastProvider, useToast } from '../components/common/Toast';
import { AUTH_INVALID_EVENT } from '../services/api';
import {
  clearAdmin,
  clearStudentId,
  getAdminName,
  getAdminToken,
  getStudentId,
  setStudentId as persistStudentId,
} from '../services/authService';

interface AuthState {
  studentId: string | null;
  adminToken: string | null;
  adminName: string | null;
  signInStudent: (id: string) => void;
  signOutStudent: () => void;
  syncAdmin: () => void;
  signOutAdmin: () => void;
}

const AuthCtx = createContext<AuthState | null>(null);

export function useAuth(): AuthState {
  const v = useContext(AuthCtx);
  if (!v) throw new Error('useAuth must be used inside AuthProvider');
  return v;
}

function AuthProvider({ children }: { children: ReactNode }) {
  const navigate = useNavigate();
  const toast = useToast();
  const [studentId, setStudentId] = useState<string | null>(() => getStudentId());
  const [adminToken, setAdminToken] = useState<string | null>(() => getAdminToken());
  const [adminName, setAdminName] = useState<string | null>(() => getAdminName());

  const signInStudent = useCallback((id: string) => {
    persistStudentId(id);
    setStudentId(id);
  }, []);
  const signOutStudent = useCallback(() => {
    clearStudentId();
    setStudentId(null);
  }, []);
  const syncAdmin = useCallback(() => {
    setAdminToken(getAdminToken());
    setAdminName(getAdminName());
  }, []);
  const signOutAdmin = useCallback(() => {
    clearAdmin();
    setAdminToken(null);
    setAdminName(null);
  }, []);

  useEffect(() => {
    const onInvalid = (e: Event) => {
      const kind = (e as CustomEvent<string>).detail;
      if (kind === 'admin') {
        setAdminToken(null);
        setAdminName(null);
        toast.info('Session ended', 'Sign in again to continue.');
        navigate('/admin/login', { replace: true });
      } else {
        setStudentId(null);
        toast.error('Student ID not accepted', 'Enter your 17-digit birth registration number again.');
        navigate('/', { replace: true });
      }
    };
    window.addEventListener(AUTH_INVALID_EVENT, onInvalid);
    return () => window.removeEventListener(AUTH_INVALID_EVENT, onInvalid);
  }, [navigate, toast]);

  const value = useMemo(
    () => ({ studentId, adminToken, adminName, signInStudent, signOutStudent, syncAdmin, signOutAdmin }),
    [studentId, adminToken, adminName, signInStudent, signOutStudent, syncAdmin, signOutAdmin],
  );
  return <AuthCtx.Provider value={value}>{children}</AuthCtx.Provider>;
}

export function Providers({ children }: { children: ReactNode }) {
  return (
    <ToastProvider>
      <AuthProvider>{children}</AuthProvider>
    </ToastProvider>
  );
}
