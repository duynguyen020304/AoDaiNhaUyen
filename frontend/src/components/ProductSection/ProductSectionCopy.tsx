import { AnimatePresence, motion } from 'framer-motion';
import styles from './ProductSection.module.css';
import { carouselCopy, carouselCopyItem } from '../../utils/motion';
import type { ProductSectionSlide } from './productSectionData';

const topFlourish = '/assets/top-flourish.png';

export default function ProductSectionCopy({ slide }: { slide: ProductSectionSlide }) {
  return (
    <div className={styles.copyColumn}>
      <AnimatePresence mode="wait">
        <motion.div
          key={slide.id}
          className={styles.copyContent}
          variants={carouselCopy}
          initial="enter"
          animate="center"
          exit="exit"
        >
          <motion.img className={styles.copyLine} src={topFlourish} alt="" aria-hidden="true" variants={carouselCopyItem} />

          <motion.p className={styles.eyebrow} variants={carouselCopyItem}>
            {slide.eyebrow}
          </motion.p>

          <motion.h2 id="product-title" className={styles.copyHeading} variants={carouselCopyItem}>
            {slide.headlineLines.map((line) => (
              <span key={line}>{line}</span>
            ))}
          </motion.h2>

          <motion.div className={styles.detailPanel} variants={carouselCopyItem}>
            {slide.detailLines.map((line, index) => (
              <p
                key={`${slide.id}-${line}`}
                className={index % 3 === 0 ? styles.detailLabel : styles.detailLine}
              >
                {line}
              </p>
            ))}
          </motion.div>
        </motion.div>
      </AnimatePresence>
    </div>
  );
}
