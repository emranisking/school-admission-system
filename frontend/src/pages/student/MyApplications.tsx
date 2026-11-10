import { RefreshCw } from 'lucide-react';
import { Link } from 'react-router-dom';
import { ApplicationCard } from '../../components/admission/ApplicationCard';
import { Button, buttonClass } from '../../components/common/Button';
import { EmptyState } from '../../components/common/EmptyState';
import { Skeleton } from '../../components/common/Spinner';
import { useApplications } from '../../hooks/useApplications';
import { errorMessage } from '../../utils/errors';

export default function MyApplications() {
  const { items, offset, loading, error, refresh } = useApplications(15000);
  return (
    <>
      <div className="pagehead">
        <div>
          <h1>My applications</h1>
          <p className="muted">Newest first. Open one to pay or review it.</p>
        </div>
        <Link to="/classes" className={buttonClass('secondary')}>Browse classes</Link>
      </div>

      {loading ? (
        <div className="stack">
          {[0, 1, 2].map((i) => (
            <div key={i} className="appcard appcard--skeleton">
              <Skeleton w="40%" h={20} />
              <Skeleton w="25%" h={12} style={{ marginTop: 10 }} />
              <Skeleton w="70%" h={14} style={{ marginTop: 18 }} />
            </div>
          ))}
        </div>
      ) : error ? (
        <EmptyState title="Applications did not load" message={errorMessage(error)} action={<Button icon={<RefreshCw size={16} />} onClick={() => void refresh()}>Try again</Button>} />
      ) : items.length === 0 ? (
        <EmptyState
          title="No applications yet"
          message="When you reserve a seat, it shows up here with its payment countdown."
          action={<Link to="/classes" className={buttonClass('primary')}>Browse classes</Link>}
        />
      ) : (
        <div className="stack">
          {items.map((h, i) => <ApplicationCard key={h.holdId} hold={h} offset={offset} index={i} />)}
        </div>
      )}
    </>
  );
}
