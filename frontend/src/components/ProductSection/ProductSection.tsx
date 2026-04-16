import { useState } from 'react';
import { AnimatePresence, motion, useReducedMotion } from 'framer-motion';
import styles from './ProductSection.module.css';
import {
  cardReveal,
  carouselCopy,
  carouselCopyItem,
  easeOutQuart,
  fadeScale,
  listStagger,
  sectionReveal,
  viewportOnce,
} from '../../utils/motion';

const topFlourish = '/assets/top-flourish.png';
const sparkle = '/assets/sparkle.png';
const goldFlower = '/assets/gold-flower.png';

const looks = [
  {
    name: 'Áo dài thêu hoa',
    hero: '/assets/product-model.png',
    preview: '/assets/dress-panel.png',
    thumbnail: '/assets/product-model.png',
  },
  {
    name: 'Áo dài hồng phấn',
    hero: '/assets/dress-pink.png',
    preview: '/assets/dress-pink.png',
    thumbnail: '/assets/dress-pink.png',
  },
  {
    name: 'Áo dài ngọc lục',
    hero: '/assets/dress-green.png',
    preview: '/assets/dress-green.png',
    thumbnail: '/assets/dress-green.png',
  },
  {
    name: 'Áo dài trắng ngà',
    hero: '/assets/dress-white.png',
    preview: '/assets/dress-white.png',
    thumbnail: '/assets/dress-white.png',
  },
  {
    name: 'Cận cảnh thêu tay',
    hero: '/assets/dress-panel.png',
    preview: '/assets/dress-panel.png',
    thumbnail: '/assets/dress-panel.png',
  },
] as const;

const detailLines = [
  'Phom áo dài cách tân giữ sự mềm mại nhưng thoáng, giúp tà áo rơi thẳng và gọn.',
  'Nền lụa ánh hồng được phủ bằng họa tiết thêu hoa tỉ mỉ để tạo chiều sâu sang trọng.',
  'Cổ áo thấp và tay lửng giữ tinh thần truyền thống, đồng thời dễ mặc trong những dịp hiện đại.',
  'Bảng phối màu vàng kim, đỏ rượu và hồng phấn được dùng xuyên suốt để đồng bộ với tinh thần Figma.',
] as const;

const previewSlides = [
  {
    name: 'Khung phong cảnh thêu hoa',
    image: '/assets/dress-panel.png',
    thumbnail: '/assets/dress-panel.png',
  },
  {
    name: 'Biến thể hồng phấn',
    image: '/assets/dress-pink.png',
    thumbnail: '/assets/dress-pink.png',
  },
  {
    name: 'Biến thể ngọc lục',
    image: '/assets/dress-green.png',
    thumbnail: '/assets/dress-green.png',
  },
  {
    name: 'Biến thể trắng ngà',
    image: '/assets/dress-white.png',
    thumbnail: '/assets/dress-white.png',
  },
  {
    name: 'Mẫu phối toàn thân',
    image: '/assets/product-model.png',
    thumbnail: '/assets/product-model.png',
  },
] as const;

