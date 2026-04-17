import { useState } from 'react';
import { AnimatePresence, motion, useReducedMotion } from 'framer-motion';
import styles from './AccessorySection.module.css';
import {
  carouselCopy,
  carouselCopyItem,
  fadeScale,
  fadeUp,
  imageParallaxHover,
  listStagger,
  sectionReveal,
  viewportOnce,
} from '../../utils/motion';

const ACCESSORIES = [
  {
    variant: 'Frame 322',
    id: 'bag',
    title: 'Túi xách',
    hero: '/assets/accessories/hero-bag.png',
    thumb: '/assets/accessories/thumb-bag.png',
    alt: 'Túi xách trắng phối hoa nổi',
  },
  {
    variant: 'Frame 323',
    id: 'shoes',
    title: 'Giày',
    hero: '/assets/accessories/hero-shoes.png',
    thumb: '/assets/accessories/thumb-shoes.png',
    alt: 'Giày trắng quai mảnh',
  },
  {
    variant: 'Frame 313',
    id: 'tram',
    title: 'Trâm cài tóc',
    hero: '/assets/accessories/hero-tram-figma.png',
    thumb: '/assets/accessories/thumb-tram.png',
    alt: 'Trâm cài tóc xanh ngọc',
  },
  {
    variant: 'Frame 324',
    id: 'fan',
    title: 'Quạt cầm tay',
    hero: '/assets/accessories/hero-fan.png',
    thumb: '/assets/accessories/thumb-fan.png',
    alt: 'Quạt cầm tay trắng',
  },
];

export default function AccessorySection() {
  const prefersReducedMotion = useReducedMotion();
  const [activeIndex, setActiveIndex] = useState(1);
  const activeAccessory = ACCESSORIES[activeIndex];

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
      <div className={styles.leftWash} aria-hidden="true" />
      <div className={styles.leftPattern} aria-hidden="true" />
      <div className={styles.largeGoldCircle} aria-hidden="true" />
      <div className={styles.sceneWrap} aria-hidden="true">
        <img className={styles.sceneBlur} src="/assets/accessories/garden-scene.png" alt="" />
        <img className={styles.sceneImage} src="/assets/accessories/garden-scene.png" alt="" />
      </div>
      <img className={styles.mountainLayer} src="/assets/accessories/mountain-scene.png" alt="" aria-hidden="true" />
      <img
        className={styles.rightPanel}
        src={`/assets/accessories/right-panel-${activeAccessory.id}.svg`}
        alt=""
        aria-hidden="true"
      />
      <div className={styles.centerPanel} aria-hidden="true" />
      <img className={styles.cornerLeft} src="/assets/accessories/corner-decor.png" alt="" aria-hidden="true" />
      <img className={styles.cornerRight} src="/assets/accessories/corner-decor.png" alt="" aria-hidden="true" />

      <motion.aside className={styles.infoPanel} variants={fadeUp}>
        <div className={styles.logoBadge} aria-hidden="true">
          <img src="/assets/footer-logo.png" alt="" />
        </div>
        <AnimatePresence mode="wait">
          <motion.div key={activeAccessory.id} variants={carouselCopy} initial="enter" animate="center" exit="exit">
            <motion.h2 id="accessory-title" className={styles.cardTitle} variants={carouselCopyItem}>
              {activeAccessory.title}
            </motion.h2>
          </motion.div>
        </AnimatePresence>
        <p className={styles.cardDesc}>
          Được chế tác từ lụa tơ tằm cao cấp và da thuộc thủ công, phụ kiện Nhã Uyên tôn vinh cốt
          cách thanh cao của phụ nữ Việt.
        </p>
      </motion.aside>

      <AnimatePresence mode="wait">
        <motion.div
          key={activeAccessory.id}
          className={`${styles.heroProduct} ${styles[`${activeAccessory.id}Product`]}`}
          variants={fadeScale}
          initial="hidden"
          animate="show"
          exit={{ opacity: 0, scale: 0.94, y: 18, filter: 'blur(6px)', transition: { duration: 0.24 } }}
        >
          <motion.img
            src={activeAccessory.hero}
            alt={activeAccessory.alt}
            whileHover={prefersReducedMotion ? undefined : imageParallaxHover.hover}
            transition={{ duration: 0.36, ease: 'easeOut' }}
          />
        </motion.div>
      </AnimatePresence>

      <motion.h3
        className={styles.label}
        variants={fadeUp}
        animate={prefersReducedMotion ? undefined : { y: [0, -7, 0] }}
        transition={{ duration: 5.4, repeat: Infinity, ease: 'easeInOut' }}
      >
        Phụ kiện
      </motion.h3>

      <motion.div className={styles.thumbnails} variants={listStagger} aria-label="Chọn phụ kiện">
        {ACCESSORIES.map((accessory, index) => (
          <motion.button
            key={accessory.id}
            type="button"
            className={`${styles.thumb} ${activeIndex === index ? styles.thumbActive : ''}`}
            onClick={() => setActiveIndex(index)}
            aria-pressed={activeIndex === index}
            aria-label={accessory.title}
            variants={fadeUp}
            whileHover={prefersReducedMotion ? undefined : { scale: 1.04 }}
            whileTap={{ scale: 0.97 }}
            transition={{ duration: 0.2 }}
          >
            <img src={accessory.thumb} alt="" />
            {activeIndex === index ? (
              <motion.span
                className={styles.thumbIndicator}
                layoutId="active-accessory-thumb"
                transition={{ type: 'spring', stiffness: 360, damping: 32 }}
              />
            ) : null}
          </motion.button>
        ))}
      </motion.div>

      <motion.div className={styles.goldLine} variants={fadeScale} aria-hidden="true" />
      <motion.div className={styles.goldDot} variants={fadeUp} aria-hidden="true" />
    </motion.section>
  );
}
