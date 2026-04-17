import { motion } from 'framer-motion';
import styles from './CategoryBanner.module.css';
import { sectionReveal, fadeUp, viewportOnce } from '../../utils/motion';

interface CategoryBannerProps {
  title: string;
}

export default function CategoryBanner({ title }: CategoryBannerProps) {
  return (
    <motion.section
      className={styles.banner}
      variants={sectionReveal}
      initial="hidden"
      whileInView="show"
      viewport={viewportOnce}
    >
      <div className={styles.textureOverlay} />
      <motion.h2 className={styles.title} variants={fadeUp}>
        {title}
      </motion.h2>
      <img
        className={styles.drumPattern}
        src="/assets/drum-pattern.png"
        alt=""
        aria-hidden="true"
      />
    </motion.section>
  );
}
