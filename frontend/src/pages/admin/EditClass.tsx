import { ArrowLeft, RefreshCw } from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { HoldDurationField } from '../../components/classes/HoldDurationField';
import { ShiftChip } from '../../components/common/Badge';
import { Button, buttonClass } from '../../components/common/Button';
import { Card } from '../../components/common/Card';
import { EmptyState } from '../../components/common/EmptyState';
import { FormField } from '../../components/common/FormField';
import { SeatBar } from '../../components/common/SeatBar';
import { Skeleton } from '../../components/common/Spinner';
import { useToast } from '../../components/common/Toast';
import { Toggle } from '../../components/common/Toggle';
import {
  setAdmissionStatus,
  updateCapacity,
  updateClassDetails,
  updateHoldDuration,
} from '../../services/classService';
import { getClass } from '../../services/classService';
import { SHIFTS, type ClassDto, type Shift } from '../../types/class';
import { formatDuration } from '../../utils/date';
import { errorCode, errorMessage } from '../../utils/errors';
import {
  fromSeconds,
  toSeconds,
  validateAcademicYear,
  validateClassName,
  validateHoldDuration,
  validateSeats,
  type HoldUnit,
} from '../../utils/validation';

export default function EditClass() {
  const { classId = '' } = useParams();
  const toast = useToast();
  const [cls, setCls] = useState<ClassDto | null>(null);
  const [loadError, setLoadError] = useState<unknown>(null);

  const [name, setName] = useState('');
  const [year, setYear] = useState('');
  const [shift, setShift] = useState<Shift>('MORNING');
  const [seats, setSeats] = useState('');
  const [dur, setDur] = useState('');
  const [unit, setUnit] = useState<HoldUnit>('minutes');
  const [errors, setErrors] = useState<Record<string, string | null>>({});
  const [busy, setBusy] = useState<string | null>(null);

  const hydrate = useCallback((c: ClassDto, parts: Array<'details' | 'capacity' | 'duration'> = ['details', 'capacity', 'duration']) => {
    setCls(c);
    if (parts.includes('details')) {
      setName(c.name);
      setYear(String(c.academicYear));
      setShift(c.shift as Shift);
    }
    if (parts.includes('capacity')) setSeats(String(c.totalSeats));
    if (parts.includes('duration')) {
      const d = fromSeconds(c.holdDurationSeconds);
      setDur(String(d.value));
      setUnit(d.unit);
    }
  }, []);

  const load = useCallback(async () => {
    setLoadError(null);
    try {
      hydrate(await getClass(classId));
    } catch (e) {
      setLoadError(e);
    }
  }, [classId, hydrate]);
  useEffect(() => {
    void load();
  }, [load]);

  const run = async (key: string, fn: () => Promise<void>) => {
    setBusy(key);
    try {
      await fn();
    } finally {
      setBusy(null);
    }
  };

  const saveDetails = () => {
    const errs = { name: validateClassName(name), year: validateAcademicYear(year) };
    setErrors((p) => ({ ...p, ...errs }));
    if (errs.name || errs.year) return;
    void run('details', async () => {
      try {
        hydrate(await updateClassDetails(classId, { name: name.trim(), academicYear: year.trim(), shift }), ['details']);
        toast.success('Details saved');
      } catch (e) {
        toast.error('Could not save details', errorMessage(e));
      }
    });
  };

  const saveCapacity = () => {
    const err = validateSeats(seats);
    setErrors((p) => ({ ...p, seats: err }));
    if (err) return;
    void run('capacity', async () => {
      try {
        hydrate(await updateCapacity(classId, Number(seats)), ['capacity']);
        toast.success('Capacity saved', `${seats} total seats.`);
      } catch (e) {
        if (errorCode(e) === 'CAPACITY_BELOW_COMMITMENTS') {
          const min = (cls?.heldSeats ?? 0) + (cls?.confirmedSeats ?? 0);
          setErrors((p) => ({ ...p, seats: `Total seats cannot be lower than ${min}, the seats already held or confirmed.` }));
        }
        toast.error('Could not change capacity', errorMessage(e));
      }
    });
  };

  const saveDuration = () => {
    const err = validateHoldDuration(dur, unit);
    setErrors((p) => ({ ...p, dur: err }));
    if (err) return;
    void run('duration', async () => {
      try {
        const res = await updateHoldDuration(classId, toSeconds(Number(dur), unit));
        setCls((c) => (c ? { ...c, holdDurationSeconds: res.holdDurationSeconds } : c));
        toast.success('Payment window saved', res.message);
      } catch (e) {
        toast.error('Could not save payment window', errorMessage(e));
      }
    });
  };

  const toggleStatus = (next: boolean) => {
    void run('status', async () => {
      try {
        const c = await setAdmissionStatus(classId, next);
        setCls(c);
        toast.success(next ? 'Admission opened' : 'Admission closed');
      } catch (e) {
        toast.error('Could not change admission status', errorMessage(e));
      }
    });
  };

  if (loadError) {
    return (
      <EmptyState
        title="Class not found"
        message={errorMessage(loadError)}
        action={<div className="row"><Button icon={<RefreshCw size={16} />} onClick={() => void load()}>Try again</Button><Link className={buttonClass('secondary')} to="/admin/classes">Back to classes</Link></div>}
      />
    );
  }
  if (!cls) return <div className="stack"><Skeleton h={90} radius={14} /><Skeleton h={220} radius={14} /><Skeleton h={160} radius={14} /></div>;

  const committed = cls.heldSeats + cls.confirmedSeats;

  return (
    <>
      <Link to="/admin/classes" className="backlink"><ArrowLeft size={16} aria-hidden /> Classes</Link>
      <div className="pagehead">
        <div><h1>{cls.displayName}</h1><p className="muted">Academic year {String(cls.academicYear)}</p></div>
        <ShiftChip shift={cls.shift} />
      </div>

      <Card className="section-gap" title="Current seats">
        <SeatBar size="lg" total={cls.totalSeats} confirmed={cls.confirmedSeats} held={cls.heldSeats} available={cls.availableSeats} legend />
        <p className="muted small">{cls.totalSeats} total. Payment window is {formatDuration(cls.holdDurationSeconds)}.</p>
      </Card>

      <div className="editgrid">
        <Card title="Details" description="Name, year and shift shown to students.">
          <div className="fields">
            <FormField id="e-name" label="Class name" error={errors.name}>
              <input id="e-name" className="input" value={name} onChange={(e) => setName(e.target.value)} aria-describedby="e-name-msg" aria-invalid={!!errors.name} />
            </FormField>
            <FormField id="e-year" label="Academic year" error={errors.year}>
              <input id="e-year" className="input" value={year} onChange={(e) => setYear(e.target.value)} aria-describedby="e-year-msg" aria-invalid={!!errors.year} />
            </FormField>
            <FormField id="e-shift" label="Shift">
              <select id="e-shift" className="select" value={shift} onChange={(e) => setShift(e.target.value as Shift)}>
                {SHIFTS.map((s) => <option key={s.value} value={s.value}>{s.label}</option>)}
              </select>
            </FormField>
          </div>
          <div className="card__foot"><Button loading={busy === 'details'} onClick={saveDetails}>Save details</Button></div>
        </Card>

        <Card title="Capacity" description="Total seats offered for this class.">
          <div className="fields">
            <FormField id="e-seats" label="Total seats" error={errors.seats} hint={`Minimum ${committed}: ${cls.confirmedSeats} confirmed + ${cls.heldSeats} held.`}>
              <input id="e-seats" className="input" type="number" min={0} value={seats} onChange={(e) => setSeats(e.target.value)} aria-describedby="e-seats-msg" aria-invalid={!!errors.seats} />
            </FormField>
          </div>
          <div className="card__foot"><Button loading={busy === 'capacity'} onClick={saveCapacity}>Save capacity</Button></div>
        </Card>

        <Card title="Payment window" description="Applies to future reservations only. Seats already held keep their current deadline.">
          <div className="fields">
            <HoldDurationField id="e-dur" value={dur} unit={unit} error={errors.dur} onValue={setDur} onUnit={setUnit} />
          </div>
          <div className="card__foot"><Button loading={busy === 'duration'} onClick={saveDuration}>Save payment window</Button></div>
        </Card>

        <Card title="Admission status" description="Closed classes stay visible but students cannot apply.">
          <Toggle checked={cls.admissionOpen} disabled={busy === 'status'} onChange={toggleStatus} label="Admission open" onText="Open for applications" offText="Closed" />
        </Card>
      </div>
    </>
  );
}
