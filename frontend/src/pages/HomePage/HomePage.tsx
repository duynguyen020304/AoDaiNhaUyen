import styles from './HomePage.module.css';
import HeroBlank from '../../components/HeroBlank/HeroBlank';
// import AiSection from '../../components/AiSection/AiSection';
import CollectionSection from '../../components/CollectionSection/CollectionSection';
import MaterialSection from '../../components/MaterialSection/MaterialSection';
import ProductSection from '../../components/ProductSection/ProductSection';
import AccessorySection from '../../components/AccessorySection/AccessorySection';
import StoreSection from '../../components/StoreSection/StoreSection';
import FeaturesStrip from '../../components/FeaturesStrip/FeaturesStrip';

export default function HomePage() {
  return (
    <main id="top" className={styles.home}>
      <HeroBlank />
      {/* <AiSection /> */}
      <CollectionSection />
      <MaterialSection />
      <ProductSection />
      <AccessorySection />
      <StoreSection />
      <FeaturesStrip />
    </main>
  );
}