export default function ProductSection() {
  const [active, setActive] = useState(0);
  const [previewActive, setPreviewActive] = useState(0);
  const prefersReducedMotion = useReducedMotion();

  const previous = () => setActive((current) => (current - 1 + looks.length) % looks.length);
  const next = () => setActive((current) => (current + 1) % looks.length);
  const previousPreview = () =>
    setPreviewActive((current) => (current - 1 + previewSlides.length) % previewSlides.length);
  const nextPreview = () => setPreviewActive((current) => (current + 1) % previewSlides.length);

  return (
    <motion.section
      className={styles.productSection}
      id="product"
      aria-labelledby="product-title"
      variants={sectionReveal}
      initial="hidden"
      whileInView="show"
      viewport={viewportOnce}
    >
      <div className={styles.leftBackground} aria-hidden="true" />
      <div className={styles.middleBackground} aria-hidden="true" />
      <div className={styles.rightBackground} aria-hidden="true" />
      <div className={styles.noiseWash} aria-hidden="true" />
      <img className={styles.patternTextureTop} src="/assets/drum-pattern.png" alt="" aria-hidden="true" />
      <img className={styles.patternTextureBottom} src="/assets/drum-pattern.png" alt="" aria-hidden="true" />
      <img className={styles.goldFlower} src={goldFlower} alt="" aria-hidden="true" />
      <img className={styles.bottomFloral} src="/assets/red-floral.png" alt="" aria-hidden="true" />

      <div className={styles.contentGrid}>
        <motion.div className={styles.copyColumn} variants={carouselCopy} animate="center" initial="enter">
          <img className={styles.copyLine} src={topFlourish} alt="" aria-hidden="true" />
          <motion.p className={styles.eyebrow} variants={carouselCopyItem}>
            Áo Dài Nhã Uyên
          </motion.p>
          <motion.h2 id="product-title" variants={carouselCopyItem}>
            ÁO DÀI THÊU HOA
          </motion.h2>

          <motion.div className={styles.detailPanel} variants={carouselCopyItem}>
            {detailLines.map((line) => (
              <p key={line}>{line}</p>
            ))}
          </motion.div>
        </motion.div>

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
                key={looks[active].hero}
                className={styles.heroImage}
                src={looks[active].hero}
                alt={looks[active].name}
                initial={{ opacity: 0, y: 18, scale: 1.02 }}
                animate={{ opacity: 1, y: 0, scale: 1 }}
                exit={{ opacity: 0, y: -18, scale: 0.98 }}
                transition={{ duration: 0.38, ease: easeOutQuart }}
              />
            </AnimatePresence>
          </div>

          <button type="button" className={styles.arrowLeft} onClick={previous} aria-label="Xem mẫu trước">
            ‹
          </button>
          <button type="button" className={styles.arrowRight} onClick={next} aria-label="Xem mẫu tiếp theo">
            ›
          </button>

          <div className={styles.heroDots} aria-label="Chọn mẫu" role="tablist">
            {looks.slice(0, 4).map((look, index) => (
              <button
                key={look.name}
                type="button"
                role="tab"
                aria-selected={active === index}
                aria-label={look.name}
                className={`${styles.heroDot} ${active === index ? styles.heroDotActive : ''}`}
                onClick={() => setActive(index)}
              />
            ))}
          </div>

          <div className={styles.titlePill}>
            <span>ÁO DÀI THÊU HOA</span>
          </div>
        </motion.div>

        <motion.div className={styles.previewColumn} variants={listStagger}>
          <img className={styles.previewFlourish} src={topFlourish} alt="" aria-hidden="true" />

          <div className={styles.previewStack}>
            <motion.div className={styles.previewShadowFrame} variants={cardReveal} aria-hidden="true" />
            <motion.div className={styles.previewFrame} variants={cardReveal}>
              <AnimatePresence mode="wait">
                <motion.img
                  key={previewSlides[previewActive].image}
                  className={styles.previewImage}
                  src={previewSlides[previewActive].image}
                  alt={`${previewSlides[previewActive].name} xem trước`}
                  initial={{ opacity: 0, scale: 1.03 }}
                  animate={{ opacity: 1, scale: 1 }}
                  exit={{ opacity: 0, scale: 0.98 }}
                  transition={{ duration: 0.34, ease: easeOutQuart }}
                />
              </AnimatePresence>
              <img className={styles.previewSparkle} src={sparkle} alt="" aria-hidden="true" />
            </motion.div>
          </div>

          <div className={styles.previewIndicatorRail} aria-hidden="true">
            {previewSlides.slice(0, 3).map((slide, index) => (
              <span
                key={slide.name}
                className={`${styles.previewIndicator} ${previewActive === index ? styles.previewIndicatorActive : ''}`}
              />
            ))}
          </div>

          <div className={styles.previewControls}>
            <button
              type="button"
              className={styles.previewArrowLeft}
              onClick={previousPreview}
              aria-label="Xem ảnh preview trước"
            >
              ‹
            </button>

            <div className={styles.thumbnailRail}>
              {previewSlides.map((slide, index) => (
                <button
                  key={slide.name}
                  type="button"
                  className={`${styles.thumbnailButton} ${previewActive === index ? styles.thumbnailActive : ''}`}
                  onClick={() => setPreviewActive(index)}
                  aria-label={slide.name}
                >
                  <img src={slide.thumbnail} alt="" aria-hidden="true" />
                </button>
              ))}
            </div>

            <button
              type="button"
              className={styles.previewArrowRight}
              onClick={nextPreview}
              aria-label="Xem ảnh preview tiếp theo"
            >
              ›
            </button>
          </div>

          <div className={styles.coinRow} aria-hidden="true">
            {previewSlides.map((slide, index) => (
              <button
                key={slide.name}
                type="button"
                className={`${styles.coinButton} ${previewActive === index ? styles.coinActive : ''}`}
                onClick={() => setPreviewActive(index)}
                aria-label={slide.name}
              >
                <span className={styles.coin} />
              </button>
            ))}
          </div>

          <div className={styles.bottomPill} aria-hidden="true" />
        </motion.div>
      </div>
    </motion.section>
  );
}
