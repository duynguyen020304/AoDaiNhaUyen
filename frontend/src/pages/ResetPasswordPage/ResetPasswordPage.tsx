import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuthModal } from '../../auth/AuthModalContext';
import { resetPassword } from '../../api/auth';
import styles from './ResetPasswordPage.module.css';

export default function ResetPasswordPage() {
  const navigate = useNavigate();
  const { openAuthModal } = useAuthModal();
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
      setError('Liên kết đặt lại mật khẩu không hợp lệ.');
      return;
    }

    if (password.length < 8) {
      setError('Mật khẩu mới phải có ít nhất 8 ký tự.');
      return;
    }

    if (password !== confirmPassword) {
      setError('Mật khẩu xác nhận không khớp.');
      return;
    }

    setSubmitting(true);
    setError(null);
    setSuccess(null);

    try {
      await resetPassword({ userId, token, newPassword: password });
      setSuccess('Mật khẩu đã được cập nhật. Hệ thống sẽ mở hộp thoại đăng nhập.');
      window.setTimeout(() => {
        navigate('/', { replace: true });
        openAuthModal();
      }, 1400);
    } catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : 'Không thể đặt lại mật khẩu.');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <section className={styles.page}>
      <div className={styles.panel}>
        <h1 className={styles.title}>Đặt lại mật khẩu</h1>
        <p className={styles.description}>
          Nhập mật khẩu mới để khôi phục truy cập vào tài khoản của bạn.
        </p>

        {invalidLink ? (
          <>
            <p className={styles.warning}>Liên kết đặt lại mật khẩu đã không còn hợp lệ hoặc đã bị thiếu thông tin.</p>
            <button className={styles.link} type="button" onClick={() => openAuthModal()}>
              Quay lại đăng nhập
            </button>
          </>
        ) : (
          <form className={styles.form} onSubmit={handleSubmit}>
            <div className={styles.field}>
              <label htmlFor="new-password">Mật khẩu mới</label>
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
              <label htmlFor="confirm-password">Xác nhận mật khẩu</label>
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
              {submitting ? 'Đang cập nhật...' : 'Cập nhật mật khẩu'}
            </button>
          </form>
        )}

        {error ? <p className={styles.error}>{error}</p> : null}
        {success ? <p className={styles.success}>{success}</p> : null}
      </div>
    </section>
  );
}
