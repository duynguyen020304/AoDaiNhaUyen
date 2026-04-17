import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../auth/useAuth';
import styles from './AuthGoogleCallbackPage.module.css';

export default function AuthGoogleCallbackPage() {
  const navigate = useNavigate();
  const { completeGoogleLogin } = useAuth();
  const [error, setError] = useState<string | null>(null);
  const code = new URLSearchParams(window.location.search).get('code');

  useEffect(() => {
    if (!code) {
      return;
    }

    const redirectUri = `${window.location.origin}/auth/google/callback`;
    void completeGoogleLogin(code, redirectUri)
      .then(() => {
        navigate('/account', { replace: true });
      })
      .catch((callbackError) => {
        setError(callbackError instanceof Error ? callbackError.message : 'Dang nhap Google that bai.');
      });
  }, [code, completeGoogleLogin, navigate]);

  if (!code) {
    return (
      <section className={styles.page}>
        <div className={styles.panel}>
          <h1 className={styles.title}>Dang hoan tat dang nhap Google</h1>
          <p className={styles.message}>Khong tim thay ma xac thuc tu Google.</p>
        </div>
      </section>
    );
  }

  return (
    <section className={styles.page}>
      <div className={styles.panel}>
        <h1 className={styles.title}>Dang hoan tat dang nhap Google</h1>
        <p className={styles.message}>
          He thong dang xac minh ma uy quyen va tao phien dang nhap bang cookie HttpOnly.
        </p>
        {error ? <p className={styles.error}>{error}</p> : null}
      </div>
    </section>
  );
}
