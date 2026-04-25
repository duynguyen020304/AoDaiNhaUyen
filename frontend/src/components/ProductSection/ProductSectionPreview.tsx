import { AnimatePresence, motion } from 'framer-motion';
import styles from './ProductSection.module.css';
import { cardReveal, easeOutQuart, listStagger } from '../../utils/motion';
import type { ProductSectionSlide } from './productSectionData';

const topFlourish = '/assets/top-flourish.png';
const indicatorOffsets = [-1, 0, 1];

type ProductSectionPreviewProps = {
  active: number;
  slide: ProductSectionSlide;
  slides: ProductSectionSlide[];
  onNext: () => void;
  onPrevious: () => void;
  onSelect: (index: number) => void;
};

export default function ProductSectionPreview({
  active,
  slide,
  slides,
  onNext,
  onPrevious,
  onSelect,
}: ProductSectionPreviewProps) {
  const previewOffsets = [-1, 0, 1];
  const previewItems = previewOffsets.map((offset) => {
    const index = (active + offset + slides.length) % slides.length;
    return { index, item: slides[index], offset };
  });

  const getThumbnailPositionClass = (offset: number) => {
    if (offset === 0) {
      return styles.thumbnailCenter;
    }

    return Math.abs(offset) === 1 ? styles.thumbnailInner : styles.thumbnailOuter;
  };

  return (
    <motion.div className={styles.previewColumn} variants={listStagger}>
      <img className={styles.previewFlourish} src={topFlourish} alt="" aria-hidden="true" />

      <div className={styles.previewStack}>
        <motion.div className={styles.previewShadowFrame} variants={cardReveal} aria-hidden="true">
          <motion.img
            key={slide.previewShadowImage}
            className={styles.previewShadow}
            src={slide.previewShadowImage}
            alt=""
          />
        </motion.div>

        <motion.div className={styles.previewFrame} variants={cardReveal}>
          <AnimatePresence mode="wait">
            <motion.img
              key={slide.previewImage}
              className={styles.previewImage}
              src={slide.previewImage}
              alt={slide.previewAlt}
              initial={{ opacity: 0, scale: 1.03 }}
              animate={{ opacity: 1, scale: 1 }}
              exit={{ opacity: 0, scale: 0.98 }}
              transition={{ duration: 0.34, ease: easeOutQuart }}
            />
          </AnimatePresence>

        </motion.div>
      </div>

      <div className={styles.previewIndicatorRail} aria-hidden="true">
        {indicatorOffsets.map((offset) => (
          <span
            key={offset}
            className={`${styles.previewIndicator} ${offset === 0 ? styles.previewIndicatorActive : ''}`}
          />
        ))}
      </div>

      <div className={styles.previewControls}>
        <button
          type="button"
          className={styles.previewArrowLeft}
          onClick={onPrevious}
          aria-label="Xem preview trước"
        >
          ‹
        </button>

        <div className={styles.thumbnailRail}>
          {previewItems.map(({ item, index, offset }) => (
            <button
              key={`${item.id}-${offset}`}
              type="button"
              className={`${styles.thumbnailButton} ${getThumbnailPositionClass(offset)} ${
                offset === 0 ? styles.thumbnailActive : ''
              }`}
              onClick={() => onSelect(index)}
              aria-label={item.previewLabel}
            >
              <img src={item.thumbnailImage} alt="" aria-hidden="true" />
            </button>
          ))}
        </div>

        <button
          type="button"
          className={styles.previewArrowRight}
          onClick={onNext}
          aria-label="Xem preview tiếp theo"
        >
          ›
        </button>
      </div>

      <div className={styles.coinRow}>
        {previewItems.map(({ item, index, offset }) => (
          <button
            key={`${item.id}-coin-${offset}`}
            type="button"
            className={`${styles.coinButton} ${Math.abs(offset) === 1 ? styles.coinInner : ''} ${
              offset === 0 ? `${styles.coinActive} ${styles.coinCenter}` : ''
            }`}
            onClick={() => onSelect(index)}
            aria-label={item.previewLabel}
          >
            <span className={styles.coin} />
          </button>
        ))}
      </div>

      <div className={styles.bottomPill} aria-hidden="true" />
    </motion.div>
  );
}
