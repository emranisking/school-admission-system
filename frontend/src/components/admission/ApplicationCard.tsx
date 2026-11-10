import { ChevronRight } from 'lucide-react';
import { Link } from 'react-router-dom';
import type { HoldDto } from '../../types/admission';
import { formatDateTime } from '../../utils/date';
import { StatusBadge } from '../common/Badge';
import { MiniCountdown } from '../countdown/MiniCountdown';

export function ApplicationCard({ hold, offset, index = 0 }: { hold: HoldDto; offset: number; index?: number }) {
  const ended = hold.status === 'CONFIRMED' ? hold.confirmedAt : hold.status === 'CANCELLED' ? hold.cancelledAt : hold.status === 'EXPIRED' ? hold.expiresAt : null;
  const endedLabel = hold.status === 'CONFIRMED' ? 'Paid' : hold.status === 'CANCELLED' ? 'Cancelled' : 'Expired';
  return (
    <Link to={`/applications/${hold.holdId}`} className={`appcard appcard--${hold.status.toLowerCase()}`} style={{ ['--i' as string]: index }}>
      <div className="appcard__main">
        <div className="appcard__top">
          <h3 className="appcard__title">{hold.className}</h3>
          <StatusBadge status={hold.status} />
        </div>
        <p className="appcard__meta">Academic year {String(hold.academicYear)}</p>
        <dl className="appcard__times">
          <div>
            <dt>Reserved</dt>
            <dd>{formatDateTime(hold.createdAt)}</dd>
          </div>
          {hold.status === 'HELD' ? (
            <div>
              <dt>Time left to pay</dt>
              <dd>
                <MiniCountdown createdAt={hold.createdAt} expiresAt={hold.expiresAt} offset={offset} />
              </dd>
            </div>
          ) : (
            <div>
              <dt>{endedLabel}</dt>
              <dd>{formatDateTime(ended)}</dd>
            </div>
          )}
        </dl>
      </div>
      <ChevronRight className="appcard__chev" size={20} aria-hidden />
    </Link>
  );
}
