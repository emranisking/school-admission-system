export function BrandMark({ size = 30 }: { size?: number }) {
  return (
    <svg width={size} height={size} viewBox="0 0 32 32" aria-hidden="true" className="brand__mark">
      <rect width="32" height="32" rx="9" fill="currentColor" className="brand__tile" />
      <circle cx="16" cy="16" r="8.5" fill="none" stroke="var(--accent)" strokeWidth="3.5" strokeDasharray="40 14" strokeLinecap="round" transform="rotate(-90 16 16)" />
      <circle cx="16" cy="16" r="2.4" fill="#fff" />
    </svg>
  );
}

export function Brand({ suffix }: { suffix?: string }) {
  return (
    <span className="brand">
      <BrandMark />
      <span className="brand__name">Seatwise</span>
      {suffix && <span className="brand__suffix">{suffix}</span>}
    </span>
  );
}
