import { useCallback, useEffect, type MouseEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../auth/useAuth';
import AccountSidebar from './AccountSidebar';
import AccountInfo from './AccountInfo';
import AccountEditForm from './AccountEditForm';
import OrderList from './OrderList';
import AddressList from './AddressList';
import styles from './AccountPage.module.css';

export type AccountView = 'profile' | 'profile/edit' | 'orders' | 'addresses';

interface AccountPageProps {
  activeView: AccountView;
  onClose: () => void;
  onViewChange: (view: AccountView) => void;
}

export default function AccountPage({
  activeView,
  onClose,
  onViewChange,
}: AccountPageProps) {
  const navigate = useNavigate();
  const { user, logout } = useAuth();

  async function handleLogout() {
    await logout();
    navigate('/login', { replace: true });
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
    orders: <OrderList />,
    addresses: <AddressList />,
  }[activeView];

  return (
    <section className={styles.page} onMouseDown={handleBackdropMouseDown}>
      <div
        className={styles.dialog}
        role="dialog"
        aria-modal="true"
        aria-label="Thông tin tài khoản"
      >
        <button
          className={styles.closeButton}
          type="button"
          onClick={handleClose}
          aria-label="Đóng"
        >
          ✕
        </button>
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
