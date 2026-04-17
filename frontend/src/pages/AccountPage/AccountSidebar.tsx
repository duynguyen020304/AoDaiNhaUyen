import type { AuthUser } from '../../types/auth';
import { resolveAssetUrl } from '../../api/client';
import type { TabKey } from './AccountPage';
import styles from './AccountSidebar.module.css';

interface AccountSidebarProps {
  user: AuthUser;
  activeTab: TabKey;
  onTabChange: (tab: TabKey) => void;
  onLogout: () => void;
}

const NAV_ITEMS: { key: TabKey; label: string }[] = [
  { key: 'info', label: 'Thong tin tai khoan' },
  { key: 'orders', label: 'Quan ly don hang' },
  { key: 'addresses', label: 'Danh sach dia chi' },
];

export default function AccountSidebar({
  user,
  activeTab,
  onTabChange,
  onLogout,
}: AccountSidebarProps) {
  const avatarSrc = resolveAssetUrl(user.avatarUrl);
  const initial = user.fullName.charAt(0).toUpperCase();

  return (
    <aside className={styles.sidebar}>
      <div className={styles.header} />
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
          Dang xuat
        </button>
      </nav>
    </aside>
  );
}
