import { Pencil, Plus, RefreshCw } from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { ShiftChip } from '../../components/common/Badge';
import { Button, buttonClass } from '../../components/common/Button';
import { EmptyState } from '../../components/common/EmptyState';
import { SeatBar } from '../../components/common/SeatBar';
import { Skeleton } from '../../components/common/Spinner';
import { useToast } from '../../components/common/Toast';
import { Toggle } from '../../components/common/Toggle';
import { adminListClasses, setAdmissionStatus } from '../../services/classService';
import type { ClassDto } from '../../types/class';
import { formatDuration } from '../../utils/date';
import { errorMessage } from '../../utils/errors';

export default function AdminClassList() {
  const toast = useToast();
  const [classes, setClasses] = useState<ClassDto[] | null>(null);
  const [error, setError] = useState<unknown>(null);
  const [busy, setBusy] = useState<string | null>(null);

  const load = useCallback(async () => {
    try {
      setClasses(await adminListClasses());
      setError(null);
    } catch (e) {
      setError(e);
    }
  }, []);
  useEffect(() => {
    void load();
  }, [load]);

  const toggle = async (c: ClassDto, next: boolean) => {
    setBusy(c.id);
    setClasses((xs) => xs && xs.map((x) => (x.id === c.id ? { ...x, admissionOpen: next } : x)));
    try {
      const updated = await setAdmissionStatus(c.id, next);
      setClasses((xs) => xs && xs.map((x) => (x.id === c.id ? updated : x)));
      toast.success(next ? 'Admission opened' : 'Admission closed', c.displayName);
    } catch (e) {
      setClasses((xs) => xs && xs.map((x) => (x.id === c.id ? { ...x, admissionOpen: !next } : x)));
      toast.error('Could not change admission status', errorMessage(e));
    } finally {
      setBusy(null);
    }
  };

  return (
    <>
      <div className="pagehead">
        <div>
          <h1>Classes</h1>
          <p className="muted">Seats, payment windows and admission status for every class.</p>
        </div>
        <Link to="/admin/classes/new" className={buttonClass('primary')}><Plus size={18} aria-hidden /> New class</Link>
      </div>

      {!classes && !error ? (
        <div className="stack">{[0, 1, 2].map((i) => <Skeleton key={i} h={120} radius={14} />)}</div>
      ) : error && !classes ? (
        <EmptyState title="Classes did not load" message={errorMessage(error)} action={<Button icon={<RefreshCw size={16} />} onClick={() => void load()}>Try again</Button>} />
      ) : classes && classes.length === 0 ? (
        <EmptyState title="No classes yet" message="Create the first class to start accepting applications." action={<Link to="/admin/classes/new" className={buttonClass('primary')}>New class</Link>} />
      ) : (
        <div className="stack">
          {classes!.map((c, i) => (
            <article key={c.id} className="adminclass" style={{ ['--i' as string]: i }}>
              <div className="adminclass__id">
                <h3>{c.displayName}</h3>
                <p className="muted">Academic year {String(c.academicYear)}</p>
                <ShiftChip shift={c.shift} />
              </div>
              <div className="adminclass__seats">
                <SeatBar total={c.totalSeats} confirmed={c.confirmedSeats} held={c.heldSeats} available={c.availableSeats} />
                <ul className="adminclass__nums">
                  <li><b>{c.totalSeats}</b> total</li>
                  <li><b>{c.confirmedSeats}</b> confirmed</li>
                  <li><b>{c.heldSeats}</b> held</li>
                  <li><b>{c.availableSeats}</b> available</li>
                </ul>
              </div>
              <div className="adminclass__meta">
                <span className="muted">Pay within {formatDuration(c.holdDurationSeconds)}</span>
                <Toggle checked={c.admissionOpen} disabled={busy === c.id} onChange={(n) => void toggle(c, n)} label={`Admission for ${c.displayName}`} onText="Open" offText="Closed" />
              </div>
              <Link to={`/admin/classes/${c.id}/edit`} className={buttonClass('secondary', 'sm')}><Pencil size={15} aria-hidden /> Edit</Link>
            </article>
          ))}
        </div>
      )}
    </>
  );
}
