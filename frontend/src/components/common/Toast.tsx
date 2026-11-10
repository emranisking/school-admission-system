import { CircleAlert, CircleCheck, Info, X } from 'lucide-react';
import { createContext, useCallback, useContext, useMemo, useRef, useState, type ReactNode } from 'react';

type Tone = 'success' | 'error' | 'info';
interface ToastItem {
  id: number;
  tone: Tone;
  title: string;
  message?: string;
  leaving?: boolean;
}
interface ToastApi {
  success: (title: string, message?: string) => void;
  error: (title: string, message?: string) => void;
  info: (title: string, message?: string) => void;
}

const Ctx = createContext<ToastApi | null>(null);

export function useToast(): ToastApi {
  const v = useContext(Ctx);
  if (!v) throw new Error('useToast must be used inside ToastProvider');
  return v;
}

const ICON: Record<Tone, ReactNode> = {
  success: <CircleCheck size={20} aria-hidden />,
  error: <CircleAlert size={20} aria-hidden />,
  info: <Info size={20} aria-hidden />,
};

export function ToastProvider({ children }: { children: ReactNode }) {
  const [items, setItems] = useState<ToastItem[]>([]);
  const nextId = useRef(1);

  const dismiss = useCallback((id: number) => {
    setItems((xs) => xs.map((x) => (x.id === id ? { ...x, leaving: true } : x)));
    window.setTimeout(() => setItems((xs) => xs.filter((x) => x.id !== id)), 220);
  }, []);

  const push = useCallback(
    (tone: Tone, title: string, message?: string) => {
      const id = nextId.current++;
      setItems((xs) => [...xs.slice(-3), { id, tone, title, message }]);
      window.setTimeout(() => dismiss(id), tone === 'error' ? 7000 : 4500);
    },
    [dismiss],
  );

  const api = useMemo<ToastApi>(
    () => ({
      success: (t, m) => push('success', t, m),
      error: (t, m) => push('error', t, m),
      info: (t, m) => push('info', t, m),
    }),
    [push],
  );

  return (
    <Ctx.Provider value={api}>
      {children}
      <div className="toasts" role="region" aria-label="Notifications">
        <div aria-live="polite" aria-atomic="false">
          {items.map((t) => (
            <div key={t.id} className={`toast toast--${t.tone}${t.leaving ? ' is-leaving' : ''}`} role={t.tone === 'error' ? 'alert' : 'status'}>
              <span className="toast__icon">{ICON[t.tone]}</span>
              <div className="toast__body">
                <strong>{t.title}</strong>
                {t.message && <span>{t.message}</span>}
              </div>
              <button className="toast__close" onClick={() => dismiss(t.id)} aria-label="Dismiss notification">
                <X size={16} />
              </button>
            </div>
          ))}
        </div>
      </div>
    </Ctx.Provider>
  );
}
