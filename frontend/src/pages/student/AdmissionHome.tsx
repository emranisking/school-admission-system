import { ArrowRight, ShieldCheck } from 'lucide-react';
import { useState, type FormEvent } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../../app/providers';
import { Brand } from '../../components/common/Brand';
import { Button } from '../../components/common/Button';
import { FormField } from '../../components/common/FormField';
import { CountdownRing } from '../../components/countdown/CountdownRing';
import { normalizeStudentId, STUDENT_ID_LENGTH, validateStudentId } from '../../utils/validation';

export default function AdmissionHome() {
  const { studentId, signInStudent } = useAuth();
  const navigate = useNavigate();
  const loc = useLocation();
  const [value, setValue] = useState(studentId ?? '');
  const [error, setError] = useState<string | null>(null);
  const [touched, setTouched] = useState(false);

  const digits = normalizeStudentId(value).replace(/\D/g, '').length;

  const onChange = (v: string) => {
    setValue(v);
    if (touched) setError(validateStudentId(v));
  };

  const submit = (e: FormEvent) => {
    e.preventDefault();
    setTouched(true);
    const err = validateStudentId(value);
    setError(err);
    if (err) return;
    signInStudent(normalizeStudentId(value));
    const from = (loc.state as { from?: string } | null)?.from;
    navigate(from && from.startsWith('/') ? from : '/classes');
  };

  return (
    <div className="home">
      <header className="home__bar container">
        <Brand />
        <Link to="/admin/login" className="home__admin">Admin sign in</Link>
      </header>

      <main className="home__hero container">
        <div className="home__copy">
          <h1 className="home__title">Hold a seat. Pay before the clock runs out.</h1>
          <p className="home__lead">
            Pick a class, reserve a seat, and pay the admission fee within the class's payment window.
            Your seat stays yours while the timer runs.
          </p>

          <form className="home__form" onSubmit={submit} noValidate>
            <FormField
              id="student-id"
              label="Birth registration number"
              error={error}
              hint="17 digits, as printed on the birth certificate."
            >
              <input
                id="student-id"
                className="input input--xl mono"
                inputMode="numeric"
                autoComplete="off"
                autoFocus
                placeholder="00000000000000000"
                value={value}
                onChange={(e) => onChange(e.target.value)}
                onBlur={() => value && (setTouched(true), setError(validateStudentId(value)))}
                aria-describedby="student-id-msg"
                aria-invalid={!!error}
              />
            </FormField>
            <div className="digits" aria-hidden="true">
              {Array.from({ length: STUDENT_ID_LENGTH }, (_, i) => (
                <i key={i} className={i < digits ? 'is-on' : ''} />
              ))}
              <span>{Math.min(digits, 99)}/{STUDENT_ID_LENGTH}</span>
            </div>
            <Button type="submit" size="lg" block icon={<ArrowRight size={18} />}>
              Continue to classes
            </Button>
            <p className="home__note"><ShieldCheck size={15} aria-hidden /> Only your registration number is used to find your applications.</p>
          </form>
        </div>

        <div className="home__visual" aria-hidden="true">
          <div className="ticket">
            <div className="ticket__top">
              <span>Seat reserved</span>
              <b>Class 6 - Morning Shift</b>
            </div>
            <div className="ticket__ring">
              <CountdownRing seconds={1620} totalSeconds={1800} size={200} stroke={10} />
            </div>
            <div className="ticket__foot">
              <span>Pay within the window to keep your seat</span>
            </div>
          </div>
        </div>
      </main>
    </div>
  );
}
