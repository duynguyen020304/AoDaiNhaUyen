import { AnimatePresence, motion } from 'framer-motion';
import styles from './ProductSection.module.css';
import { carouselCopy, carouselCopyItem } from '../../utils/motion';
import type { ProductSectionNote, ProductSectionSlide } from './productSectionData';

const topFlourish = '/assets/top-flourish.png';

type ProductSectionCopyProps = {
  activeNote: ProductSectionNote | null;
  slide: ProductSectionSlide;
};

export default function ProductSectionCopy({ activeNote, slide }: ProductSectionCopyProps) {
  const detailItems = activeNote?.lines ?? slide.detailLines;

  return (
    <div className={styles.copyColumn}>
      <AnimatePresence mode="wait">
        <motion.div
          key={`${slide.id}-${activeNote?.id ?? 'overview'}`}
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
            {activeNote ? (
              <>
                <p className={styles.detailLabel}>{activeNote.title}</p>
                <ul className={styles.noteList}>
                  {activeNote.lines.map((line) => (
                    <li key={`${activeNote.id}-${line}`}>{line}</li>
                  ))}
                </ul>
              </>
            ) : (
              detailItems.map((line, index) => (
                <p
                  key={`${slide.id}-${line}`}
                  className={index % 3 === 0 ? styles.detailLabel : styles.detailLine}
                >
                  {line}
                </p>
              ))
            )}
          </motion.div>
        </motion.div>
      </AnimatePresence>
    </div>
  );
}
