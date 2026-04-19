import { useEffect, useState } from 'react';
import { Routes, Route, useLocation, useNavigate } from 'react-router-dom';
import Header from './components/Header/Header';
import Footer from './components/Footer/Footer';
import ChatWidget from './components/ChatWidget/ChatWidget';
import HomePage from './pages/HomePage/HomePage';
import CollectionPage from './pages/CollectionPage/CollectionPage';
import AiTryonPage from './pages/AiTryonPage/AiTryonPage';
import ProductsPage from './pages/ProductsPage/ProductsPage';
import AccessoriesPage from './pages/AccessoriesPage/AccessoriesPage';
import CartPage from './pages/CartPage/CartPage';
import NotFoundPage from './pages/NotFoundPage/NotFoundPage';
import LoginPage from './pages/LoginPage/LoginPage';
import AccountPage, { type AccountView } from './pages/AccountPage/AccountPage';
import AuthGoogleCallbackPage from './pages/AuthGoogleCallbackPage/AuthGoogleCallbackPage';
import AuthFacebookCallbackPage from './pages/AuthFacebookCallbackPage/AuthFacebookCallbackPage';
import ResetPasswordPage from './pages/ResetPasswordPage/ResetPasswordPage';
import PrivacyPolicyPage from './pages/PrivacyPolicyPage/PrivacyPolicyPage';
import DataDeletionPage from './pages/DataDeletionPage/DataDeletionPage';
import { useAuth } from './auth/useAuth';

function resolveAccountView(pathname: string): AccountView {
  if (pathname.endsWith('/profile/edit')) {
    return 'profile/edit';
  }

  if (pathname.endsWith('/orders')) {
    return 'orders';
  }

  if (pathname.endsWith('/addresses')) {
    return 'addresses';
  }

  return 'profile';
}

export default function App() {
  const location = useLocation();
  const navigate = useNavigate();
  const { status } = useAuth();
  const { pathname } = location;
  const [isAccountModalOpen, setIsAccountModalOpen] = useState(false);
  const [accountView, setAccountView] = useState<AccountView>('profile');
  const hideHeader = pathname === '/login'
    || pathname === '/reset-password'
    || pathname === '/auth/google/callback'
    || pathname === '/auth/facebook/callback';
  const hideFooter = pathname === '/login'
    || pathname === '/reset-password'
    || pathname === '/auth/google/callback'
    || pathname === '/auth/facebook/callback';

  function openAccountModal(view: AccountView = 'profile') {
    setAccountView(view);
    setIsAccountModalOpen(true);
  }

  function closeAccountModal() {
    setIsAccountModalOpen(false);
    setAccountView('profile');
  }

  useEffect(() => {
    if (!pathname.startsWith('/account')) {
      return;
    }

    if (status === 'loading') {
      return;
    }

    if (status === 'anonymous') {
      navigate('/login', { replace: true, state: { from: pathname } });
      return;
    }

    queueMicrotask(() => {
      openAccountModal(resolveAccountView(pathname));
      navigate('/', { replace: true });
    });
  }, [navigate, pathname, status]);

  return (
    <>
      {!hideHeader && <Header onOpenAccount={() => openAccountModal('profile')} />}
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/collection" element={<CollectionPage />} />
        <Route path="/ai-tryon" element={<AiTryonPage />} />
        <Route path="/products" element={<ProductsPage />} />
        <Route path="/accessories" element={<AccessoriesPage />} />
        <Route path="/cart" element={<CartPage />} />
        <Route path="/login" element={<LoginPage />} />
        <Route path="/reset-password" element={<ResetPasswordPage />} />
        <Route path="/auth/google/callback" element={<AuthGoogleCallbackPage />} />
        <Route path="/auth/facebook/callback" element={<AuthFacebookCallbackPage />} />
        <Route path="/privacy-policy" element={<PrivacyPolicyPage />} />
        <Route path="/data-deletion" element={<DataDeletionPage />} />
        <Route path="/account/*" element={<HomePage />} />
        <Route path="*" element={<NotFoundPage />} />
      </Routes>
      {isAccountModalOpen ? (
        <AccountPage
          activeView={accountView}
          onClose={closeAccountModal}
          onViewChange={setAccountView}
        />
      ) : null}
      {!hideFooter && <Footer />}
      <ChatWidget />
    </>
  );
}
