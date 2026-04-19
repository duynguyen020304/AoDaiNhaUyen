import { useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../auth/useAuth';
import styles from './AuthCallbackPage.module.css';

type Provider = 'google' | 'zalo';

type CallbackState = 'idle' | 'completing' | 'error';

const oauthExchangeCache = new Map<string, Promise<unknown>>();

function getCacheKey(provider: Provider, code: string) {
  return `${provider}:${code}`;
}

function getProviderLabel(provider: Provider) {
  return provider === 'google' ? 'Google' : 'Zalo';
}

function getFailureMessage(provider: Provider) {
  return provider === 'google'
    ? 'Khong the xac minh dang nhap Google. Vui long thu lai.'
    : 'Khong the xac minh dang nhap Zalo. Vui long thu lai.';
}

export interface AuthCallbackPageProps {
  provider: Provider;
}

export default function AuthCallbackPage({ provider }: AuthCallbackPageProps) {
  const navigate = useNavigate();
  const { completeGoogleLogin, completeZaloLogin } = useAuth();
  const query = useMemo(() => new URLSearchParams(window.location.search), []);
  const code = useMemo(() => query.get('code'), [query]);
  const statusParam = useMemo(() => query.get('status'), [query]);
  const reasonParam = useMemo(() => query.get('reason'), [query]);
  const isZaloBackendSuccess = provider === 'zalo' && statusParam === 'success';
  const exchangeStarted = useRef(false);
  const [state, setState] = useState<CallbackState>(() => ((code || isZaloBackendSuccess) ? 'completing' : 'error'));
  const [error, setError] = useState<string | null>(() => {
    if (code || isZaloBackendSuccess) {
      return null;
    }

    if (provider === 'zalo' && statusParam === 'error') {
      return `Dang nhap Zalo that bai: ${reasonParam || 'zalo_login_failed'}.`;
    }

    return `Khong tim thay ma xac thuc tu ${getProviderLabel(provider)}.`;
  });

  useEffect(() => {
    if (provider === 'zalo' && statusParam === 'error') {
      return;
    }

    if (provider === 'zalo' && statusParam === 'success') {
      if (exchangeStarted.current) {
        return;
      }

      const cacheKey = 'zalo:backend-callback';
      const exchange = oauthExchangeCache.get(cacheKey) ?? completeZaloLogin();
      if (!oauthExchangeCache.has(cacheKey)) {
        oauthExchangeCache.set(cacheKey, Promise.resolve(exchange));
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

      return;
    }

    if (!code) {
      return;
    }

    if (exchangeStarted.current) {
      return;
    }

    const cacheKey = getCacheKey(provider, code);
    const exchange =
      oauthExchangeCache.get(cacheKey) ??
      (provider === 'google'
        ? completeGoogleLogin(code)
        : Promise.reject(new Error('Dang nhap Zalo phai duoc xu ly qua backend callback.')));

    if (!oauthExchangeCache.has(cacheKey)) {
      oauthExchangeCache.set(cacheKey, Promise.resolve(exchange));
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
  }, [code, completeGoogleLogin, completeZaloLogin, navigate, provider, reasonParam, statusParam]);

  return (
    <section className={styles.page}>
      <div className={styles.panel}>
        <div className={styles.content}>
          <p className={styles.eyebrow}>
            <span className={styles.eyebrowDot} />
            {getProviderLabel(provider)} sign-in
          </p>
          <h1 className={styles.title}>
            {state === 'error'
              ? `Ma xac thuc ${getProviderLabel(provider)} khong hop le`
              : `Dang hoan tat dang nhap ${getProviderLabel(provider)}`}
          </h1>
          <p className={styles.message}>
            {state === 'error'
              ? `Khong the hoan tat dang nhap ${getProviderLabel(provider)}. Hay quay lai trang dang nhap va thu lai.`
              : 'He thong dang xac minh ma uy quyen va tao phien dang nhap bang cookie HttpOnly. Ban se duoc chuyen toi trang tai khoan ngay lap tuc.'}
          </p>

          {state === 'completing' ? (
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
