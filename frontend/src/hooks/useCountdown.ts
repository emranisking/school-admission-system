import { useEffect, useRef, useState } from 'react';

function compute(expiresAt: string | null | undefined, offset: number): number {
  if (!expiresAt) return 0;
  const end = Date.parse(expiresAt);
  if (Number.isNaN(end)) return 0;
  return Math.max(0, Math.ceil((end - (Date.now() + offset)) / 1000));
}

/**
 * Seconds remaining until `expiresAt`, measured against server time
 * (Date.now() + offset). Recomputed from the deadline on every tick, so it never drifts
 * and survives tab throttling and page reloads.
 */
export function useCountdown(
  expiresAt: string | null | undefined,
  offset: number,
  active = true,
  onZero?: () => void,
): number {
  const [seconds, setSeconds] = useState(() => compute(expiresAt, offset));
  const zeroRef = useRef(onZero);
  zeroRef.current = onZero;
  const firedFor = useRef<string | null>(null);

  useEffect(() => {
    const update = () => {
      const s = compute(expiresAt, offset);
      setSeconds((prev) => (prev === s ? prev : s));
      if (active && s === 0 && expiresAt && firedFor.current !== expiresAt) {
        firedFor.current = expiresAt;
        zeroRef.current?.();
      }
    };
    update();
    if (!active) return;
    const id = window.setInterval(update, 250);
    const onVis = () => update();
    document.addEventListener('visibilitychange', onVis);
    return () => {
      window.clearInterval(id);
      document.removeEventListener('visibilitychange', onVis);
    };
  }, [expiresAt, offset, active]);

  return seconds;
}
