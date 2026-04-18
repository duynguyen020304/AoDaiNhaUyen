import { motion, useReducedMotion } from 'framer-motion';
import styles from './StoreSection.module.css';
import { cardHover, fadeScale, fadeUp, sectionReveal, viewportOnce } from '../../utils/motion';

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
        <p>
          <i className="fa-solid fa-location-dot" aria-hidden="true" />
          <span>98, 96/5A Đ. Nguyễn Công Hoan, Cầu Kiệu, Hồ Chí Minh 72200, Việt Nam</span>
        </p>
        <p>
          <i className="fa-solid fa-phone" aria-hidden="true" />
          <span>0938 424 241</span>
        </p>
        <p>
          <i className="fa-solid fa-id-card" aria-hidden="true" />
          <span>0310623075</span>
        </p>
        <p>
          <i className="fa-solid fa-clock" aria-hidden="true" />
          <span>Thứ 2 - Thứ 7: 06:00 - 18:00</span>
        </p>
      </motion.div>
      <motion.h2 className={styles.storeHeading} variants={fadeUp}>Hệ thống cửa hàng</motion.h2>
      <motion.div
        className={styles.storeMap}
        variants={fadeScale}
        whileHover={prefersReducedMotion ? undefined : 'hover'}
      >
        <motion.iframe
          src="https://www.google.com/maps?q=Kho%20%C3%81o%20d%C3%A0i%20Nh%C3%A3%20Uy%C3%AAn,%2010.7989693,106.6900893&z=17&output=embed"
        />
      </motion.div>
    </motion.section>
  );
}
