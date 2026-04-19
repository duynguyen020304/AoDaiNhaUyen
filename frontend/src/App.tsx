import { useLocation, Routes, Route, Navigate } from 'react-router-dom';
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
import AccountPage from './pages/AccountPage/AccountPage';
import AccountInfo from './pages/AccountPage/AccountInfo';
import AccountEditForm from './pages/AccountPage/AccountEditForm';
import OrderList from './pages/AccountPage/OrderList';
import AddressList from './pages/AccountPage/AddressList';
import AuthGoogleCallbackPage from './pages/AuthGoogleCallbackPage/AuthGoogleCallbackPage';
import AuthFacebookCallbackPage from './pages/AuthFacebookCallbackPage/AuthFacebookCallbackPage';
import ResetPasswordPage from './pages/ResetPasswordPage/ResetPasswordPage';
import PrivacyPolicyPage from './pages/PrivacyPolicyPage/PrivacyPolicyPage';
import DataDeletionPage from './pages/DataDeletionPage/DataDeletionPage';
import ProtectedRoute from './auth/ProtectedRoute';

export default function App() {
  const { pathname } = useLocation();
  const hideHeader = pathname === '/login'
    || pathname === '/reset-password'
    || pathname === '/auth/google/callback'
    || pathname === '/auth/facebook/callback'
    || pathname.startsWith('/account');
  const hideFooter = pathname === '/login'
    || pathname === '/reset-password'
    || pathname === '/auth/google/callback'
    || pathname === '/auth/facebook/callback';

  return (
    <>
      {!hideHeader && <Header />}
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
        <Route element={<ProtectedRoute />}>
          <Route path="/account" element={<AccountPage />}>
            <Route index element={<Navigate to="profile" replace />} />
            <Route path="profile" element={<AccountInfo />} />
            <Route path="profile/edit" element={<AccountEditForm />} />
            <Route path="orders" element={<OrderList />} />
            <Route path="addresses" element={<AddressList />} />
          </Route>
        </Route>
        <Route path="*" element={<NotFoundPage />} />
      </Routes>
      {!hideFooter && <Footer />}
      <ChatWidget />
    </>
  );
}
