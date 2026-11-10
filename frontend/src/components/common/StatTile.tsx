import { useEffect, useRef, useState, type ReactNode } from 'react';

export function useCountUp(target: number, duration = 900): number {
  const [value, setValue] = useState(0);
  const from = useRef(0);
  useEffect(() => {
    const reduce = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    if (reduce || from.current === target) {
      from.current = target;
      setValue(target);
      return;
    }
    const start = performance.now();
    const origin = from.current;
    let raf = 0;
    const step = (now: number) => {
      const t = Math.min(1, (now - start) / duration);
      const eased = 1 - Math.pow(1 - t, 3);
      setValue(Math.round(origin + (target - origin) * eased));
      if (t < 1) raf = requestAnimationFrame(step);
      else from.current = target;
    };
    raf = requestAnimationFrame(step);
    return () => {
      cancelAnimationFrame(raf);
      from.current = target;
    };
  }, [target, duration]);
  return value;
}

interface Props {
  label: string;
  value: number;
  icon: ReactNode;
  tone?: 'ink' | 'accent' | 'held' | 'confirmed' | 'expired' | 'neutral';
  index?: number;
}

export function StatTile({ label, value, icon, tone = 'neutral', index = 0 }: Props) {
  const n = useCountUp(value);
  return (
    <div className={`stat stat--${tone}`} style={{ ['--i' as string]: index }}>
      <span className="stat__icon">{icon}</span>
      <span className="stat__value">{n.toLocaleString('en-US')}</span>
      <span className="stat__label">{label}</span>
    </div>
  );
}
