import { motion, useReducedMotion } from 'framer-motion';
import styles from './AccessorySection.module.css';
import { cardHover, fadeScale, fadeUp, sectionReveal, viewportOnce } from '../../utils/motion';

export default function AccessorySection() {
  const prefersReducedMotion = useReducedMotion();

  return (
    <motion.section
      className={styles.accessorySection}
      id="accessories"
      aria-labelledby="accessory-title"
      variants={sectionReveal}
      initial="hidden"
      whileInView="show"
      viewport={viewportOnce}
    >
      <motion.img
        className={styles.accessoryHero}
        src="/assets/accessory-tram.gif"
        alt="Trâm cài tóc Nhã Uyên"
        variants={fadeScale}
        whileHover={prefersReducedMotion ? undefined : { scale: 1.018 }}
        transition={{ duration: 0.36, ease: 'easeOut' }}
      />
      <motion.div
        className={styles.accessoryCard}
        variants={fadeUp}
        whileHover={prefersReducedMotion ? undefined : cardHover.hover}
        transition={{ duration: 0.28, ease: 'easeOut' }}
      >
        <h2 id="accessory-title" className={styles.accessoryCardTitle}>Trâm cài tóc</h2>
        <p>
          Được chế tác từ lụa tơ tằm cao cấp và da thuộc thủ công, phụ kiện Nhã Uyên tôn vinh
          cốt cách thanh cao của phụ nữ Việt.
        </p>
      </motion.div>
      <motion.h3
        className={styles.accessoryLabel}
        variants={fadeUp}
        animate={prefersReducedMotion ? undefined : { y: [0, -8, 0] }}
        transition={{ duration: 5.6, repeat: Infinity, ease: 'easeInOut' }}
      >
        Phụ kiện
      </motion.h3>
    </motion.section>
  );
}
