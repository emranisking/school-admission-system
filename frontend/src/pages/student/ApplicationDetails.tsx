import { ArrowLeft, Ban, CreditCard, TimerOff } from 'lucide-react';
import { useCallback, useEffect, useRef, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { SuccessCheck } from '../../components/admission/SuccessCheck';
import { StatusBadge } from '../../components/common/Badge';
import { Button, buttonClass } from '../../components/common/Button';
import { Card } from '../../components/common/Card';
import { EmptyState } from '../../components/common/EmptyState';
import { ConfirmDialog } from '../../components/common/Modal';
import { PageLoader, Spinner } from '../../components/common/Spinner';
import { useToast } from '../../components/common/Toast';
import { CountdownRing } from '../../components/countdown/CountdownRing';
import { useCountdown } from '../../hooks/useCountdown';
import { useVisibleInterval } from '../../hooks/useVisibleInterval';
import { cancelHold, getHold } from '../../services/admissionService';
import { payForHold } from '../../services/paymentService';
import { serverOffsetFrom, type HoldDto } from '../../types/admission';
import { formatClock, formatDateTime, formatMoney } from '../../utils/date';
import { errorCode, errorMessage } from '../../utils/errors';

export default function ApplicationDetails() {
  const { holdId = '' } = useParams();
  const toast = useToast();
  const [hold, setHold] = useState<HoldDto | null>(null);
  const [offset, setOffset] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<unknown>(null);
  const [paying, setPaying] = useState(false);
  const [confirmCancel, setConfirmCancel] = useState(false);
  const [cancelling, setCancelling] = useState(false);
  const alive = useRef(true);
  const hasHold = useRef(false);

  const apply = useCallback((h: HoldDto) => {
    setOffset(serverOffsetFrom(h)); // captured the moment the payload arrives
    hasHold.current = true;
    setHold(h);
    setError(null);
  }, []);

  const load = useCallback(async () => {
    try {
      const h = await getHold(holdId);
      if (alive.current) apply(h);
    } catch (e) {
      if (alive.current && !hasHold.current) setError(e);
    } finally {
      if (alive.current) setLoading(false);
    }
  }, [holdId, apply]);

  useEffect(() => {
    alive.current = true;
    hasHold.current = false;
    setLoading(true);
    setHold(null);
    void load();
    return () => {
      alive.current = false;
    };
  }, [load]);

  const isHeld = hold?.status === 'HELD';
  const seconds = useCountdown(hold?.expiresAt, offset, isHeld);
  const timeIsUp = isHeld && seconds === 0;
  const total = hold ? Math.max(1, Math.round((Date.parse(hold.expiresAt) - Date.parse(hold.createdAt)) / 1000)) : 1;

  // Server is authoritative: poll while held, and re-fetch right at zero (retrying until the server agrees).
  useVisibleInterval(() => void load(), isHeld && !timeIsUp ? 10000 : null);
  useEffect(() => {
    if (!timeIsUp) return;
    void load();
    const id = window.setInterval(() => void load(), 2000);
    return () => window.clearInterval(id);
  }, [timeIsUp, load]);

  const pay = async () => {
    if (!hold) return;
    setPaying(true);
    try {
      apply(await payForHold(hold.holdId));
      toast.success('Payment received', 'Your admission is confirmed.');
    } catch (e) {
      const code = errorCode(e);
      toast.error(code === 'RESERVATION_EXPIRED' ? 'Reservation expired' : 'Payment did not go through', errorMessage(e));
      if (code === 'RESERVATION_EXPIRED' || code === 'INVALID_STATE' || code === 'NOT_FOUND') await load();
    } finally {
      setPaying(false);
    }
  };

  const cancel = async () => {
    if (!hold) return;
    setCancelling(true);
    try {
      apply(await cancelHold(hold.holdId));
      toast.success('Application cancelled', 'The seat was released.');
      setConfirmCancel(false);
    } catch (e) {
      toast.error('Could not cancel', errorMessage(e));
      setConfirmCancel(false);
      await load();
    } finally {
      setCancelling(false);
    }
  };

  if (loading) return <PageLoader label="Loading application" />;
  if (error || !hold) {
    return (
      <EmptyState
        title="Application not found"
        message={error ? errorMessage(error) : 'It may belong to a different student ID.'}
        action={<Link className={buttonClass('primary')} to="/applications">Back to my applications</Link>}
      />
    );
  }

  const remainingText = isHeld ? formatClock(seconds) : hold.status === 'CONFIRMED' ? 'Not applicable' : 'Ended';

  return (
    <div className="details">
      <Link to="/applications" className="backlink"><ArrowLeft size={16} aria-hidden /> My applications</Link>

      <div className="details__grid">
        <Card className="details__hero" key={hold.status}>
          {isHeld && !timeIsUp && (
            <div className="state state--held">
              <CountdownRing seconds={seconds} totalSeconds={total} />
              <h1 className="state__title">Your seat is temporarily reserved</h1>
              <p className="state__msg">Pay the admission fee before the timer reaches zero. After that the seat is released to other students.</p>
              <div className="state__actions">
                <Button size="lg" block icon={<CreditCard size={18} />} loading={paying} disabled={seconds === 0} onClick={pay}>
                  Pay admission fee
                </Button>
                <Button variant="ghost" block onClick={() => setConfirmCancel(true)} disabled={paying}>
                  Cancel application
                </Button>
              </div>
            </div>
          )}

          {timeIsUp && (
            <div className="state state--held">
              <CountdownRing seconds={0} totalSeconds={total} />
              <h1 className="state__title">Time is up</h1>
              <p className="state__msg"><Spinner size={14} /> Checking with the server...</p>
              <Button size="lg" block icon={<CreditCard size={18} />} disabled>Pay admission fee</Button>
            </div>
          )}

          {hold.status === 'CONFIRMED' && (
            <div className="state state--confirmed">
              <SuccessCheck />
              <h1 className="state__title">Admission confirmed</h1>
              <p className="state__msg">Payment received. The seat in {hold.className} is yours.</p>
              {hold.payment && (
                <dl className="receipt">
                  <div><dt>Payment reference</dt><dd className="mono">{hold.payment.transactionReference}</dd></div>
                  <div><dt>Amount</dt><dd>{formatMoney(hold.payment.amount)}</dd></div>
                  <div><dt>Paid</dt><dd>{formatDateTime(hold.payment.paidAt)}</dd></div>
                </dl>
              )}
            </div>
          )}

          {hold.status === 'EXPIRED' && (
            <div className="state state--expired">
              <span className="state__icon"><TimerOff size={34} aria-hidden /></span>
              <h1 className="state__title">This reservation expired</h1>
              <p className="state__msg">The payment window closed before payment was received, so the seat was released. You can apply again if seats are still open.</p>
              <Link className={buttonClass('primary', 'lg', true)} to="/classes">Browse classes</Link>
            </div>
          )}

          {hold.status === 'CANCELLED' && (
            <div className="state state--cancelled">
              <span className="state__icon"><Ban size={34} aria-hidden /></span>
              <h1 className="state__title">Application cancelled</h1>
              <p className="state__msg">The seat was released{hold.cancelledAt ? ` on ${formatDateTime(hold.cancelledAt)}` : ''}. Nothing was charged.</p>
              <Link className={buttonClass('primary', 'lg', true)} to="/classes">Browse classes</Link>
            </div>
          )}
        </Card>

        <Card title="Application details" className="details__facts">
          <dl className="facts">
            <div><dt>Student ID</dt><dd className="mono">{hold.studentId}</dd></div>
            <div><dt>Class</dt><dd>{hold.className}</dd></div>
            <div><dt>Status</dt><dd><StatusBadge status={hold.status} /></dd></div>
            <div><dt>Reservation started</dt><dd>{formatDateTime(hold.createdAt)}</dd></div>
            <div><dt>Payment deadline</dt><dd>{formatDateTime(hold.expiresAt)}</dd></div>
            <div><dt>Remaining time</dt><dd className={isHeld ? 'mono' : ''}>{remainingText}</dd></div>
          </dl>
        </Card>
      </div>

      <ConfirmDialog
        open={confirmCancel}
        title="Cancel this application?"
        message={<p>Your reserved seat in <strong>{hold.className}</strong> will be released right away. You can apply again later if seats remain.</p>}
        confirmLabel="Cancel application"
        cancelLabel="Keep my seat"
        loading={cancelling}
        onConfirm={cancel}
        onCancel={() => setConfirmCancel(false)}
      />
    </div>
  );
}
