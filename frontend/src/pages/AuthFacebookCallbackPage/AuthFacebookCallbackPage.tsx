import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../auth/useAuth';
import styles from './AuthFacebookCallbackPage.module.css';

export default function AuthFacebookCallbackPage() {
  const navigate = useNavigate();
  const { completeFacebookLogin } = useAuth();
  const [error, setError] = useState<string | null>(null);
  const code = new URLSearchParams(window.location.search).get('code');

  useEffect(() => {
    if (!code) {
      return;
    }

    void completeFacebookLogin(code)
      .then(() => {
        navigate('/account', { replace: true });
      })
      .catch((callbackError) => {
        setError(callbackError instanceof Error ? callbackError.message : 'Dang nhap Facebook that bai.');
      });
  }, [code, completeFacebookLogin, navigate]);

  if (!code) {
    return (
      <section className={styles.page}>
        <div className={styles.panel}>
          <h1 className={styles.title}>Dang hoan tat dang nhap Facebook</h1>
          <p className={styles.message}>Khong tim thay ma xac thuc tu Facebook.</p>
        </div>
      </section>
    );
  }

  return (
    <section className={styles.page}>
      <div className={styles.panel}>
        <h1 className={styles.title}>Dang hoan tat dang nhap Facebook</h1>
        <p className={styles.message}>
          He thong dang xac minh ma uy quyen va tao phien dang nhap bang cookie HttpOnly.
        </p>
        {error ? <p className={styles.error}>{error}</p> : null}
      </div>
    </section>
  );
}
