import { ArrowLeft } from 'lucide-react';
import { useState, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { HoldDurationField } from '../../components/classes/HoldDurationField';
import { Button } from '../../components/common/Button';
import { Card } from '../../components/common/Card';
import { FormField } from '../../components/common/FormField';
import { useToast } from '../../components/common/Toast';
import { Toggle } from '../../components/common/Toggle';
import { createClass } from '../../services/classService';
import { SHIFTS, shiftLabel, type Shift } from '../../types/class';
import { formatDuration } from '../../utils/date';
import { errorMessage } from '../../utils/errors';
import {
  toSeconds,
  validateAcademicYear,
  validateClassName,
  validateHoldDuration,
  validateSeats,
  type HoldUnit,
} from '../../utils/validation';

export default function CreateClass() {
  const navigate = useNavigate();
  const toast = useToast();
  const [name, setName] = useState('');
  const [shift, setShift] = useState<Shift>('MORNING');
  const [year, setYear] = useState(String(new Date().getFullYear()));
  const [seats, setSeats] = useState('');
  const [dur, setDur] = useState('30');
  const [unit, setUnit] = useState<HoldUnit>('minutes');
  const [open, setOpen] = useState(true);
  const [errors, setErrors] = useState<Record<string, string | null>>({});
  const [busy, setBusy] = useState(false);

  const secs = validateHoldDuration(dur, unit) ? null : toSeconds(Number(dur), unit);
  const seatsOk = !validateSeats(seats);
  const preview = `${name.trim() || 'Class name'} - ${shiftLabel(shift)} Shift • ${seatsOk ? Number(seats) : '--'} seats • ${
    secs !== null ? `${formatDuration(secs)} = ${secs} s` : '--'
  }`;

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    const errs = {
      name: validateClassName(name),
      year: validateAcademicYear(year),
      seats: validateSeats(seats),
      dur: validateHoldDuration(dur, unit),
    };
    setErrors(errs);
    if (Object.values(errs).some(Boolean) || secs === null) return;
    setBusy(true);
    try {
      await createClass({
        name: name.trim(),
        shift,
        academicYear: year.trim(),
        totalSeats: Number(seats),
        holdDurationSeconds: secs,
        admissionOpen: open,
      });
      toast.success('Class created', preview.split(' • ')[0]);
      navigate('/admin/classes');
    } catch (err) {
      toast.error('Could not create the class', errorMessage(err));
    } finally {
      setBusy(false);
    }
  };

  return (
    <>
      <Link to="/admin/classes" className="backlink"><ArrowLeft size={16} aria-hidden /> Classes</Link>
      <div className="pagehead"><div><h1>New class</h1><p className="muted">Set up a class and its payment window.</p></div></div>

      <form className="formgrid" onSubmit={submit} noValidate>
        <Card title="Class details">
          <div className="fields">
            <FormField id="name" label="Class name" error={errors.name} hint='For example "Class 6".'>
              <input id="name" className="input" value={name} onChange={(e) => setName(e.target.value)} aria-describedby="name-msg" aria-invalid={!!errors.name} />
            </FormField>
            <FormField id="shift" label="Shift">
              <select id="shift" className="select" value={shift} onChange={(e) => setShift(e.target.value as Shift)}>
                {SHIFTS.map((s) => <option key={s.value} value={s.value}>{s.label}</option>)}
              </select>
            </FormField>
            <FormField id="year" label="Academic year" error={errors.year}>
              <input id="year" className="input" value={year} onChange={(e) => setYear(e.target.value)} aria-describedby="year-msg" aria-invalid={!!errors.year} />
            </FormField>
            <FormField id="seats" label="Total seats" error={errors.seats} hint="0 to 100,000.">
              <input id="seats" className="input" type="number" inputMode="numeric" min={0} value={seats} onChange={(e) => setSeats(e.target.value)} aria-describedby="seats-msg" aria-invalid={!!errors.seats} />
            </FormField>
            <HoldDurationField id="dur" value={dur} unit={unit} error={errors.dur} onValue={setDur} onUnit={setUnit} />
            <div className="field">
              <span className="field__label">Admission</span>
              <Toggle checked={open} onChange={setOpen} label="Admission open" onText="Open for applications" offText="Closed for now" />
            </div>
          </div>
        </Card>

        <aside className="summary">
          <p className="summary__label">Summary</p>
          <p className="summary__text" aria-live="polite">{preview}</p>
          <p className="summary__sub">{open ? 'Students can apply as soon as you save.' : 'Students cannot apply until you open admission.'}</p>
          <Button type="submit" size="lg" block loading={busy}>Create class</Button>
          <Link to="/admin/classes" className="linkbtn linkbtn--center">Cancel</Link>
        </aside>
      </form>
    </>
  );
}
