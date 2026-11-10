import { Link, Navigate, Outlet, Route, Routes, useLocation } from 'react-router-dom';
import { buttonClass } from '../components/common/Button';
import { AdminLayout } from '../layouts/AdminLayout';
import { StudentLayout } from '../layouts/StudentLayout';
import AdminLogin from '../pages/admin/AdminLogin';
import Applications from '../pages/admin/Applications';
import AdminClassList from '../pages/admin/ClassList';
import CreateClass from '../pages/admin/CreateClass';
import Dashboard from '../pages/admin/Dashboard';
import EditClass from '../pages/admin/EditClass';
import AdmissionHome from '../pages/student/AdmissionHome';
import ApplicationDetails from '../pages/student/ApplicationDetails';
import StudentClassList from '../pages/student/ClassList';
import MyApplications from '../pages/student/MyApplications';
import { useAuth } from './providers';

function RequireStudent() {
  const { studentId } = useAuth();
  const loc = useLocation();
  if (!studentId) return <Navigate to="/" replace state={{ from: loc.pathname }} />;
  return <Outlet />;
}

function RequireAdmin() {
  const { adminToken } = useAuth();
  if (!adminToken) return <Navigate to="/admin/login" replace />;
  return <Outlet />;
}

function NotFound() {
  return (
    <main className="notfound">
      <p className="notfound__code">404</p>
      <h1>This page does not exist</h1>
      <p>The link may be old, or the address was typed incorrectly.</p>
      <Link className={buttonClass('primary', 'lg')} to="/">Go to the start</Link>
    </main>
  );
}

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/" element={<AdmissionHome />} />
      <Route element={<RequireStudent />}>
        <Route element={<StudentLayout />}>
          <Route path="/classes" element={<StudentClassList />} />
          <Route path="/applications" element={<MyApplications />} />
          <Route path="/applications/:holdId" element={<ApplicationDetails />} />
        </Route>
      </Route>

      <Route path="/admin/login" element={<AdminLogin />} />
      <Route element={<RequireAdmin />}>
        <Route element={<AdminLayout />}>
          <Route path="/admin" element={<Dashboard />} />
          <Route path="/admin/classes" element={<AdminClassList />} />
          <Route path="/admin/classes/new" element={<CreateClass />} />
          <Route path="/admin/classes/:classId/edit" element={<EditClass />} />
          <Route path="/admin/applications" element={<Applications />} />
        </Route>
      </Route>
      <Route path="*" element={<NotFound />} />
    </Routes>
  );
}
