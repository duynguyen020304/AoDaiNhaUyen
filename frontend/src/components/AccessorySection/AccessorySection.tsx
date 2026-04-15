import styles from './AccessorySection.module.css';

export default function AccessorySection() {
  return (
    <section className={styles.accessorySection} id="accessories" aria-labelledby="accessory-title">
      <img className={styles.accessoryHero} src="/assets/accessory-tram.gif" alt="Trâm cài tóc Nhã Uyên" />
      <div className={styles.accessoryCard}>
        <h2 id="accessory-title" className={styles.accessoryCardTitle}>Trâm cài tóc</h2>
        <p>
          Được chế tác từ lụa tơ tằm cao cấp và da thuộc thủ công, phụ kiện Nhã Uyên tôn vinh
          cốt cách thanh cao của phụ nữ Việt.
        </p>
      </div>
      <h3 className={styles.accessoryLabel}>Phụ kiện</h3>
    </section>
  );
}
