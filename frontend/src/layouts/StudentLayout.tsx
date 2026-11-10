import { ArrowLeftRight, ListChecks, School } from 'lucide-react';
import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '../app/providers';
import { Brand } from '../components/common/Brand';
import { maskStudentId } from '../utils/validation';

export function StudentLayout() {
  const { studentId, signOutStudent } = useAuth();
  const navigate = useNavigate();
  const switchStudent = () => {
    signOutStudent();
    navigate('/');
  };
  return (
    <div className="shell">
      <a className="skip" href="#main">Skip to content</a>
      <header className="topbar">
        <div className="topbar__inner">
          <NavLink to="/classes" className="topbar__brand" aria-label="Seatwise home"><Brand /></NavLink>
          <nav className="topbar__nav" aria-label="Main">
            <NavLink to="/classes"><School size={17} aria-hidden /><span>Classes</span></NavLink>
            <NavLink to="/applications"><ListChecks size={17} aria-hidden /><span>My applications</span></NavLink>
          </nav>
          <div className="topbar__id">
            <span className="idpill" title="Birth registration number">
              <span className="sr-only">Signed in as </span>
              {studentId ? maskStudentId(studentId) : ''}
            </span>
            <button className="linkbtn" onClick={switchStudent}>
              <ArrowLeftRight size={15} aria-hidden /> Switch
            </button>
          </div>
        </div>
      </header>
      <main id="main" className="container page">
        <Outlet />
      </main>
    </div>
  );
}
