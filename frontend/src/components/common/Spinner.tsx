import type { CSSProperties } from 'react';

export function Spinner({ size = 20, label }: { size?: number; label?: string }) {
  return (
    <span
      className="spinner"
      style={{ width: size, height: size }}
      role={label ? 'status' : undefined}
      aria-label={label}
      aria-hidden={label ? undefined : true}
    />
  );
}

export function Skeleton({
  w,
  h = 14,
  radius,
  className = '',
  style,
}: {
  w?: number | string;
  h?: number | string;
  radius?: number | string;
  className?: string;
  style?: CSSProperties;
}) {
  return (
    <span
      className={`skeleton ${className}`}
      style={{ width: w ?? '100%', height: h, borderRadius: radius, ...style }}
      aria-hidden="true"
    />
  );
}

export function PageLoader({ label = 'Loading' }: { label?: string }) {
  return (
    <div className="page-loader">
      <Spinner size={28} label={label} />
    </div>
  );
}
