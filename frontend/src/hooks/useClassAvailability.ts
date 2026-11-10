import { useCallback, useEffect, useRef, useState } from 'react';
import { listClasses } from '../services/classService';
import type { ClassDto } from '../types/class';
import { useVisibleInterval } from './useVisibleInterval';

export function useClassAvailability(intervalMs = 8000) {
  const [classes, setClasses] = useState<ClassDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<unknown>(null);
  const hasData = useRef(false);
  const alive = useRef(true);

  const refresh = useCallback(async () => {
    try {
      const data = await listClasses();
      if (!alive.current) return;
      hasData.current = true;
      setClasses(data);
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

  useVisibleInterval(() => void refresh(), intervalMs);

  return { classes, loading, error, refresh };
}
