import { formatClock } from '../../utils/date';

export type RingTone = 'ok' | 'low' | 'critical' | 'done';

export function ringTone(seconds: number, total: number): RingTone {
  if (seconds <= 0) return 'done';
  const f = total > 0 ? seconds / total : 0;
  if (f <= 0.15 || seconds <= 10) return 'critical';
  if (f <= 0.4) return 'low';
  return 'ok';
}

/** Spoken-friendly status that changes only at coarse steps, so screen readers are not flooded. */
export function spokenRemaining(seconds: number): string {
  if (seconds <= 0) return 'Time is up';
  if (seconds <= 10) return '10 seconds remaining';
  if (seconds <= 30) return '30 seconds remaining';
  if (seconds <= 60) return '1 minute remaining';
  return `${Math.ceil(seconds / 60)} minutes remaining`;
}

interface Props {
  seconds: number;
  totalSeconds: number;
  size?: number;
  stroke?: number;
  compact?: boolean;
}

export function CountdownRing({ seconds, totalSeconds, size = 220, stroke = 10, compact = false }: Props) {
  const r = (size - stroke) / 2;
  const c = 2 * Math.PI * r;
  const frac = totalSeconds > 0 ? Math.min(1, seconds / totalSeconds) : 0;
  const tone = ringTone(seconds, totalSeconds);
  return (
    <div className={`ring ring--${tone}${compact ? ' ring--compact' : ''}`} style={{ width: size, height: size }}>
      <svg width={size} height={size} viewBox={`0 0 ${size} ${size}`} aria-hidden="true">
        <circle className="ring__track" cx={size / 2} cy={size / 2} r={r} strokeWidth={stroke} fill="none" />
        <circle
          className="ring__bar"
          cx={size / 2}
          cy={size / 2}
          r={r}
          strokeWidth={stroke}
          fill="none"
          strokeLinecap="round"
          strokeDasharray={c}
          strokeDashoffset={c * (1 - frac)}
          transform={`rotate(-90 ${size / 2} ${size / 2})`}
        />
      </svg>
      <div className="ring__label" aria-hidden="true">
        <span className="ring__time">{formatClock(seconds)}</span>
        {!compact && <span className="ring__sub">{seconds >= 3600 ? 'hh : mm : ss' : 'min : sec'}</span>}
      </div>
      {!compact && (
        <span className="sr-only" role="status" aria-live="polite">
          {spokenRemaining(seconds)}
        </span>
      )}
    </div>
  );
}
