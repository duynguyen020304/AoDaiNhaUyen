import { useCallback, useEffect, useMemo, useState } from 'react';
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
import AuthZaloCallbackPage from './pages/AuthZaloCallbackPage/AuthZaloCallbackPage';
import ResetPasswordPage from './pages/ResetPasswordPage/ResetPasswordPage';
import { AuthModalProvider, type AuthTab } from './auth/AuthModalContext';
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
  const [isAuthModalOpen, setIsAuthModalOpen] = useState(false);
  const [authTab, setAuthTab] = useState<AuthTab>('login');
  const [authRedirectTo, setAuthRedirectTo] = useState('/');
  const hideHeader = pathname === '/reset-password'
    || pathname === '/auth/google/callback'
    || pathname === '/auth/callback/zalo';
  const hideFooter = pathname === '/reset-password'
    || pathname === '/auth/google/callback'
    || pathname === '/auth/callback/zalo';

  function openAccountModal(view: AccountView = 'profile') {
    setAccountView(view);
    setIsAccountModalOpen(true);
  }

  function closeAccountModal() {
    setIsAccountModalOpen(false);
    setAccountView('profile');
  }

  const openAuthModal = useCallback((options?: { from?: string; tab?: AuthTab }) => {
    setAuthRedirectTo(options?.from ?? `${location.pathname}${location.search}`);
    setAuthTab(options?.tab ?? 'login');
    setIsAuthModalOpen(true);
  }, [location.pathname, location.search]);

  const closeAuthModal = useCallback((options?: { skipNavigate?: boolean }) => {
    setIsAuthModalOpen(false);
    if (options?.skipNavigate) {
      return;
    }

    if (location.pathname === '/login' || location.pathname === '/cart' || location.pathname.startsWith('/account')) {
      navigate('/', { replace: true });
    }
  }, [location.pathname, navigate]);

  const authModalContext = useMemo(() => ({ openAuthModal }), [openAuthModal]);
  const authModalFrom = (location.state as { from?: string } | null)?.from ?? '/';
  const cartAuthFrom = pathname === '/cart' && status === 'anonymous' ? '/cart' : null;
  const accountAuthFrom = pathname.startsWith('/account') && status === 'anonymous' ? pathname : null;
  const shouldShowAuthModal = isAuthModalOpen || pathname === '/login' || accountAuthFrom !== null || cartAuthFrom !== null;

  useEffect(() => {
    if (!pathname.startsWith('/account')) {
      return;
    }

    if (status === 'loading') {
      return;
    }

    if (status === 'anonymous') {
      return;
    }

    queueMicrotask(() => {
      openAccountModal(resolveAccountView(pathname));
      navigate('/', { replace: true });
    });
  }, [navigate, pathname, status]);

  return (
    <AuthModalProvider value={authModalContext}>
      {!hideHeader && <Header onOpenAccount={() => openAccountModal('profile')} onOpenAuth={() => openAuthModal()} />}
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/collection" element={<CollectionPage />} />
        <Route path="/ai-tryon" element={<AiTryonPage />} />
        <Route path="/products" element={<ProductsPage />} />
        <Route path="/accessories" element={<AccessoriesPage />} />
        <Route path="/cart" element={status === 'anonymous' ? <HomePage /> : <CartPage />} />
        <Route path="/login" element={<HomePage />} />
        <Route path="/reset-password" element={<ResetPasswordPage />} />
        <Route path="/auth/google/callback" element={<AuthGoogleCallbackPage />} />
        <Route path="/auth/callback/zalo" element={<AuthZaloCallbackPage />} />
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
      {shouldShowAuthModal ? (
        <section className="auth-modal-backdrop" onMouseDown={(event) => {
          if (event.target === event.currentTarget) {
            closeAuthModal();
          }
        }}>
          <LoginPage
            key={authTab}
            initialTab={authTab}
            redirectTo={cartAuthFrom ?? accountAuthFrom ?? (pathname === '/login' ? authModalFrom : authRedirectTo)}
            onClose={closeAuthModal}
          />
        </section>
      ) : null}
      {!hideFooter && <Footer />}
      <ChatWidget />
    </AuthModalProvider>
  );
}
