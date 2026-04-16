import { useState } from 'react';
import Header from './components/Header/Header';
import HeroBlank from './components/HeroBlank/HeroBlank';
import AiSection from './components/AiSection/AiSection';
import CollectionSection from './components/CollectionSection/CollectionSection';
import MaterialSection from './components/MaterialSection/MaterialSection';
import ProductSection from './components/ProductSection/ProductSection';
import AccessorySection from './components/AccessorySection/AccessorySection';
import StoreSection from './components/StoreSection/StoreSection';
import FeaturesStrip from './components/FeaturesStrip/FeaturesStrip';
import Footer from './components/Footer/Footer';
import CollectionPage from './pages/CollectionPage/CollectionPage';

export type Page = 'home' | 'collection';

export default function App() {
  const [page, setPage] = useState<Page>('home');

  return (
    <>
      <Header currentPage={page} onNavigate={setPage} />
      {page === 'collection' ? (
        <CollectionPage />
      ) : (
        <main id="top">
          <HeroBlank />
          <AiSection />
          <CollectionSection />
          <MaterialSection />
          <ProductSection />
          <AccessorySection />
          <StoreSection />
          <FeaturesStrip />
        </main>
      )}
      <Footer />
    </>
  );
}
