import { FileText, LayoutDashboard, LogOut, Menu, School, X } from 'lucide-react';
import { useEffect, useState } from 'react';
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../app/providers';
import { Brand } from '../components/common/Brand';

const NAV = [
  { to: '/admin', label: 'Dashboard', icon: LayoutDashboard, end: true },
  { to: '/admin/classes', label: 'Classes', icon: School, end: false },
  { to: '/admin/applications', label: 'Applications', icon: FileText, end: false },
];

export function AdminLayout() {
  const { adminName, signOutAdmin } = useAuth();
  const navigate = useNavigate();
  const loc = useLocation();
  const [open, setOpen] = useState(false);

  useEffect(() => setOpen(false), [loc.pathname]);
  useEffect(() => {
    if (!open) return;
    const onKey = (e: KeyboardEvent) => e.key === 'Escape' && setOpen(false);
    document.addEventListener('keydown', onKey);
    return () => document.removeEventListener('keydown', onKey);
  }, [open]);

  const signOut = () => {
    signOutAdmin();
    navigate('/admin/login', { replace: true });
  };

  return (
    <div className="admin">
      <a className="skip" href="#main">Skip to content</a>
      <div className={`scrim${open ? ' is-open' : ''}`} onClick={() => setOpen(false)} aria-hidden="true" />
      <aside id="admin-nav" className={`sidebar${open ? ' is-open' : ''}`} aria-label="Admin navigation">
        <div className="sidebar__head">
          <Brand suffix="Admin" />
          <button className="icon-btn icon-btn--dark sidebar__close" onClick={() => setOpen(false)} aria-label="Close menu">
            <X size={20} />
          </button>
        </div>
        <nav className="sidebar__nav">
          {NAV.map(({ to, label, icon: Icon, end }) => (
            <NavLink key={to} to={to} end={end}>
              <Icon size={18} aria-hidden /> {label}
            </NavLink>
          ))}
        </nav>
        <p className="sidebar__foot">Times shown in your local time zone.</p>
      </aside>

      <div className="admin__main">
        <header className="admintop">
          <button className="icon-btn admintop__menu" onClick={() => setOpen(true)} aria-label="Open menu" aria-expanded={open} aria-controls="admin-nav">
            <Menu size={22} />
          </button>
          <span className="admintop__spacer" />
          <span className="admintop__user">
            <span className="avatar" aria-hidden="true">{(adminName ?? 'A').charAt(0).toUpperCase()}</span>
            <span className="admintop__name">{adminName ?? 'Admin'}</span>
          </span>
          <button className="linkbtn" onClick={signOut}>
            <LogOut size={16} aria-hidden /> Sign out
          </button>
        </header>
        <main id="main" className="admin__content page">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
