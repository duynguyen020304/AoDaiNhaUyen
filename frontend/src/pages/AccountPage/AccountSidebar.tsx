import type { AuthUser } from '../../types/auth';
import { resolveAssetUrl } from '../../api/client';
import { useNavigate, NavLink } from 'react-router-dom';
import styles from './AccountSidebar.module.css';

interface AccountSidebarProps {
  user: AuthUser;
  onLogout: () => void;
}

const NAV_ITEMS: { path: string; label: string }[] = [
  { path: '/account/profile', label: 'Thông tin tài khoản' },
  { path: '/account/orders', label: 'Quản lý đơn hàng' },
  { path: '/account/addresses', label: 'Danh sách địa chỉ' },
];

export default function AccountSidebar({
  user,
  onLogout,
}: AccountSidebarProps) {
  const navigate = useNavigate();
  const avatarSrc = resolveAssetUrl(user.avatarUrl);
  const initial = user.fullName.charAt(0).toUpperCase();

  return (
    <aside className={styles.sidebar}>
      <div className={styles.header}>
        <button
          type="button"
          className={styles.logoButton}
          onClick={() => navigate('/')}
          aria-label="Về trang chủ"
        >
          <img className={styles.logo} src="/assets/login/logo.svg" alt="Hà Uyên" />
        </button>
      </div>
      <div className={styles.avatarWrapper}>
        {avatarSrc ? (
          <img
            className={styles.avatar}
            src={avatarSrc}
            alt={user.fullName}
          />
        ) : (
          <div className={styles.avatarPlaceholder}>{initial}</div>
        )}
      </div>
      <p className={styles.userName}>{user.fullName}</p>

      <nav className={styles.nav}>
        <button
          type="button"
          className={styles.homeButton}
          onClick={() => navigate('/')}
        >
          Trang chủ
        </button>
        {NAV_ITEMS.map(({ path, label }) => (
          <NavLink
            key={path}
            to={path}
            className={({ isActive }) =>
              `${styles.navItem} ${isActive ? styles.navItemActive : ''}`
            }
          >
            {label}
          </NavLink>
        ))}
        <button
          type="button"
          className={styles.logoutItem}
          onClick={onLogout}
        >
          Đăng xuất
        </button>
      </nav>
    </aside>
  );
}
