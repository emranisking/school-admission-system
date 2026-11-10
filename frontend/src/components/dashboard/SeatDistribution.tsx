import { SeatBar } from '../common/SeatBar';
import { Card } from '../common/Card';
import type { DashboardDto } from '../../types/admission';

export function SeatDistribution({ data }: { data: DashboardDto }) {
  const pct = (n: number) => (data.totalSeats ? Math.round((n / data.totalSeats) * 100) : 0);
  return (
    <Card title="Seat distribution" description={`Across ${data.totalClasses} classes, ${data.totalSeats.toLocaleString('en-US')} seats in total.`}>
      <SeatBar size="lg" total={data.totalSeats} confirmed={data.confirmedAdmissions} held={data.heldSeats} available={data.availableSeats} />
      <ul className="distro">
        <li><i className="dot dot--confirmed" /><strong>{pct(data.confirmedAdmissions)}%</strong> confirmed</li>
        <li><i className="dot dot--held" /><strong>{pct(data.heldSeats)}%</strong> held</li>
        <li><i className="dot dot--available" /><strong>{pct(data.availableSeats)}%</strong> available</li>
      </ul>
    </Card>
  );
}
