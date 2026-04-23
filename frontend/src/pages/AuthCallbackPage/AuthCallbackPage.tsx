import { useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { consumeZaloOAuthSession } from '../../api/auth';
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
    ? 'Không thể xác minh đăng nhập Google. Vui lòng thử lại.'
    : 'Không thể xác minh đăng nhập Zalo. Vui lòng thử lại.';
}

export interface AuthCallbackPageProps {
  provider: Provider;
}

export default function AuthCallbackPage({ provider }: AuthCallbackPageProps) {
  const navigate = useNavigate();
  const { completeGoogleLogin, completeZaloLogin } = useAuth();
  const code = useMemo(() => new URLSearchParams(window.location.search).get('code'), []);
  const stateParam = useMemo(() => new URLSearchParams(window.location.search).get('state'), []);
  const exchangeStarted = useRef(false);
  const [state, setState] = useState<CallbackState>(() => (code ? 'completing' : 'error'));
  const [error, setError] = useState<string | null>(() => (
    code ? null : `Không tìm thấy mã xác thực từ ${getProviderLabel(provider)}.`
  ));

  useEffect(() => {
    if (!code) {
      return;
    }

    if (exchangeStarted.current) {
      return;
    }

    let codeVerifier: string | null = null;
    if (provider === 'zalo') {
      codeVerifier = consumeZaloOAuthSession(stateParam);
      if (!codeVerifier) {
        queueMicrotask(() => {
          setState('error');
          setError('Phiên đăng nhập Zalo không hợp lệ hoặc đã hết hạn. Vui lòng thử lại.');
        });
        return;
      }
    }

    const cacheKey = getCacheKey(provider, code);
    const exchange =
      oauthExchangeCache.get(cacheKey) ??
      (provider === 'google'
        ? completeGoogleLogin(code)
        : completeZaloLogin(code, codeVerifier ?? ''));

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
  }, [code, completeGoogleLogin, completeZaloLogin, navigate, provider, stateParam]);

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
              ? `Mã xác thực ${getProviderLabel(provider)} không hợp lệ`
              : `Đang hoàn tất đăng nhập ${getProviderLabel(provider)}`}
          </h1>
          <p className={styles.message}>
            {state === 'error'
              ? `Không thể hoàn tất đăng nhập ${getProviderLabel(provider)}. Hãy quay lại trang đăng nhập và thử lại.`
              : 'Hệ thống đang xác minh mã uỷ quyền và tạo phiên đăng nhập bằng cookie HttpOnly. Bạn sẽ được chuyển tới trang tài khoản ngay lập tức.'}
          </p>

          {state === 'completing' ? (
            <div className={styles.statusRow} aria-live="polite">
              <div className={styles.spinner} />
              <div>
                <p className={styles.statusCopy}>Đang đồng bộ phiên đăng nhập...</p>
                <p className={styles.statusSubcopy}>Vui lòng giữ nguyên trang trong giây lát.</p>
              </div>
            </div>
          ) : null}

          {state === 'error' && error ? (
            <div className={styles.error}>
              {error}
              <div className={styles.hint}>
                Nếu bạn vừa cho phép đăng nhập, hãy quay lại trang login và thử lại. Nếu lỗi vẫn lặp lại, làm mới trang có thể giúp cập nhật cookie phiên.
              </div>
            </div>
          ) : null}
        </div>
      </div>
    </section>
  );
}
