import { useCallback, useEffect, type MouseEvent } from 'react';
import { useAuthModal } from '../../auth/AuthModalContext';
import { useAuth } from '../../auth/useAuth';
import AccountSidebar from './AccountSidebar';
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

export default function AccountPage({
  activeView,
  onClose,
  onViewChange,
  variant = 'modal',
}: AccountPageProps) {
  const { user, logout } = useAuth();
  const { openAuthModal } = useAuthModal();

  async function handleLogout() {
    await logout();
    onClose();
    openAuthModal();
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

  const content = {
    profile: <AccountInfo onEdit={() => onViewChange('profile/edit')} />,
    'profile/edit': <AccountEditForm onSaved={() => onViewChange('profile')} />,
    orders: <OrderList onRequestClose={variant === 'modal' ? onClose : undefined} />,
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
        <div className={styles.layout}>
          <AccountSidebar
            user={user}
            onLogout={handleLogout}
            activeView={activeView}
            onNavigate={onViewChange}
          />
          <div className={styles.content}>
            {content}
          </div>
        </div>
      </div>
    </section>
  );
}
