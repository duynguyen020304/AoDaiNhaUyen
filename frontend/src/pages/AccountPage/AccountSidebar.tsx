import type { AuthUser } from '../../types/auth';
import { resolveAssetUrl } from '../../api/client';
import { useNavigate } from 'react-router-dom';
import type { TabKey } from './AccountPage';
import styles from './AccountSidebar.module.css';

interface AccountSidebarProps {
  user: AuthUser;
  activeTab: TabKey;
  onTabChange: (tab: TabKey) => void;
  onLogout: () => void;
}

const NAV_ITEMS: { key: TabKey; label: string }[] = [
  { key: 'info', label: 'Thông tin tài khoản' },
  { key: 'orders', label: 'Quản lý đơn hàng' },
  { key: 'addresses', label: 'Danh sách địa chỉ' },
];

export default function AccountSidebar({
  user,
  activeTab,
  onTabChange,
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
        {NAV_ITEMS.map(({ key, label }) => (
          <button
            key={key}
            type="button"
            className={`${styles.navItem} ${
              (key === 'info' && activeTab === 'edit')
                ? styles.navItemActive
                : activeTab === key
                  ? styles.navItemActive
                  : ''
            }`}
            onClick={() => onTabChange(key)}
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
