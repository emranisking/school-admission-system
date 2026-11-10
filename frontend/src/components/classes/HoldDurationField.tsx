import { FormField } from '../common/FormField';
import type { HoldUnit } from '../../utils/validation';

interface Props {
  id: string;
  value: string;
  unit: HoldUnit;
  error?: string | null;
  onValue: (v: string) => void;
  onUnit: (u: HoldUnit) => void;
}

export function HoldDurationField({ id, value, unit, error, onValue, onUnit }: Props) {
  return (
    <FormField id={id} label="Payment window" error={error} hint="How long a student has to pay after reserving a seat.">
      <div className="field__row">
        <input
          id={id}
          className="input"
          inputMode="numeric"
          type="number"
          min={1}
          value={value}
          onChange={(e) => onValue(e.target.value)}
          aria-describedby={`${id}-msg`}
          aria-invalid={!!error}
        />
        <select className="select" aria-label="Payment window unit" value={unit} onChange={(e) => onUnit(e.target.value as HoldUnit)}>
          <option value="minutes">Minutes</option>
          <option value="seconds">Seconds</option>
        </select>
      </div>
    </FormField>
  );
}
