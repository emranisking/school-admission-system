import { useEffect, useRef } from 'react';

/** Calls fn every `ms` while the tab is visible; fires once when the tab becomes visible again. */
export function useVisibleInterval(fn: () => void, ms: number | null) {
  const ref = useRef(fn);
  ref.current = fn;
  useEffect(() => {
    if (ms === null) return;
    const tick = () => {
      if (!document.hidden) ref.current();
    };
    const id = window.setInterval(tick, ms);
    const onVis = () => {
      if (!document.hidden) ref.current();
    };
    document.addEventListener('visibilitychange', onVis);
    return () => {
      window.clearInterval(id);
      document.removeEventListener('visibilitychange', onVis);
    };
  }, [ms]);
}
