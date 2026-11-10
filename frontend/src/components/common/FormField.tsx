import type { ReactNode } from 'react';

interface Props {
  id: string;
  label: string;
  hint?: ReactNode;
  error?: string | null;
  children: ReactNode;
}

/** Callers must give their control the same `id` and `aria-describedby={`${id}-msg`}`. */
export function FormField({ id, label, hint, error, children }: Props) {
  return (
    <div className={`field${error ? ' field--error' : ''}`}>
      <label className="field__label" htmlFor={id}>
        {label}
      </label>
      {children}
      {error ? (
        <p className="field__msg field__msg--error" id={`${id}-msg`} role="alert">
          {error}
        </p>
      ) : hint ? (
        <p className="field__msg" id={`${id}-msg`}>
          {hint}
        </p>
      ) : null}
    </div>
  );
}
