import type { HoldDto } from '../../types/admission';
import { formatDateTime } from '../../utils/date';
import { StatusBadge } from '../common/Badge';

function outcome(h: HoldDto): string {
  if (h.status === 'CONFIRMED') return `Paid ${formatDateTime(h.confirmedAt)}`;
  if (h.status === 'CANCELLED') return `Cancelled ${formatDateTime(h.cancelledAt)}`;
  if (h.status === 'EXPIRED') return `Expired ${formatDateTime(h.expiresAt)}`;
  return `Pay by ${formatDateTime(h.expiresAt)}`;
}

export function ApplicationsTable({ items }: { items: HoldDto[] }) {
  return (
    <div className="dtable-wrap">
      <table className="dtable">
        <thead>
          <tr>
            <th>Student ID</th>
            <th>Class</th>
            <th>Status</th>
            <th>Reserved</th>
            <th>Outcome</th>
          </tr>
        </thead>
        <tbody>
          {items.map((h, i) => (
            <tr key={h.holdId} style={{ ['--i' as string]: i }}>
              <td data-label="Student ID" className="mono">{h.studentId}</td>
              <td data-label="Class">{h.className}</td>
              <td data-label="Status"><StatusBadge status={h.status} /></td>
              <td data-label="Reserved">{formatDateTime(h.createdAt)}</td>
              <td data-label="Outcome">{outcome(h)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
