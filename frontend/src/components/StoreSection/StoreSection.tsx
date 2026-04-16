import { motion, useReducedMotion } from 'framer-motion';
import styles from './StoreSection.module.css';
import { cardHover, fadeScale, fadeUp, imageParallaxHover, sectionReveal, viewportOnce } from '../../utils/motion';

export default function StoreSection() {
  const prefersReducedMotion = useReducedMotion();

  return (
    <motion.section
      className={styles.storeSection}
      aria-labelledby="store-title"
      variants={sectionReveal}
      initial="hidden"
      whileInView="show"
      viewport={viewportOnce}
    >
      <motion.div
        className={styles.storeCard}
        variants={fadeUp}
        whileHover={prefersReducedMotion ? undefined : cardHover.hover}
        transition={{ duration: 0.28, ease: 'easeOut' }}
      >
        <h2 id="store-title">Cửa hàng<br />Nhã Uyên</h2>
        <p>98, 96/5A Đ. Nguyễn Công Hoan, Cầu Kiệu, Hồ Chí Minh 72200, Việt Nam</p>
        <p>0938 424 241</p>
        <p>0310623075</p>
        <p>Thứ 2 - Thứ 7: 06:00 - 18:00</p>
      </motion.div>
      <motion.div
        className={styles.storeMap}
        variants={fadeScale}
        whileHover={prefersReducedMotion ? undefined : 'hover'}
      >
        <motion.img
          src="/assets/store-map.png"
          alt="Ảnh cửa hàng Nhã Uyên"
          variants={imageParallaxHover}
          transition={{ duration: 0.42, ease: 'easeOut' }}
        />
        <img src="/assets/store-overlay.png" alt="" aria-hidden="true" />
      </motion.div>
      <motion.h2 className={styles.storeHeading} variants={fadeUp}>Hệ thống cửa hàng</motion.h2>
    </motion.section>
  );
}
