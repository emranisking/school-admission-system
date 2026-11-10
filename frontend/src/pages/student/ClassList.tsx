import { RefreshCw } from 'lucide-react';
import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { ClassCard, ClassCardSkeleton } from '../../components/classes/ClassCard';
import { Button } from '../../components/common/Button';
import { EmptyState } from '../../components/common/EmptyState';
import { useToast } from '../../components/common/Toast';
import { useApplications } from '../../hooks/useApplications';
import { useClassAvailability } from '../../hooks/useClassAvailability';
import { createHold } from '../../services/admissionService';
import type { ClassDto } from '../../types/class';
import { errorCode, errorMessage } from '../../utils/errors';

export default function StudentClassList() {
  const navigate = useNavigate();
  const toast = useToast();
  const { classes, loading, error, refresh } = useClassAvailability(8000);
  const { items: holds, refresh: refreshHolds } = useApplications(null);
  const [applying, setApplying] = useState<string | null>(null);

  const activeByClass = useMemo(() => {
    const m = new Map<string, string>();
    for (const h of holds) {
      if ((h.status === 'HELD' || h.status === 'CONFIRMED') && !m.has(h.classId)) m.set(h.classId, h.holdId);
    }
    return m;
  }, [holds]);

  const apply = async (cls: ClassDto) => {
    setApplying(cls.id);
    try {
      const hold = await createHold(cls.id);
      toast.success('Seat reserved', 'Pay the admission fee before the timer ends.');
      navigate(`/applications/${hold.holdId}`);
    } catch (e) {
      const code = errorCode(e);
      const titles: Record<string, string> = {
        SEAT_UNAVAILABLE: 'Seat no longer available',
        DUPLICATE_APPLICATION: 'Already applied',
        ADMISSION_CLOSED: 'Admission closed',
      };
      toast.error(titles[code] ?? 'Could not reserve a seat', errorMessage(e));
      if (code === 'SEAT_UNAVAILABLE' || code === 'ADMISSION_CLOSED') void refresh();
      if (code === 'DUPLICATE_APPLICATION') void refreshHolds();
    } finally {
      setApplying(null);
    }
  };

  return (
    <>
      <div className="pagehead">
        <div>
          <h1>Choose a class</h1>
          <p className="muted">Seat counts update every few seconds. Applying holds a seat while you pay.</p>
        </div>
      </div>

      {loading ? (
        <div className="grid">
          {[0, 1, 2].map((i) => <ClassCardSkeleton key={i} />)}
        </div>
      ) : error ? (
        <EmptyState
          title="Classes did not load"
          message={errorMessage(error)}
          action={<Button icon={<RefreshCw size={16} />} onClick={() => void refresh()}>Try again</Button>}
        />
      ) : classes.length === 0 ? (
        <EmptyState title="No classes are listed yet" message="Check back soon. Classes appear here when the school opens admission." />
      ) : (
        <div className="grid">
          {classes.map((c, i) => (
            <ClassCard key={c.id} cls={c} index={i} activeHoldId={activeByClass.get(c.id)} applying={applying === c.id} onApply={apply} />
          ))}
        </div>
      )}
    </>
  );
}
