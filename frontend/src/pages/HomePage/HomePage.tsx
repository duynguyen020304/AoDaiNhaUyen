import styles from './HomePage.module.css';
import HeroBlank from '../../components/HeroBlank/HeroBlank';
import AiSection from '../../components/AiSection/AiSection';
import CollectionSection from '../../components/CollectionSection/CollectionSection';
import MaterialSection from '../../components/MaterialSection/MaterialSection';
import ProductSection from '../../components/ProductSection/ProductSection';
import AccessorySection from '../../components/AccessorySection/AccessorySection';
import StoreSection from '../../components/StoreSection/StoreSection';
import FeaturesStrip from '../../components/FeaturesStrip/FeaturesStrip';

interface HomePageProps {
  onOpenChat: () => void;
}

export default function HomePage({ onOpenChat }: HomePageProps) {
  return (
    <main id="top" className={styles.home}>
      <HeroBlank />
      <AiSection />
      <CollectionSection />
      <MaterialSection />
      <ProductSection />
      <AccessorySection />
      <StoreSection />
      <FeaturesStrip />
      <button type="button" className={styles.aiConsultButton} onClick={onOpenChat}>
        <span className={styles.aiConsultIcon}>✦</span>
        <span>Tư vấn AI</span>
      </button>
    </main>
  );
}
