import styles from './StoreSection.module.css';

export default function StoreSection() {
  return (
    <section className={styles.storeSection} aria-labelledby="store-title">
      <div className={styles.storeCard}>
        <h2 id="store-title">Cửa hàng<br />Nhã Uyên</h2>
        <p>98, 96/5A Đ. Nguyễn Công Hoan, Cầu Kiệu, Hồ Chí Minh 72200, Việt Nam</p>
        <p>0938 424 241</p>
        <p>0310623075</p>
        <p>Thứ 2 - Thứ 7: 06:00 - 18:00</p>
      </div>
      <div className={styles.storeMap}>
        <img src="/assets/store-map.png" alt="Ảnh cửa hàng Nhã Uyên" />
        <img src="/assets/store-overlay.png" alt="" aria-hidden="true" />
      </div>
      <h2 className={styles.storeHeading}>Hệ thống cửa hàng</h2>
    </section>
  );
}
