import { motion } from 'framer-motion';
import styles from './BrandStorySection.module.css';
import { fadeUp, sectionReveal, viewportOnce } from '../../utils/motion';
import { BRAND_STORY, GOLD_GRADIENT, IMG } from './data';

export default function BrandStorySection() {
  return (
    <motion.section
      className={styles.brand}
      variants={sectionReveal}
      initial="hidden"
      whileInView="show"
      viewport={viewportOnce}
    >
      {/* Background texture */}
      <div className={styles.textureOverlay}>
        <img src={IMG.figmaBst6Bg} alt="" />
      </div>

      <img src={IMG.figmaCloudPattern} alt="" className={styles.cloudDecor} aria-hidden="true" />
      <img src={IMG.figmaVectorRight} alt="" className={styles.vectorDecor} aria-hidden="true" />

      {/* Title */}
      <motion.h2
        className={styles.title}
        style={{ backgroundImage: GOLD_GRADIENT }}
        variants={fadeUp}
      >
        {BRAND_STORY.title}
      </motion.h2>

      {/* Two-column text */}
      <div className={styles.columns}>
        <motion.p className={styles.paragraph} variants={fadeUp}>
          {BRAND_STORY.leftText}
        </motion.p>
        <motion.p className={`${styles.paragraph} ${styles.paragraphRight}`} variants={fadeUp}>
          {BRAND_STORY.rightText}
        </motion.p>
      </div>
    </motion.section>
  );
}
