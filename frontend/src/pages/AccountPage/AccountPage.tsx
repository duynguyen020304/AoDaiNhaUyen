import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../auth/useAuth';
import AccountSidebar from './AccountSidebar';
import AccountInfo from './AccountInfo';
import AccountEditForm from './AccountEditForm';
import AddressList from './AddressList';
import OrderList from './OrderList';
import styles from './AccountPage.module.css';

export type TabKey = 'info' | 'edit' | 'orders' | 'addresses';

export default function AccountPage() {
  const navigate = useNavigate();
  const { user, logout } = useAuth();
  const [activeTab, setActiveTab] = useState<TabKey>('info');

  async function handleLogout() {
    await logout();
    navigate('/login', { replace: true });
  }

  if (!user) {
    return null;
  }

  function handleTabChange(tab: TabKey) {
    if (tab === 'info' || tab === 'orders' || tab === 'addresses') {
      setActiveTab(tab);
    }
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
        <AccountSidebar
          user={user}
          activeTab={activeTab}
          onTabChange={handleTabChange}
          onLogout={handleLogout}
        />
        <div className={styles.content}>
          {activeTab === 'info' && <AccountInfo onEdit={() => setActiveTab('edit')} />}
          {activeTab === 'edit' && <AccountEditForm onCancel={() => setActiveTab('info')} />}
          {activeTab === 'orders' && <OrderList />}
          {activeTab === 'addresses' && <AddressList />}
        </div>
      </div>
    </section>
  );
}
