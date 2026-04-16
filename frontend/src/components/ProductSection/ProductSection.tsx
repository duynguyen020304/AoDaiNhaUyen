import { motion, useReducedMotion } from 'framer-motion';
import styles from './ProductSection.module.css';
import { cardReveal, fadeScale, fadeUp, imageParallaxHover, listStagger, sectionReveal, viewportOnce } from '../../utils/motion';

export default function ProductSection() {
  const prefersReducedMotion = useReducedMotion();

  return (
    <motion.section
      className={styles.productSection}
      id="product"
      aria-labelledby="product-title"
      variants={sectionReveal}
      initial="hidden"
      whileInView="show"
      viewport={viewportOnce}
    >
      <motion.div className={styles.productCopy} variants={fadeUp}>
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
      </motion.div>
      <motion.div
        className={styles.productFigure}
        variants={fadeScale}
        whileHover={prefersReducedMotion ? undefined : 'hover'}
      >
        <div className={styles.archedPanel}>
          <motion.img
            src="/assets/dress-panel.png"
            alt=""
            variants={imageParallaxHover}
            transition={{ duration: 0.42, ease: 'easeOut' }}
          />
        </div>
        <motion.img
          className={styles.productModelLarge}
          src="/assets/product-model.png"
          alt="Mẫu áo dài cách tân hồng phấn"
          variants={imageParallaxHover}
          transition={{ duration: 0.42, ease: 'easeOut' }}
        />
        <motion.img
          className={styles.dragonSoft}
          src="/assets/dragon.png"
          alt=""
          aria-hidden="true"
          animate={prefersReducedMotion ? undefined : { rotate: [17.52, 20, 17.52], opacity: [0.12, 0.18, 0.12] }}
          transition={{ duration: 8, repeat: Infinity, ease: 'easeInOut' }}
        />
      </motion.div>
      <motion.div className={styles.sideGallery} aria-hidden="true" variants={listStagger}>
        {['/assets/product-model.png', '/assets/dress-pink.png', '/assets/dress-green.png'].map((src) => (
          <motion.img
            key={src}
            src={src}
            alt=""
            variants={cardReveal}
            whileHover={prefersReducedMotion ? undefined : { y: -6, scale: 1.025 }}
            transition={{ duration: 0.28, ease: 'easeOut' }}
          />
        ))}
      </motion.div>
    </motion.section>
  );
}
