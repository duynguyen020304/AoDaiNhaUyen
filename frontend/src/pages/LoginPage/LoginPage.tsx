import { useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../../auth/useAuth';
import styles from './LoginPage.module.css';

export default function LoginPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const { login, startGoogleLogin } = useAuth();
  const [activeTab, setActiveTab] = useState<'login' | 'register'>('login');

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

  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const redirectTo = location.state?.from || '/account';

  async function handleLogin(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setIsSubmitting(true);
    setError(null);
    try {
      await login(email, password);
      navigate(redirectTo, { replace: true });
    } catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : 'Không thể đăng nhập.');
    } finally {
      setIsSubmitting(false);
    }
  }

  function handleRegister(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    if (regPassword !== regConfirm) {
      setError('Mật khẩu xác nhận không khớp.');
      return;
    }
    // TODO: implement register API
    setError('Chức năng đăng ký chưa được hỗ trợ.');
  }

  return (
    <section className={styles.page}>
      <div className={styles.container}>
        {/* Header: logo + close */}
        <div className={styles.header}>
          <img src="/assets/login/logo.svg" alt="Nhà Uyên" />
          <button className={styles.closeBtn} onClick={() => navigate(-1)} aria-label="Đóng">
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
              <path d="M18 6L6 18" stroke="#99A1AF" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
              <path d="M6 6L18 18" stroke="#99A1AF" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
            </svg>
          </button>
        </div>

        {/* Tabs */}
        <div className={styles.tabs}>
          <button
            className={activeTab === 'login' ? styles.tabActive : styles.tab}
            onClick={() => { setActiveTab('login'); setError(null); }}
          >
            ĐĂNG NHẬP
          </button>
          <button
            className={activeTab === 'register' ? styles.tabActive : styles.tab}
            onClick={() => { setActiveTab('register'); setError(null); }}
          >
            ĐĂNG KÝ
          </button>
        </div>

        {/* Login Form */}
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
              <button type="button" className={styles.forgot}>Quên mật khẩu?</button>
            </div>

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
              <button type="button" className={styles.socialBtn}>
                <img src="/assets/login/icon-facebook.svg" alt="Facebook" />
                Facebook
              </button>
            </div>
          </form>
        )}

        {/* Register Form */}
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
              <button type="button" className={styles.socialBtn}>
                <img src="/assets/login/icon-facebook.svg" alt="Facebook" />
                Facebook
              </button>
            </div>

            <p className={styles.terms}>
              Bằng việc đăng ký, bạn đã đồng ý với Điều khoản sử dụng và Chính sách bảo mật của MaryMy
            </p>
          </form>
        )}

        {error ? <p className={styles.error}>{error}</p> : null}
      </div>
    </section>
  );
}
