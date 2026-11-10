import { useCountdown } from '../../hooks/useCountdown';
import { formatClock } from '../../utils/date';
import { ringTone } from './CountdownRing';

interface Props {
  createdAt: string;
  expiresAt: string;
  offset: number;
}

/** Compact live countdown: a thin progress bar with the clock beside it. */
export function MiniCountdown({ createdAt, expiresAt, offset }: Props) {
  const seconds = useCountdown(expiresAt, offset, true);
  const total = Math.max(1, Math.round((Date.parse(expiresAt) - Date.parse(createdAt)) / 1000));
  const tone = ringTone(seconds, total);
  return (
    <span className={`mini mini--${tone}`}>
      <span className="mini__bar" aria-hidden="true">
        <span style={{ transform: `scaleX(${Math.min(1, seconds / total)})` }} />
      </span>
      <span className="mini__time">{formatClock(seconds)}</span>
    </span>
  );
}
