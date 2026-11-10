import { useCallback, useEffect, useRef, useState } from 'react';
import { listMyHolds } from '../services/admissionService';
import { serverOffsetFrom, type HoldDto } from '../types/admission';
import { useVisibleInterval } from './useVisibleInterval';

/** The signed-in student's applications, newest first, with a clock-skew offset. */
export function useApplications(pollMs: number | null = null) {
  const [items, setItems] = useState<HoldDto[]>([]);
  const [offset, setOffset] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<unknown>(null);
  const hasData = useRef(false);
  const alive = useRef(true);

  const refresh = useCallback(async () => {
    try {
      const data = await listMyHolds();
      if (!alive.current) return;
      hasData.current = true;
      setOffset(serverOffsetFrom(data[0]));
      setItems(data);
      setError(null);
    } catch (e) {
      if (!alive.current) return;
      if (!hasData.current) setError(e);
    } finally {
      if (alive.current) setLoading(false);
    }
  }, []);

  useEffect(() => {
    alive.current = true;
    void refresh();
    return () => {
      alive.current = false;
    };
  }, [refresh]);

  useVisibleInterval(() => void refresh(), pollMs);

  return { items, offset, loading, error, refresh };
}
