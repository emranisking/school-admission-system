import { BadgeCheck, Clock3, Armchair, Layers, RefreshCw, TimerOff, Users } from 'lucide-react';
import { useCallback, useEffect, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import { ApplicationsTable } from '../../components/admission/ApplicationsTable';
import { Button, buttonClass } from '../../components/common/Button';
import { Card } from '../../components/common/Card';
import { EmptyState } from '../../components/common/EmptyState';
import { Skeleton } from '../../components/common/Spinner';
import { StatTile } from '../../components/common/StatTile';
import { SeatDistribution } from '../../components/dashboard/SeatDistribution';
import { useVisibleInterval } from '../../hooks/useVisibleInterval';
import { getDashboard } from '../../services/admissionService';
import type { DashboardDto } from '../../types/admission';
import { errorMessage } from '../../utils/errors';

export default function Dashboard() {
  const [data, setData] = useState<DashboardDto | null>(null);
  const [error, setError] = useState<unknown>(null);
  const has = useRef(false);
  const alive = useRef(true);

  const load = useCallback(async () => {
    try {
      const d = await getDashboard();
      if (!alive.current) return;
      has.current = true;
      setData(d);
      setError(null);
    } catch (e) {
      if (alive.current && !has.current) setError(e);
    }
  }, []);

  useEffect(() => {
    alive.current = true;
    void load();
    return () => {
      alive.current = false;
    };
  }, [load]);
  useVisibleInterval(() => void load(), 15000);

  return (
    <>
      <div className="pagehead">
        <div>
          <h1>Dashboard</h1>
          <p className="muted">Live overview. Refreshes every 15 seconds.</p>
        </div>
        <Link to="/admin/classes/new" className={buttonClass('primary')}>New class</Link>
      </div>

      {error && !data ? (
        <EmptyState title="Dashboard did not load" message={errorMessage(error)} action={<Button icon={<RefreshCw size={16} />} onClick={() => void load()}>Try again</Button>} />
      ) : !data ? (
        <>
          <div className="stats">{Array.from({ length: 6 }, (_, i) => <Skeleton key={i} h={112} radius={14} />)}</div>
          <Skeleton h={160} radius={14} style={{ marginTop: 16 }} />
        </>
      ) : (
        <>
          <div className="stats">
            <StatTile index={0} tone="ink" label="Total classes" value={data.totalClasses} icon={<Layers size={20} />} />
            <StatTile index={1} tone="accent" label="Total seats" value={data.totalSeats} icon={<Armchair size={20} />} />
            <StatTile index={2} tone="neutral" label="Available seats" value={data.availableSeats} icon={<Users size={20} />} />
            <StatTile index={3} tone="held" label="Held seats" value={data.heldSeats} icon={<Clock3 size={20} />} />
            <StatTile index={4} tone="confirmed" label="Confirmed admissions" value={data.confirmedAdmissions} icon={<BadgeCheck size={20} />} />
            <StatTile index={5} tone="expired" label="Expired reservations" value={data.expiredReservations} icon={<TimerOff size={20} />} />
          </div>

          <div className="section"><SeatDistribution data={data} /></div>

          <div className="section">
            <Card title="Recent applications" flush actions={<Link className="linkbtn" to="/admin/applications">View all</Link>}>
              {data.recentApplications.length === 0 ? (
                <EmptyState title="No applications yet" message="Reservations appear here as soon as students apply." />
              ) : (
                <ApplicationsTable items={data.recentApplications} />
              )}
            </Card>
          </div>
        </>
      )}
    </>
  );
}
