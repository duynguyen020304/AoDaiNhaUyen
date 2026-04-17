import { Routes, Route, useLocation } from 'react-router-dom';
import Header from './components/Header/Header';
import Footer from './components/Footer/Footer';
import HomePage from './pages/HomePage/HomePage';
import CollectionPage from './pages/CollectionPage/CollectionPage';
import AiTryonPage from './pages/AiTryonPage/AiTryonPage';
import ProductsPage from './pages/ProductsPage/ProductsPage';
import AccessoriesPage from './pages/AccessoriesPage/AccessoriesPage';
import CartPage from './pages/CartPage/CartPage';
import NotFoundPage from './pages/NotFoundPage/NotFoundPage';
import LoginPage from './pages/LoginPage/LoginPage';
import AccountPage from './pages/AccountPage/AccountPage';
import AuthGoogleCallbackPage from './pages/AuthGoogleCallbackPage/AuthGoogleCallbackPage';
import AuthFacebookCallbackPage from './pages/AuthFacebookCallbackPage/AuthFacebookCallbackPage';
import ResetPasswordPage from './pages/ResetPasswordPage/ResetPasswordPage';
import ProtectedRoute from './auth/ProtectedRoute';

export default function App() {
  const { pathname } = useLocation();
  const hideLayout = pathname === '/login'
    || pathname === '/reset-password'
    || pathname === '/auth/google/callback'
    || pathname === '/auth/facebook/callback';

  return (
    <>
      {!hideLayout && <Header />}
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
        <Route element={<ProtectedRoute />}>
          <Route path="/account" element={<AccountPage />} />
        </Route>
        <Route path="*" element={<NotFoundPage />} />
      </Routes>
      {!hideLayout && <Footer />}
    </>
  );
}
