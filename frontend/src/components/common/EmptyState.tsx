import type { ReactNode } from 'react';

export function EmptyIllustration() {
  return (
    <svg className="empty__art" viewBox="0 0 160 120" fill="none" aria-hidden="true">
      <rect x="22" y="30" width="116" height="72" rx="12" fill="var(--accent-soft)" />
      <rect x="22" y="30" width="116" height="72" rx="12" stroke="var(--ink)" strokeWidth="2" />
      <path d="M22 48h116" stroke="var(--ink)" strokeWidth="2" />
      <rect x="36" y="62" width="40" height="6" rx="3" fill="var(--ink)" opacity=".85" />
      <rect x="36" y="76" width="64" height="6" rx="3" fill="var(--ink)" opacity=".25" />
      <circle cx="116" cy="78" r="14" fill="var(--surface)" stroke="var(--accent)" strokeWidth="2.5" />
      <path d="M116 70v8l5 3" stroke="var(--accent)" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round" />
      <path d="M50 20c0-6 6-10 12-10s12 4 12 10" stroke="var(--ink)" strokeWidth="2" strokeLinecap="round" />
    </svg>
  );
}

export function EmptyState({
  title,
  message,
  action,
  art = true,
}: {
  title: string;
  message?: string;
  action?: ReactNode;
  art?: boolean;
}) {
  return (
    <div className="empty">
      {art && <EmptyIllustration />}
      <h3 className="empty__title">{title}</h3>
      {message && <p className="empty__msg">{message}</p>}
      {action && <div className="empty__action">{action}</div>}
    </div>
  );
}
