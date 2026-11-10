import type { CSSProperties, ReactNode } from 'react';

interface Props {
  title?: ReactNode;
  description?: ReactNode;
  actions?: ReactNode;
  children?: ReactNode;
  className?: string;
  style?: CSSProperties;
  flush?: boolean;
}

export function Card({ title, description, actions, children, className = '', style, flush }: Props) {
  return (
    <section className={`card${flush ? ' card--flush' : ''} ${className}`} style={style}>
      {(title || actions) && (
        <header className="card__head">
          <div>
            {title && <h2 className="card__title">{title}</h2>}
            {description && <p className="card__desc">{description}</p>}
          </div>
          {actions && <div className="card__actions">{actions}</div>}
        </header>
      )}
      {children}
    </section>
  );
}
