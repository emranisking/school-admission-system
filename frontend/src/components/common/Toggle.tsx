interface Props {
  checked: boolean;
  onChange: (next: boolean) => void;
  label: string;
  disabled?: boolean;
  showLabel?: boolean;
  onText?: string;
  offText?: string;
}

export function Toggle({ checked, onChange, label, disabled, showLabel = true, onText = 'On', offText = 'Off' }: Props) {
  return (
    <button
      type="button"
      role="switch"
      aria-checked={checked}
      aria-label={label}
      disabled={disabled}
      className={`toggle${checked ? ' is-on' : ''}`}
      onClick={() => onChange(!checked)}
    >
      <span className="toggle__track">
        <span className="toggle__thumb" />
      </span>
      {showLabel && <span className="toggle__text">{checked ? onText : offText}</span>}
    </button>
  );
}
