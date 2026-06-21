import { useCallback, useEffect, type MouseEvent } from 'react';
import { useAuthModal } from '../../auth/AuthModalContext';
import { useAuth } from '../../auth/useAuth';
import { resolveAssetUrl } from '../../api/client';
import { formatAccountDisplayName, getAccountInitial, getAccountRoleLabel } from '../../utils/accountDisplay';
import AccountInfo from './AccountInfo';
import AccountEditForm from './AccountEditForm';
import OrderList from './OrderList';
import AddressList from './AddressList';
import ImageHistory from './ImageHistory';
import styles from './AccountPage.module.css';

export type AccountView = 'profile' | 'profile/edit' | 'orders' | 'addresses' | 'images';

interface AccountPageProps {
  activeView: AccountView;
  onClose: () => void;
  onViewChange: (view: AccountView) => void;
  variant?: 'modal' | 'page';
}

const NAV_ITEMS: { view: AccountView; label: string; icon: string }[] = [
  { view: 'profile', label: 'Thông tin tài khoản', icon: '◉' },
  { view: 'orders', label: 'Quản lý đơn hàng', icon: '▣' },
  { view: 'addresses', label: 'Danh sách địa chỉ', icon: '⌖' },
  { view: 'images', label: 'Hình ảnh của tôi', icon: '□' },
];

export default function AccountPage({
  activeView,
  onClose,
  onViewChange,
  variant = 'modal',
}: AccountPageProps) {
  const { user, logout } = useAuth();
  const { openAuthModal } = useAuthModal();

  async function handleLogout() {
    try {
      await logout();
    } catch {
      // ignore errors - still clear local state
    } finally {
      onClose();
      openAuthModal();
    }
  }

  const handleClose = useCallback(() => {
    onClose();
  }, [onClose]);

  function handleBackdropMouseDown(event: MouseEvent<HTMLElement>) {
    if (event.target === event.currentTarget) {
      handleClose();
    }
  }

  useEffect(() => {
    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        handleClose();
      }
    }

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [handleClose]);

  if (!user) {
    return null;
  }

  const avatarSrc = resolveAssetUrl(user.avatarUrl);
  const displayName = formatAccountDisplayName(user);
  const initial = getAccountInitial(user);
  const roleLabel = getAccountRoleLabel(user);
  const activeRootView = activeView === 'profile/edit' ? 'profile' : activeView;
  const content = {
    profile: <AccountInfo onEdit={() => onViewChange('profile/edit')} onNavigate={onViewChange} />,
    'profile/edit': <AccountEditForm onSaved={() => onViewChange('profile')} />,
    orders: <OrderList />,
    addresses: <AddressList />,
    images: <ImageHistory />,
  }[activeView];

  return (
    <section className={variant === 'page' ? styles.accountPage : styles.page} onMouseDown={variant === 'modal' ? handleBackdropMouseDown : undefined}>
      <div
        className={variant === 'page' ? styles.pageShell : styles.dialog}
        role={variant === 'modal' ? 'dialog' : undefined}
        aria-modal={variant === 'modal' ? 'true' : undefined}
        aria-label="Thông tin tài khoản"
      >
        {variant === 'modal' ? (
          <button
            className={styles.closeButton}
            type="button"
            onClick={handleClose}
            aria-label="Đóng"
          >
            ✕
          </button>
        ) : null}

        <div className={styles.dashboard}>
          <header className={styles.profileHero}>
            <div className={styles.profileIdentity}>
              <div className={styles.avatarFrame}>
                {avatarSrc ? (
                  <img className={styles.avatar} src={avatarSrc} alt={displayName} />
                ) : (
                  <span className={styles.avatarFallback}>{initial}</span>
                )}
              </div>
              <div className={styles.profileCopy}>
                <p className={styles.eyebrow}>Bảng điều khiển tài khoản</p>
                <h1>{displayName}</h1>
                <p>{user.email ?? 'Chưa cập nhật email'}</p>
                <div className={styles.badges}>
                  <span>{roleLabel}</span>
                  <span>Đã đăng nhập</span>
                </div>
              </div>
            </div>

            <div className={styles.heroActions}>
              <button type="button" className={styles.primaryAction} onClick={() => onViewChange('profile/edit')}>
                Cập nhật hồ sơ
              </button>
              <button type="button" className={styles.logoutAction} onClick={handleLogout}>
                Đăng xuất
              </button>
            </div>
          </header>

          <nav className={styles.tabs} aria-label="Điều hướng tài khoản">
            {NAV_ITEMS.map(({ view, label, icon }) => (
              <button
                key={view}
                type="button"
                className={`${styles.tab} ${activeRootView === view ? styles.tabActive : ''}`}
                onClick={() => onViewChange(view)}
              >
                <span aria-hidden="true">{icon}</span>
                {label}
              </button>
            ))}
          </nav>

          <main className={styles.content}>
            {content}
          </main>
        </div>
      </div>
    </section>
  );
}
