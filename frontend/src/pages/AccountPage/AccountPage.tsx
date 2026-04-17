import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../auth/useAuth';
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
      <div className={styles.shell}>
        <div className={styles.hero}>
          <p>Tai khoan da xac thuc</p>
          <h1>Xin chao, {user.fullName}.</h1>
        </div>

        <div className={styles.grid}>
          <article className={styles.panel}>
            <h2 className={styles.panelTitle}>Thong tin tai khoan</h2>
            <p className={styles.value}>Email: {user.email ?? 'Chua cap nhat'}</p>
            <p className={styles.value}>Vai tro: {user.roles.join(', ') || 'customer'}</p>
          </article>

          <article className={styles.panel}>
            <h2 className={styles.panelTitle}>Phien dang nhap</h2>
            <p className={styles.value}>
              Access token va refresh token duoc luu trong cookie HttpOnly va khong hien thi trong JavaScript.
            </p>
            <button className={styles.logoutButton} type="button" onClick={handleLogout}>
              Dang xuat
            </button>
          </article>
        </div>
      </div>
    </section>
  );
}
