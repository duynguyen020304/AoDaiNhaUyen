import { useNavigate, Outlet } from 'react-router-dom';
import { useAuth } from '../../auth/useAuth';
import AccountSidebar from './AccountSidebar';
import styles from './AccountPage.module.css';

export default function AccountPage() {
  const navigate = useNavigate();
  const { user, logout } = useAuth();

  async function handleLogout() {
    await logout();
    navigate('/login', { replace: true });
  }

  if (!user) {
    return null;
  }

  return (
    <section className={styles.page}>
      <button
        className={styles.closeButton}
        type="button"
        onClick={() => navigate('/')}
        aria-label="Close"
      >
        ✕
      </button>
      <div className={styles.layout}>
        <AccountSidebar user={user} onLogout={handleLogout} />
        <div className={styles.content}>
          <Outlet />
        </div>
      </div>
    </section>
  );
}
