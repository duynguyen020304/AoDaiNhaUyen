import { AnimatePresence, motion } from 'framer-motion';
import styles from './ProductSection.module.css';
import { cardReveal, easeOutQuart, listStagger } from '../../utils/motion';
import type { ProductSectionSlide } from './productSectionData';

const topFlourish = '/assets/top-flourish.png';

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
  const previousIndex = (active - 1 + slides.length) % slides.length;
  const nextIndex = (active + 1) % slides.length;
  const hiddenIndex = slides.findIndex((_, index) => ![previousIndex, active, nextIndex].includes(index));
  const repeatedIndex = hiddenIndex === -1 ? active : hiddenIndex;

  const previewItems = [
    { index: repeatedIndex, item: slides[repeatedIndex], slot: 0 },
    { index: previousIndex, item: slides[previousIndex], slot: 1 },
    { index: active, item: slides[active], slot: 2 },
    { index: nextIndex, item: slides[nextIndex], slot: 3 },
    { index: repeatedIndex, item: slides[repeatedIndex], slot: 4 },
  ];

  const getThumbnailPositionClass = (slot: number) => {
    if (slot === 2) {
      return styles.thumbnailCenter;
    }

    return slot === 1 || slot === 3 ? styles.thumbnailInner : styles.thumbnailOuter;
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
          {previewItems.map(({ item, index, slot }) => (
            <button
              key={`${item.id}-${slot}`}
              type="button"
              className={`${styles.thumbnailButton} ${getThumbnailPositionClass(slot)} ${
                slot === 2 ? styles.thumbnailActive : ''
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
        {previewItems.map(({ item, index, slot }) => (
          <button
            key={`${item.id}-coin-${slot}`}
            type="button"
            className={`${styles.coinButton} ${slot === 1 || slot === 3 ? styles.coinInner : ''} ${
              slot === 2 ? `${styles.coinActive} ${styles.coinCenter}` : ''
            }`}
            onClick={() => onSelect(index)}
            aria-label={item.previewLabel}
          >
            <span className={styles.coin} />
          </button>
        ))}
      </div>

      <img className={styles.coinRowBottom} src="/assets/coinrow-bottom.png" alt="" aria-hidden="true" />
    </motion.div>
  );
}
