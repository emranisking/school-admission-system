import { RefreshCw, Search } from 'lucide-react';
import { useCallback, useEffect, useRef, useState } from 'react';
import { ApplicationsTable } from '../../components/admission/ApplicationsTable';
import { Button } from '../../components/common/Button';
import { Card } from '../../components/common/Card';
import { EmptyState } from '../../components/common/EmptyState';
import { Pagination } from '../../components/common/Pagination';
import { Skeleton } from '../../components/common/Spinner';
import { adminListClasses } from '../../services/classService';
import { listApplications } from '../../services/admissionService';
import { HOLD_STATUSES, type HoldDto, type PagedResult } from '../../types/admission';
import type { ClassDto } from '../../types/class';
import { errorMessage } from '../../utils/errors';

export default function Applications() {
  const [classes, setClasses] = useState<ClassDto[]>([]);
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [classId, setClassId] = useState('');
  const [status, setStatus] = useState('ALL');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [result, setResult] = useState<PagedResult<HoldDto> | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<unknown>(null);
  const reqId = useRef(0);

  useEffect(() => {
    adminListClasses().then(setClasses).catch(() => undefined);
  }, []);

  useEffect(() => {
    const t = window.setTimeout(() => {
      setSearch(searchInput);
      setPage(1);
    }, 350);
    return () => window.clearTimeout(t);
  }, [searchInput]);

  const load = useCallback(async () => {
    const id = ++reqId.current;
    setLoading(true);
    try {
      const r = await listApplications({ search, status: status as never, classId, page, pageSize });
      if (id !== reqId.current) return;
      setResult(r);
      setError(null);
    } catch (e) {
      if (id !== reqId.current) return;
      setError(e);
    } finally {
      if (id === reqId.current) setLoading(false);
    }
  }, [search, status, classId, page, pageSize]);

  useEffect(() => {
    void load();
  }, [load]);

  return (
    <>
      <div className="pagehead"><div><h1>Applications</h1><p className="muted">Every reservation, payment and cancellation.</p></div></div>

      <div className="filters">
        <label className="filters__search">
          <Search size={17} aria-hidden />
          <input className="input" type="search" placeholder="Search by student ID" aria-label="Search by student ID" value={searchInput} onChange={(e) => setSearchInput(e.target.value)} />
        </label>
        <select className="select" aria-label="Filter by class" value={classId} onChange={(e) => { setClassId(e.target.value); setPage(1); }}>
          <option value="">All classes</option>
          {classes.map((c) => <option key={c.id} value={c.id}>{c.displayName} ({String(c.academicYear)})</option>)}
        </select>
        <select className="select" aria-label="Filter by status" value={status} onChange={(e) => { setStatus(e.target.value); setPage(1); }}>
          <option value="ALL">All statuses</option>
          {HOLD_STATUSES.map((s) => <option key={s} value={s}>{s.charAt(0) + s.slice(1).toLowerCase()}</option>)}
        </select>
      </div>

      <Card flush className={loading && result ? 'is-refreshing' : ''}>
        {error && !result ? (
          <EmptyState title="Applications did not load" message={errorMessage(error)} action={<Button icon={<RefreshCw size={16} />} onClick={() => void load()}>Try again</Button>} />
        ) : !result ? (
          <div className="stack pad">{[0, 1, 2, 3].map((i) => <Skeleton key={i} h={44} />)}</div>
        ) : result.items.length === 0 ? (
          <EmptyState title="No applications match" message="Try a different student ID, class or status." />
        ) : (
          <ApplicationsTable items={result.items} />
        )}
        {result && result.total > 0 && (
          <Pagination page={page} pageSize={pageSize} total={result.total} onPage={setPage} onPageSize={(s) => { setPageSize(s); setPage(1); }} />
        )}
      </Card>
    </>
  );
}
