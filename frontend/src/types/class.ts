export type Shift = 'MORNING' | 'DAY' | 'EVENING';

export const SHIFTS: { value: Shift; label: string }[] = [
  { value: 'MORNING', label: 'Morning' },
  { value: 'DAY', label: 'Day' },
  { value: 'EVENING', label: 'Evening' },
];

export function shiftLabel(shift: string): string {
  const s = shift.toLowerCase();
  return s.charAt(0).toUpperCase() + s.slice(1);
}

export interface ClassDto {
  id: string;
  name: string;
  displayName: string;
  academicYear: string | number;
  shift: Shift | string;
  totalSeats: number;
  holdDurationSeconds: number;
  admissionOpen: boolean;
  heldSeats: number;
  confirmedSeats: number;
  availableSeats: number;
  createdAt: string;
  updatedAt: string;
}

export interface CreateClassRequest {
  name: string;
  academicYear: string;
  shift: Shift;
  totalSeats: number;
  holdDurationSeconds?: number;
  admissionOpen?: boolean;
}

export interface UpdateClassDetailsRequest {
  name: string;
  academicYear: string;
  shift: Shift;
}

export interface HoldDurationResponse {
  classId: string;
  holdDurationSeconds: number;
  message: string;
}
