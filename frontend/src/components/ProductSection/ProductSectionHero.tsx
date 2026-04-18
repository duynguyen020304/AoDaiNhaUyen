import { AnimatePresence, motion } from 'framer-motion';
import styles from './ProductSection.module.css';
import { easeOutQuart, fadeScale } from '../../utils/motion';
import type { ProductSectionSlide } from './productSectionData';

type ProductSectionHeroProps = {
  active: number;
  prefersReducedMotion: boolean | null;
  slide: ProductSectionSlide;
  slides: ProductSectionSlide[];
  onNext: () => void;
  onPrevious: () => void;
  onSelect: (index: number) => void;
};

export default function ProductSectionHero({
  prefersReducedMotion,
  slide,
}: ProductSectionHeroProps) {
  return (
    <motion.div className={styles.heroColumn} variants={fadeScale}>
      <motion.img
        className={styles.heroDragon}
        src="/assets/dragon.png"
        alt=""
        aria-hidden="true"
        animate={
          prefersReducedMotion
            ? { opacity: 0.12 }
            : { opacity: [0.08, 0.16, 0.08], rotate: [17.52, 20, 17.52] }
        }
        transition={{ duration: 8, repeat: Infinity, ease: 'easeInOut' }}
      />

      <div className={styles.archFrame}>
        <img className={styles.archBackdrop} src="/assets/dress-panel.png" alt="" aria-hidden="true" />

        <AnimatePresence mode="wait">
          <motion.img
            key={slide.heroImage}
            className={styles.heroImage}
            src={slide.heroImage}
            alt={slide.heroAlt}
            initial={{ opacity: 0, y: 18, scale: 1.02 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: -18, scale: 0.98 }}
            transition={{ duration: 0.38, ease: easeOutQuart }}
          />
        </AnimatePresence>
      </div>

      <div className={styles.titlePill}>
        <AnimatePresence mode="wait">
          <motion.span
            key={slide.id}
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -10 }}
            transition={{ duration: 0.28, ease: easeOutQuart }}
          >
            {slide.title}
          </motion.span>
        </AnimatePresence>
      </div>
    </motion.div>
  );
}
