import { useEffect, useState } from 'react';

interface Props {
  total: number;
  confirmed: number;
  held: number;
  available: number;
  legend?: boolean;
  size?: 'md' | 'lg';
}

export function SeatBar({ total, confirmed, held, available, legend = false, size = 'md' }: Props) {
  const [shown, setShown] = useState(false);
  useEffect(() => {
    const id = requestAnimationFrame(() => setShown(true));
    return () => cancelAnimationFrame(id);
  }, []);
  const base = Math.max(total, confirmed + held + available, 1);
  const pct = (n: number) => `${shown ? (Math.max(0, n) / base) * 100 : 0}%`;
  return (
    <div>
      <div
        className={`seatbar seatbar--${size}`}
        role="img"
        aria-label={`${confirmed} confirmed, ${held} held, ${available} available of ${total} seats`}
      >
        <span className="seatbar__seg seatbar__seg--confirmed" style={{ width: pct(confirmed) }} />
        <span className="seatbar__seg seatbar__seg--held" style={{ width: pct(held) }} />
        <span className="seatbar__seg seatbar__seg--available" style={{ width: pct(available) }} />
      </div>
      {legend && (
        <ul className="seatbar__legend">
          <li><i className="dot dot--confirmed" />{confirmed} confirmed</li>
          <li><i className="dot dot--held" />{held} held</li>
          <li><i className="dot dot--available" />{available} available</li>
        </ul>
      )}
    </div>
  );
}
