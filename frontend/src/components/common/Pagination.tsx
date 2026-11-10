import { ChevronLeft, ChevronRight } from 'lucide-react';

interface Props {
  page: number;
  pageSize: number;
  total: number;
  onPage: (p: number) => void;
  onPageSize: (s: number) => void;
}

export function Pagination({ page, pageSize, total, onPage, onPageSize }: Props) {
  const pages = Math.max(1, Math.ceil(total / pageSize));
  const from = total === 0 ? 0 : (page - 1) * pageSize + 1;
  const to = Math.min(total, page * pageSize);
  return (
    <div className="pager">
      <span className="pager__info" aria-live="polite">
        {from}-{to} of {total.toLocaleString('en-US')}
      </span>
      <label className="pager__size">
        <span>Rows</span>
        <select className="select select--sm" value={pageSize} onChange={(e) => onPageSize(Number(e.target.value))}>
          {[10, 20, 50].map((s) => (
            <option key={s} value={s}>{s}</option>
          ))}
        </select>
      </label>
      <div className="pager__nav">
        <button className="icon-btn" onClick={() => onPage(page - 1)} disabled={page <= 1} aria-label="Previous page">
          <ChevronLeft size={18} />
        </button>
        <span className="pager__page">{page} / {pages}</span>
        <button className="icon-btn" onClick={() => onPage(page + 1)} disabled={page >= pages} aria-label="Next page">
          <ChevronRight size={18} />
        </button>
      </div>
    </div>
  );
}
