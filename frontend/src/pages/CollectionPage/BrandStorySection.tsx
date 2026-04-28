import { motion } from 'framer-motion';
import styles from './BrandStorySection.module.css';
import { fadeUp, viewportOnce } from '../../utils/motion';
import { BRAND_STORY, GOLD_GRADIENT, IMG } from './data';

export default function BrandStorySection() {
  return (
    <section className={styles.brand}>
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
        initial="hidden"
        whileInView="show"
        viewport={viewportOnce}
      >
        {BRAND_STORY.title}
      </motion.h2>

      {/* Two-column text */}
      <div className={styles.columns}>
        <motion.p
          className={styles.paragraph}
          variants={fadeUp}
          initial="hidden"
          whileInView="show"
          viewport={viewportOnce}
        >
          {BRAND_STORY.leftText}
        </motion.p>
        <motion.p
          className={`${styles.paragraph} ${styles.paragraphRight}`}
          variants={fadeUp}
          initial="hidden"
          whileInView="show"
          viewport={viewportOnce}
        >
          {BRAND_STORY.rightText}
        </motion.p>
      </div>
    </section>
  );
}
