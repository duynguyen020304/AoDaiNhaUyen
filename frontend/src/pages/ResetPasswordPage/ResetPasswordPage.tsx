import { useEffect, useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { resetPassword } from '../../api/auth';
import styles from './ResetPasswordPage.module.css';

export default function ResetPasswordPage() {
  const navigate = useNavigate();
  const params = useMemo(() => new URLSearchParams(window.location.search), []);
  const rawUserId = params.get('id');
  const token = params.get('token');
  const userId = rawUserId ? Number(rawUserId) : Number.NaN;

  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    if (token || rawUserId) {
      window.history.replaceState({}, document.title, window.location.pathname);
    }
  }, [rawUserId, token]);

  const invalidLink = !token || !Number.isFinite(userId) || userId <= 0;

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (invalidLink) {
      setError('Lien ket dat lai mat khau khong hop le.');
      return;
    }

    if (password.length < 8) {
      setError('Mat khau moi phai co it nhat 8 ky tu.');
      return;
    }

    if (password !== confirmPassword) {
      setError('Mat khau xac nhan khong khop.');
      return;
    }

    setSubmitting(true);
    setError(null);
    setSuccess(null);

    try {
      await resetPassword({ userId, token, newPassword: password });
      setSuccess('Mat khau da duoc cap nhat. He thong se chuyen ban ve trang dang nhap.');
      window.setTimeout(() => navigate('/login', { replace: true }), 1400);
    } catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : 'Khong the dat lai mat khau.');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <section className={styles.page}>
      <div className={styles.panel}>
        <h1 className={styles.title}>Dat lai mat khau</h1>
        <p className={styles.description}>
          Nhap mat khau moi de khoi phuc truy cap vao tai khoan cua ban.
        </p>

        {invalidLink ? (
          <>
            <p className={styles.warning}>Lien ket dat lai mat khau da khong con hop le hoac da bi thieu thong tin.</p>
            <Link className={styles.link} to="/login">Quay lai dang nhap</Link>
          </>
        ) : (
          <form className={styles.form} onSubmit={handleSubmit}>
            <div className={styles.field}>
              <label htmlFor="new-password">Mat khau moi</label>
              <input
                id="new-password"
                type="password"
                autoComplete="new-password"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                required
              />
            </div>

            <div className={styles.field}>
              <label htmlFor="confirm-password">Xac nhan mat khau</label>
              <input
                id="confirm-password"
                type="password"
                autoComplete="new-password"
                value={confirmPassword}
                onChange={(event) => setConfirmPassword(event.target.value)}
                required
              />
            </div>

            <button className={styles.submit} type="submit" disabled={submitting}>
              {submitting ? 'Dang cap nhat...' : 'Cap nhat mat khau'}
            </button>
          </form>
        )}

        {error ? <p className={styles.error}>{error}</p> : null}
        {success ? <p className={styles.success}>{success}</p> : null}
      </div>
    </section>
  );
}
