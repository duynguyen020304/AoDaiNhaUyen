import { useState } from 'react';
import { AnimatePresence, motion, useReducedMotion } from 'framer-motion';
import styles from './AccessorySection.module.css';
import {
  cardHover,
  cardReveal,
  carouselCopy,
  carouselCopyItem,
  fadeScale,
  fadeUp,
  imageParallaxHover,
  listStagger,
  sectionReveal,
  viewportOnce,
} from '../../utils/motion';

const PRODUCTS = [
  { id: 'tram', title: 'Trâm cài tóc', hero: '/assets/accessories/hero-tram.png' },
  { id: 'quat', title: 'Quạt cầm tay', hero: '/assets/accessories/hero-quat.png' },
];

const THUMBNAILS = [
  { src: '/assets/accessories/thumb-1.png', productIndex: 0 },
  { src: '/assets/accessories/thumb-2.png', productIndex: 0 },
  { src: '/assets/accessories/thumb-3.png', productIndex: 1 },
  { src: '/assets/accessories/thumb-4.png', productIndex: 1 },
];

export default function AccessorySection() {
  const prefersReducedMotion = useReducedMotion();
  const [activeThumb, setActiveThumb] = useState(0);
  const activeProduct = THUMBNAILS[activeThumb].productIndex;
  const product = PRODUCTS[activeProduct];

  return (
    <motion.section
      className={styles.section}
      id="accessories"
      aria-labelledby="accessory-title"
      variants={sectionReveal}
      initial="hidden"
      whileInView="show"
      viewport={viewportOnce}
    >
      {/* Decorative background layers */}
      <div className={styles.leftPanel} />
      <div className={styles.dragonPattern} />
      <div className={styles.circleOverlay} />
      <div className={styles.subtractShape} />

      {/* Hero product image */}
      <AnimatePresence mode="wait">
        <motion.div
          key={product.id}
          className={`${styles.heroWrapper} ${activeProduct === 0 ? styles.heroLeft : styles.heroCenter}`}
          variants={fadeScale}
          initial="hidden"
          animate="show"
          exit={{ opacity: 0, scale: 0.96, transition: { duration: 0.28 } }}
        >
          <motion.img
            className={styles.heroImage}
            src={product.hero}
            alt={product.title}
            whileHover={prefersReducedMotion ? undefined : imageParallaxHover.hover}
            transition={{ duration: 0.36, ease: 'easeOut' }}
          />
        </motion.div>
      </AnimatePresence>

      {/* Product info card */}
      <motion.div
        className={styles.card}
        variants={cardReveal}
        whileHover={prefersReducedMotion ? undefined : cardHover.hover}
        transition={{ duration: 0.28, ease: 'easeOut' }}
      >
        <AnimatePresence mode="wait">
          <motion.div
            key={product.id}
            variants={carouselCopy}
            initial="enter"
            animate="center"
            exit="exit"
          >
            <motion.h2
              id="accessory-title"
              className={styles.cardTitle}
              variants={carouselCopyItem}
            >
              {product.title}
            </motion.h2>
          </motion.div>
        </AnimatePresence>
        <motion.p className={styles.cardDesc} variants={fadeUp}>
          Được chế tác từ lụa tơ tằm cao cấp và da thuộc thủ công, phụ kiện Nhã Uyên tôn vinh
          cốt cách thanh cao của phụ nữ Việt.
        </motion.p>
      </motion.div>

      {/* "Phụ kiện" floating label */}
      <motion.h3
        className={styles.label}
        variants={fadeUp}
        animate={prefersReducedMotion ? undefined : { y: [0, -8, 0] }}
        transition={{ duration: 5.6, repeat: Infinity, ease: 'easeInOut' }}
      >
        Phụ kiện
      </motion.h3>

      {/* Thumbnail gallery */}
      <motion.div className={styles.thumbnails} variants={listStagger}>
        {THUMBNAILS.map((thumb, i) => (
          <motion.button
            key={i}
            className={`${styles.thumb} ${activeThumb === i ? styles.thumbActive : ''}`}
            variants={fadeUp}
            onClick={() => setActiveThumb(i)}
            whileHover={prefersReducedMotion ? undefined : { scale: 1.05 }}
            transition={{ duration: 0.2 }}
          >
            <img src={thumb.src} alt={`Phụ kiện ${i + 1}`} />
            {activeThumb === i && (
              <motion.div className={styles.thumbIndicator} layoutId="activeThumb" />
            )}
          </motion.button>
        ))}
      </motion.div>

      {/* Decorative gold line + dot */}
      <motion.div
        className={styles.goldLine}
        variants={{
          hidden: { scaleY: 0, transformOrigin: 'top center' },
          show: { scaleY: 1, transition: { duration: 0.9, ease: [0.22, 1, 0.36, 1] } },
        }}
      />
      <motion.div className={styles.goldDot} variants={fadeUp} />
    </motion.section>
  );
}
