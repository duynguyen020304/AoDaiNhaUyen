import type { AuthUser } from '../../types/auth';
import { resolveAssetUrl } from '../../api/client';
import { useNavigate } from 'react-router-dom';
import styles from './AccountSidebar.module.css';
import type { AccountView } from './AccountPage';

interface AccountSidebarProps {
  user: AuthUser;
  onLogout: () => void;
  activeView: AccountView;
  onNavigate: (view: AccountView) => void;
}

const NAV_ITEMS: { view: AccountView; label: string }[] = [
  { view: 'profile', label: 'Thông tin tài khoản' },
  { view: 'orders', label: 'Quản lý đơn hàng' },
  { view: 'addresses', label: 'Danh sách địa chỉ' },
];

export default function AccountSidebar({
  user,
  onLogout,
  activeView,
  onNavigate,
}: AccountSidebarProps) {
  const navigate = useNavigate();
  const avatarSrc = resolveAssetUrl(user.avatarUrl);
  const initial = user.fullName.charAt(0).toUpperCase();
  const activeRootView = activeView === 'profile/edit' ? 'profile' : activeView;

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
        {NAV_ITEMS.map(({ view, label }) => (
          <button
            key={view}
            type="button"
            className={`${styles.navItem} ${activeRootView === view ? styles.navItemActive : ''}`}
            onClick={() => onNavigate(view)}
          >
            {label}
          </button>
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
