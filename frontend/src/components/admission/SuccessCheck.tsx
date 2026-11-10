export function SuccessCheck({ size = 112 }: { size?: number }) {
  return (
    <svg className="success-check" width={size} height={size} viewBox="0 0 112 112" aria-hidden="true">
      <circle className="success-check__disc" cx="56" cy="56" r="50" />
      <circle className="success-check__ring" cx="56" cy="56" r="50" fill="none" strokeWidth="4" />
      <path className="success-check__tick" d="M34 58l15 15 30-33" fill="none" strokeWidth="7" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}
