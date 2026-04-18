import { useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../auth/useAuth';
import styles from './AuthCallbackPage.module.css';

type Provider = 'google' | 'facebook';

type CallbackState = 'idle' | 'completing' | 'error';

const oauthExchangeCache = new Map<string, Promise<unknown>>();

function getCacheKey(provider: Provider, code: string) {
  return `${provider}:${code}`;
}

function getProviderLabel(provider: Provider) {
  return provider === 'google' ? 'Google' : 'Facebook';
}

function getFailureMessage(provider: Provider) {
  return provider === 'google'
    ? 'Khong the xac minh dang nhap Google. Vui long thu lai.'
    : 'Khong the xac minh dang nhap Facebook. Vui long thu lai.';
}

export interface AuthCallbackPageProps {
  provider: Provider;
}

export default function AuthCallbackPage({ provider }: AuthCallbackPageProps) {
  const navigate = useNavigate();
  const { completeGoogleLogin, completeFacebookLogin } = useAuth();
  const code = useMemo(() => new URLSearchParams(window.location.search).get('code'), []);
  const exchangeStarted = useRef(false);
  const [state, setState] = useState<CallbackState>(() => (code ? 'completing' : 'error'));
  const [error, setError] = useState<string | null>(() => (
    code ? null : `Khong tim thay ma xac thuc tu ${getProviderLabel(provider)}.`
  ));

  useEffect(() => {
    if (!code) {
      return;
    }

    const cacheKey = getCacheKey(provider, code);
    const exchange =
      oauthExchangeCache.get(cacheKey) ??
      (provider === 'google'
        ? completeGoogleLogin(code)
        : completeFacebookLogin(code));

    if (!oauthExchangeCache.has(cacheKey)) {
      oauthExchangeCache.set(cacheKey, Promise.resolve(exchange));
    }

    if (exchangeStarted.current) {
      return;
    }

    exchangeStarted.current = true;

    void Promise.resolve(exchange)
      .then(() => {
        oauthExchangeCache.delete(cacheKey);
        navigate('/account', { replace: true });
      })
      .catch((callbackError) => {
        oauthExchangeCache.delete(cacheKey);
        setState('error');
        setError(callbackError instanceof Error ? callbackError.message : getFailureMessage(provider));
      });
  }, [code, completeFacebookLogin, completeGoogleLogin, navigate, provider]);

  return (
    <section className={styles.page}>
      <div className={styles.panel}>
        <div className={styles.content}>
          <p className={styles.eyebrow}>
            <span className={styles.eyebrowDot} />
            {getProviderLabel(provider)} sign-in
          </p>
          <h1 className={styles.title}>
            {code ? `Dang hoan tat dang nhap ${getProviderLabel(provider)}` : `Ma xac thuc ${getProviderLabel(provider)} khong hop le`}
          </h1>
          <p className={styles.message}>
            {code
              ? 'He thong dang xac minh ma uy quyen va tao phien dang nhap bang cookie HttpOnly. Ban se duoc chuyen toi trang tai khoan ngay lap tuc.'
              : `Khong tim thay ma xac thuc tu ${getProviderLabel(provider)}. Hay quay lai trang dang nhap va thu lai.`}
          </p>

          {state === 'completing' && code ? (
            <div className={styles.statusRow} aria-live="polite">
              <div className={styles.spinner} />
              <div>
                <p className={styles.statusCopy}>Dang dong bo phien dang nhap...</p>
                <p className={styles.statusSubcopy}>Vui long giu nguyen trang trong giay lat.</p>
              </div>
            </div>
          ) : null}

          {state === 'error' && error ? (
            <div className={styles.error}>
              {error}
              <div className={styles.hint}>
                Neu ban vua cho phep dang nhap, hay quay lai trang login va thu lai. Neu loi van lap lai, lam moi trang co the giup cap nhat cookie phien.
              </div>
            </div>
          ) : null}
        </div>
      </div>
    </section>
  );
}
