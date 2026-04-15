import styles from './ProductSection.module.css';

export default function ProductSection() {
  return (
    <section className={styles.productSection} id="product" aria-labelledby="product-title">
      <div className={styles.productCopy}>
        <p className="gold eyebrow">Áo Dài Nhã Uyên</p>
        <h2 id="product-title">ÁO DÀI<br />CÁCH TÂN</h2>
        <div className={styles.productDetail}>
          <p><strong>1. Đặc điểm thiết kế</strong></p>
          <ul>
            <li>Dáng suông hiện đại, không chiết eo, tạo sự thoải mái và thanh lịch.</li>
            <li>Cổ tàu thấp, mang nét truyền thống pha chút trẻ trung.</li>
            <li>Tay cánh tiên xòe rộng bằng vải voan tơ, tạo hiệu ứng bay bổng.</li>
          </ul>
          <p><strong>2. Thành phần phối bộ</strong></p>
          <ul>
            <li>Lụa cao cấp hoặc gấm mềm, bề mặt có độ bóng nhẹ.</li>
            <li>Quần lụa ống rộng cùng tông hồng hoặc trắng kem để tôn dáng.</li>
          </ul>
        </div>
      </div>
      <div className={styles.productFigure}>
        <div className={styles.archedPanel}>
          <img src="/assets/dress-panel.png" alt="" />
        </div>
        <img className={styles.productModelLarge} src="/assets/product-model.png" alt="Mẫu áo dài cách tân hồng phấn" />
        <img className={styles.dragonSoft} src="/assets/dragon.png" alt="" aria-hidden="true" />
      </div>
      <div className={styles.sideGallery} aria-hidden="true">
        <img src="/assets/product-model.png" alt="" />
        <img src="/assets/dress-pink.png" alt="" />
        <img src="/assets/dress-green.png" alt="" />
      </div>
    </section>
  );
}
