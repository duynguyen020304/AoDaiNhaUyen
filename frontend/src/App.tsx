import { Routes, Route } from 'react-router-dom';
import Header from './components/Header/Header';
import Footer from './components/Footer/Footer';
import HomePage from './pages/HomePage/HomePage';
import CollectionPage from './pages/CollectionPage/CollectionPage';
import AiTryonPage from './pages/AiTryonPage/AiTryonPage';
import ProductsPage from './pages/ProductsPage/ProductsPage';
import AccessoriesPage from './pages/AccessoriesPage/AccessoriesPage';
import NotFoundPage from './pages/NotFoundPage/NotFoundPage';

export default function App() {
  return (
    <>
      <Header />
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/collection" element={<CollectionPage />} />
        <Route path="/ai-tryon" element={<AiTryonPage />} />
        <Route path="/products" element={<ProductsPage />} />
        <Route path="/accessories" element={<AccessoriesPage />} />
        <Route path="*" element={<NotFoundPage />} />
      </Routes>
      <Footer />
    </>
  );
}
