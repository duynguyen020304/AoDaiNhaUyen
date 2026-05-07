import { useCallback, useState } from 'react';
import { AnimatePresence } from 'framer-motion';
import styles from './HomePage.module.css';
import HeroBlank from '../../components/HeroBlank/HeroBlank';
import AiSection from '../../components/AiSection/AiSection';
import CollectionSection from '../../components/CollectionSection/CollectionSection';
import MaterialSection from '../../components/MaterialSection/MaterialSection';
import ProductSection from '../../components/ProductSection/ProductSection';
import AccessorySection from '../../components/AccessorySection/AccessorySection';
import StoreSection from '../../components/StoreSection/StoreSection';
import FeaturesStrip from '../../components/FeaturesStrip/FeaturesStrip';
import LoadingScreen from '../../components/LoadingScreen/LoadingScreen';

const HOME_LOADING_STORAGE_KEY = 'aodai-home-loading-seen';

export default function HomePage() {
  const [isLoading, setIsLoading] = useState(() => {
    if (typeof window === 'undefined') {
      return false;
    }

    return window.sessionStorage.getItem(HOME_LOADING_STORAGE_KEY) !== 'true';
  });

  const handleLoadingComplete = useCallback(() => {
    window.sessionStorage.setItem(HOME_LOADING_STORAGE_KEY, 'true');
    setIsLoading(false);
  }, []);

  return (
    <>
      <AnimatePresence>
        {isLoading ? <LoadingScreen onComplete={handleLoadingComplete} /> : null}
      </AnimatePresence>
      <main id="top" className={styles.home} aria-busy={isLoading}>
        <HeroBlank />
        <AiSection />
        <CollectionSection />
        <MaterialSection />
        <ProductSection />
        <AccessorySection />
        <StoreSection />
        <FeaturesStrip />
      </main>
    </>
  );
}
