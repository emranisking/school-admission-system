export class ApiError extends Error {
  code: string;
  status: number;
  constructor(code: string, message: string, status = 0) {
    super(message);
    this.name = 'ApiError';
    this.code = code;
    this.status = status;
  }
}

const FRIENDLY: Record<string, string> = {
  INVALID_STUDENT_ID: 'That student ID is not valid. It must be exactly 17 digits.',
  SEAT_UNAVAILABLE: 'Someone just took the last seat. Seat counts are refreshed.',
  ADMISSION_CLOSED: 'Admission for this class is closed right now.',
  DUPLICATE_APPLICATION: 'You already have an active application for this class.',
  RESERVATION_EXPIRED: 'Your reservation time ran out, so the seat was released.',
  INVALID_STATE: 'This application can no longer be changed.',
  CAPACITY_BELOW_COMMITMENTS: 'Total seats cannot be lower than the seats already held or confirmed.',
  NOT_FOUND: 'We could not find what you were looking for.',
  UNAUTHORIZED: 'Your session has ended. Sign in again.',
  INVALID_CREDENTIALS: 'Username or password is incorrect.',
  FORBIDDEN: 'You do not have permission to do that.',
  BUSY: 'The system is busy. Wait a moment and try again.',
  NETWORK: 'Cannot reach the server. Check your connection and try again.',
};

export function errorCode(err: unknown): string {
  return err instanceof ApiError ? err.code : 'UNKNOWN';
}

/** Always returns something safe to show a person. */
export function errorMessage(err: unknown): string {
  if (err instanceof ApiError) {
    if (FRIENDLY[err.code]) return FRIENDLY[err.code];
    if (err.code === 'VALIDATION_ERROR' && err.message) return err.message;
  }
  return 'Something went wrong. Please try again.';
}
