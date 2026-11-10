import { Eye, EyeOff, LogIn } from 'lucide-react';
import { useState, type FormEvent } from 'react';
import { Link, Navigate, useNavigate } from 'react-router-dom';
import { useAuth } from '../../app/providers';
import { Brand } from '../../components/common/Brand';
import { Button } from '../../components/common/Button';
import { FormField } from '../../components/common/FormField';
import { useToast } from '../../components/common/Toast';
import { adminLogin } from '../../services/authService';
import { errorMessage } from '../../utils/errors';

export default function AdminLogin() {
  const { adminToken, syncAdmin } = useAuth();
  const navigate = useNavigate();
  const toast = useToast();
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [show, setShow] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (adminToken) return <Navigate to="/admin" replace />;

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    if (!username.trim() || !password) {
      setError('Enter your username and password.');
      return;
    }
    setBusy(true);
    setError(null);
    try {
      const res = await adminLogin(username.trim(), password);
      syncAdmin();
      toast.success(`Welcome, ${res.username}`);
      navigate('/admin', { replace: true });
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="login">
      <aside className="login__panel">
        <Brand />
        <div className="login__pitch">
          <h2>Every seat, accounted for.</h2>
          <p>Open and close admission, set payment windows, and watch reservations turn into confirmed students.</p>
          <ul className="login__bars" aria-hidden="true">
            <li><span style={{ width: '62%' }} /></li>
            <li><span style={{ width: '38%' }} /></li>
            <li><span style={{ width: '81%' }} /></li>
          </ul>
        </div>
        <Link to="/" className="login__back">Back to student admission</Link>
      </aside>

      <main className="login__form">
        <form onSubmit={submit} noValidate className="login__card">
          <h1>Admin sign in</h1>
          <p className="muted">Use the account issued by the school office.</p>
          <FormField id="username" label="Username">
            <input id="username" className="input" autoComplete="username" autoFocus value={username} onChange={(e) => setUsername(e.target.value)} />
          </FormField>
          <FormField id="password" label="Password" error={error}>
            <div className="input-wrap">
              <input
                id="password"
                className="input"
                type={show ? 'text' : 'password'}
                autoComplete="current-password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                aria-describedby="password-msg"
                aria-invalid={!!error}
              />
              <button type="button" className="input-wrap__btn" onClick={() => setShow((s) => !s)} aria-label={show ? 'Hide password' : 'Show password'}>
                {show ? <EyeOff size={18} /> : <Eye size={18} />}
              </button>
            </div>
          </FormField>
          <Button type="submit" size="lg" block loading={busy} icon={<LogIn size={18} />}>Sign in</Button>
        </form>
      </main>
    </div>
  );
}
