import { useEffect, useMemo, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { forgotPassword, register } from '../../api/auth';
import { type AuthTab } from '../../auth/AuthModalContext';
import { useAuth } from '../../auth/useAuth';
import { useToast } from '../../components/Toast/useToast';
import styles from './LoginPage.module.css';

interface LoginPageProps {
  initialTab?: AuthTab;
  redirectTo?: string;
  onClose?: (options?: { skipNavigate?: boolean }) => void;
}

export default function LoginPage({ initialTab = 'login', redirectTo: redirectOverride, onClose }: LoginPageProps) {
  const navigate = useNavigate();
  const location = useLocation();
  const { login, startGoogleLogin, startZaloLogin, status } = useAuth();
  const { showToast } = useToast();
  const [activeTab, setActiveTab] = useState<AuthTab>(initialTab);
  const [showForgotPassword, setShowForgotPassword] = useState(false);

  // Login fields
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [rememberMe, setRememberMe] = useState(false);

  // Register fields
  const [regName, setRegName] = useState('');
  const [regEmail, setRegEmail] = useState('');
  const [regPhone, setRegPhone] = useState('');
  const [regPassword, setRegPassword] = useState('');
  const [regConfirm, setRegConfirm] = useState('');
  const [forgotEmail, setForgotEmail] = useState('');

  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const query = useMemo(() => new URLSearchParams(location.search), [location.search]);

  const redirectTo = redirectOverride ?? location.state?.from ?? '/';
  const verified = query.get('verified');
  const autoLogin = query.get('autologin');
  const verificationReason = query.get('reason');

  useEffect(() => {
    if (verified === 'true') {
      showToast('Email đã được xác thực. Hệ thống đang khôi phục phiên đăng nhập của bạn.', 'success');
      window.setTimeout(() => {
        setError(null);
        setActiveTab('login');
        setShowForgotPassword(false);
      }, 0);
      return;
    }

    if (verified === 'false') {
      const message = verificationReason === 'verification_token_expired'
        ? 'Liên kết xác thực đã hết hạn. Vui lòng đăng ký lại hoặc liên hệ hỗ trợ.'
        : 'Không thể xác thực email với liên kết này.';
      showToast(message, 'error');
      window.setTimeout(() => {
        setActiveTab('login');
        setShowForgotPassword(false);
      }, 0);
    }
  }, [showToast, verificationReason, verified]);

  useEffect(() => {
    if (verified === 'true' && autoLogin === 'true' && status === 'authenticated') {
      navigate('/', { replace: true });
    }
  }, [autoLogin, navigate, status, verified]);

  async function handleLogin(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setIsSubmitting(true);
    setError(null);
    try {
      await login(email, password);
      if (onClose) {
        onClose({ skipNavigate: true });
        if (redirectTo !== location.pathname) {
          navigate(redirectTo, { replace: true });
        }
      } else {
        navigate(redirectTo, { replace: true });
      }
    } catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : 'Không thể đăng nhập.');
    } finally {
      setIsSubmitting(false);
    }
  }

  async function handleRegister(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setIsSubmitting(true);
    setError(null);
    if (regPassword !== regConfirm) {
      setError('Mật khẩu xác nhận không khớp.');
      setIsSubmitting(false);
      return;
    }

    try {
      await register({
        fullName: regName,
        email: regEmail,
        phone: regPhone,
        password: regPassword,
        confirmPassword: regConfirm,
      });
      showToast('Đăng ký thành công. Vui lòng kiểm tra email để xác thực tài khoản.', 'success');
      setActiveTab('login');
      setRegPassword('');
      setRegConfirm('');
      setPassword('');
      setEmail(regEmail);
    } catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : 'Không thể đăng ký tài khoản.');
    } finally {
      setIsSubmitting(false);
    }
  }

  async function handleForgotPassword() {
    setIsSubmitting(true);
    setError(null);

    try {
      await forgotPassword(forgotEmail);
      showToast('Nếu email tồn tại trong hệ thống, hướng dẫn đặt lại mật khẩu đã được gửi.', 'success');
      setShowForgotPassword(false);
      setForgotEmail('');
    } catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : 'Không thể gửi email đặt lại mật khẩu.');
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <section className={onClose ? styles.modalContent : styles.page}>
      <div className={styles.container}>
        {onClose ? (
          <button className={styles.closeButton} type="button" onClick={() => onClose()} aria-label="Đóng">
            ✕
          </button>
        ) : null}
        <div className={styles.tabs}>
          <button
            className={activeTab === 'login' ? styles.tabActive : styles.tab}
            onClick={() => { setActiveTab('login'); setError(null); setShowForgotPassword(false); }}
          >
            ĐĂNG NHẬP
          </button>
          <button
            className={activeTab === 'register' ? styles.tabActive : styles.tab}
            onClick={() => { setActiveTab('register'); setError(null); setShowForgotPassword(false); }}
          >
            ĐĂNG KÝ
          </button>
        </div>

        {activeTab === 'login' && (
          <form className={styles.form} onSubmit={handleLogin}>
            <div className={styles.field}>
              <label htmlFor="login-email">Email *</label>
              <div className={styles.inputWrapper}>
                <img className={styles.inputIcon} src="/assets/login/icon-email.svg" alt="" />
                <input
                  id="login-email"
                  type="email"
                  autoComplete="email"
                  placeholder="example@gmail.com"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  required
                />
              </div>
            </div>

            <div className={styles.field}>
              <label htmlFor="login-password">Mật khẩu *</label>
              <div className={styles.inputWrapper}>
                <img className={styles.inputIcon} src="/assets/login/icon-password.svg" alt="" />
                <input
                  id="login-password"
                  type="password"
                  autoComplete="current-password"
                  placeholder="••••••••"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  required
                />
              </div>
            </div>

            <div className={styles.options}>
              <label className={styles.remember}>
                <input
                  type="checkbox"
                  checked={rememberMe}
                  onChange={(e) => setRememberMe(e.target.checked)}
                />
                Ghi nhớ đăng nhập
              </label>
              <button
                type="button"
                className={styles.forgot}
                onClick={() => {
                  setShowForgotPassword((current) => !current);
                  setError(null);
                  setForgotEmail(email);
                }}
              >
                Quên mật khẩu?
              </button>
            </div>

            {showForgotPassword ? (
              <div className={styles.inlinePanel}>
                <p className={styles.inlineCopy}>
                  Nhập email đăng ký. Nếu tài khoản tồn tại, chúng tôi sẽ gửi liên kết đặt lại mật khẩu.
                </p>
                <div className={styles.inlineForm}>
                  <div className={styles.field}>
                    <label htmlFor="forgot-email">Email khôi phục</label>
                    <div className={styles.inputWrapper}>
                      <img className={styles.inputIcon} src="/assets/login/icon-email.svg" alt="" />
                      <input
                        id="forgot-email"
                        type="email"
                        autoComplete="email"
                        placeholder="example@gmail.com"
                        value={forgotEmail}
                        onChange={(event) => setForgotEmail(event.target.value)}
                        required
                      />
                    </div>
                  </div>
                  <button
                    className={styles.secondaryBtn}
                    type="button"
                    disabled={isSubmitting}
                    onClick={() => { void handleForgotPassword(); }}
                  >
                    {isSubmitting ? 'Đang gửi...' : 'Gửi email đặt lại mật khẩu'}
                  </button>
                </div>
              </div>
            ) : null}

            <button className={styles.loginBtn} type="submit" disabled={isSubmitting}>
              {isSubmitting ? 'Đang xử lý...' : 'ĐĂNG NHẬP'}
            </button>

            <div className={styles.divider}>
              <span className={styles.dividerLine} />
              <span className={styles.dividerText}>Hoặc tiếp tục với</span>
              <span className={styles.dividerLine} />
            </div>

            <div className={styles.socialButtons}>
              <button type="button" className={styles.socialBtn} onClick={startGoogleLogin}>
                <img src="/assets/login/icon-google.svg" alt="Google" />
                Google
              </button>
              <button type="button" className={styles.socialBtn} onClick={startZaloLogin}>
                <img src="/assets/login/icon-zalo.svg" alt="Zalo" />
                Zalo
              </button>
            </div>
          </form>
        )}

        {activeTab === 'register' && (
          <form className={styles.form} onSubmit={handleRegister}>
            <div className={styles.field}>
              <label htmlFor="reg-name">Họ và Tên *</label>
              <div className={styles.inputWrapper}>
                <img className={styles.inputIcon} src="/assets/login/icon-name.svg" alt="" />
                <input
                  id="reg-name"
                  type="text"
                  autoComplete="name"
                  placeholder="Nguyễn Văn A"
                  value={regName}
                  onChange={(e) => setRegName(e.target.value)}
                  required
                />
              </div>
            </div>

            <div className={styles.field}>
              <label htmlFor="reg-email">Email *</label>
              <div className={styles.inputWrapper}>
                <img className={styles.inputIcon} src="/assets/login/icon-email.svg" alt="" />
                <input
                  id="reg-email"
                  type="email"
                  autoComplete="email"
                  placeholder="example@gmail.com"
                  value={regEmail}
                  onChange={(e) => setRegEmail(e.target.value)}
                  required
                />
              </div>
            </div>

            <div className={styles.field}>
              <label htmlFor="reg-phone">Số điện thoại *</label>
              <div className={styles.inputWrapper}>
                <img className={styles.inputIcon} src="/assets/login/icon-phone.svg" alt="" />
                <input
                  id="reg-phone"
                  type="tel"
                  autoComplete="tel"
                  placeholder="0901 234 567"
                  value={regPhone}
                  onChange={(e) => setRegPhone(e.target.value)}
                  required
                />
              </div>
            </div>

            <div className={styles.field}>
              <label htmlFor="reg-password">Mật khẩu *</label>
              <div className={styles.inputWrapper}>
                <img className={styles.inputIcon} src="/assets/login/icon-password.svg" alt="" />
                <input
                  id="reg-password"
                  type="password"
                  autoComplete="new-password"
                  placeholder="••••••••"
                  value={regPassword}
                  onChange={(e) => setRegPassword(e.target.value)}
                  required
                />
              </div>
            </div>

            <div className={styles.field}>
              <label htmlFor="reg-confirm">Xác nhận mật khẩu *</label>
              <div className={styles.inputWrapper}>
                <img className={styles.inputIcon} src="/assets/login/icon-password.svg" alt="" />
                <input
                  id="reg-confirm"
                  type="password"
                  autoComplete="new-password"
                  placeholder="••••••••"
                  value={regConfirm}
                  onChange={(e) => setRegConfirm(e.target.value)}
                  required
                />
              </div>
            </div>

            <button className={styles.loginBtn} type="submit" disabled={isSubmitting}>
              ĐĂNG KÝ
            </button>

            <div className={styles.divider}>
              <span className={styles.dividerLine} />
              <span className={styles.dividerText}>Hoặc tiếp tục với</span>
              <span className={styles.dividerLine} />
            </div>

            <div className={styles.socialButtons}>
              <button type="button" className={styles.socialBtn} onClick={startGoogleLogin}>
                <img src="/assets/login/icon-google.svg" alt="Google" />
                Google
              </button>
              <button type="button" className={styles.socialBtn} onClick={startZaloLogin}>
                <img src="/assets/login/icon-zalo.svg" alt="Zalo" />
                Zalo
              </button>
            </div>

            <p className={styles.terms}>
              Bằng việc đăng ký, bạn đã đồng ý với Điều khoản sử dụng và Chính sách bảo mật của Nhã Uyên
            </p>
          </form>
        )}

        {error ? <p className={styles.error}>{error}</p> : null}
      </div>
    </section>
  );
}
