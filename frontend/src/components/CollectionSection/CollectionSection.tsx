import styles from './CollectionSection.module.css';
import CategoryPills from './CategoryPills';
import DressShowcase from './DressShowcase';

export default function CollectionSection() {
  return (
    <section className={`red-texture ${styles.collectionSection}`} id="collection" aria-labelledby="collection-title">
      <div className={styles.goldStar} aria-hidden="true" />
      <h2 className="script-title" id="collection-title">Bộ sưu tập</h2>
      <CategoryPills />
      <DressShowcase />
    </section>
  );
}
