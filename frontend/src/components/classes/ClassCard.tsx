import { Clock, Lock, Users } from 'lucide-react';
import { Link } from 'react-router-dom';
import type { ClassDto } from '../../types/class';
import { formatDuration } from '../../utils/date';
import { ShiftChip } from '../common/Badge';
import { Button, buttonClass } from '../common/Button';
import { SeatBar } from '../common/SeatBar';

interface Props {
  cls: ClassDto;
  activeHoldId?: string;
  applying?: boolean;
  onApply: (cls: ClassDto) => void;
  index?: number;
}

export function ClassCard({ cls, activeHoldId, applying, onApply, index = 0 }: Props) {
  const closed = !cls.admissionOpen;
  const full = cls.availableSeats <= 0;
  return (
    <article className={`classcard${closed ? ' is-closed' : ''}`} style={{ ['--i' as string]: index }}>
      <header className="classcard__head">
        <div>
          <h3 className="classcard__title">{cls.displayName}</h3>
          <p className="classcard__year">Academic year {String(cls.academicYear)}</p>
        </div>
        <ShiftChip shift={cls.shift} />
      </header>

      <div className="classcard__seats">
        <div className="classcard__count">
          <strong>{cls.availableSeats}</strong>
          <span>of {cls.totalSeats} seats open</span>
        </div>
        <SeatBar total={cls.totalSeats} confirmed={cls.confirmedSeats} held={cls.heldSeats} available={cls.availableSeats} legend />
      </div>

      <p className="classcard__window">
        <Clock size={15} aria-hidden /> {formatDuration(cls.holdDurationSeconds)} to pay after you apply
      </p>

      <footer className="classcard__foot">
        {activeHoldId ? (
          <Link className={buttonClass('secondary', 'md', true)} to={`/applications/${activeHoldId}`}>
            View application
          </Link>
        ) : closed ? (
          <Button block variant="secondary" disabled icon={<Lock size={16} />}>Admission closed</Button>
        ) : full ? (
          <Button block variant="secondary" disabled icon={<Users size={16} />}>No seats left</Button>
        ) : (
          <Button block loading={applying} onClick={() => onApply(cls)}>Apply for this class</Button>
        )}
      </footer>
    </article>
  );
}

export function ClassCardSkeleton() {
  return (
    <div className="classcard classcard--skeleton" aria-hidden="true">
      <span className="skeleton" style={{ width: '60%', height: 22 }} />
      <span className="skeleton" style={{ width: '35%', height: 14, marginTop: 10 }} />
      <span className="skeleton" style={{ width: '45%', height: 34, marginTop: 28 }} />
      <span className="skeleton" style={{ width: '100%', height: 10, marginTop: 14, borderRadius: 999 }} />
      <span className="skeleton" style={{ width: '100%', height: 42, marginTop: 28, borderRadius: 10 }} />
    </div>
  );
}
