import { Ban, CircleCheck, Clock, TimerOff } from 'lucide-react';
import type { ReactNode } from 'react';
import type { HoldStatus } from '../../types/admission';
import { shiftLabel } from '../../types/class';

const META: Record<HoldStatus, { label: string; icon: ReactNode }> = {
  HELD: { label: 'Held', icon: <Clock size={13} aria-hidden /> },
  CONFIRMED: { label: 'Confirmed', icon: <CircleCheck size={13} aria-hidden /> },
  EXPIRED: { label: 'Expired', icon: <TimerOff size={13} aria-hidden /> },
  CANCELLED: { label: 'Cancelled', icon: <Ban size={13} aria-hidden /> },
};

export function StatusBadge({ status }: { status: HoldStatus }) {
  const m = META[status] ?? { label: status, icon: null };
  return (
    <span className={`badge badge--${status.toLowerCase()}`}>
      {m.icon}
      {m.label}
    </span>
  );
}

export function Badge({
  tone = 'neutral',
  children,
}: {
  tone?: 'neutral' | 'accent' | 'open' | 'closed';
  children: ReactNode;
}) {
  return <span className={`badge badge--${tone}`}>{children}</span>;
}

export function ShiftChip({ shift }: { shift: string }) {
  return <span className={`chip chip--${shift.toLowerCase()}`}>{shiftLabel(shift)} shift</span>;
}
