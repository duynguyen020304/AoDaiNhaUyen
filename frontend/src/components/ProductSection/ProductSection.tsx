import { useState } from 'react';
import { motion, useReducedMotion } from 'framer-motion';
import styles from './ProductSection.module.css';
import { sectionReveal, viewportOnce } from '../../utils/motion';
import ProductSectionCopy from './ProductSectionCopy';
import ProductSectionHero from './ProductSectionHero';
import ProductSectionPreview from './ProductSectionPreview';
import { productSectionSlides } from './productSectionData';

const goldFlower = '/assets/gold-flower.svg';
const redFloral = '/assets/red-floral.svg';

export default function ProductSection() {
  const [active, setActive] = useState(2);
  const prefersReducedMotion = useReducedMotion();
  const activeSlide = productSectionSlides[active];

  const previous = () =>
    setActive((current) => (current - 1 + productSectionSlides.length) % productSectionSlides.length);
  const next = () => setActive((current) => (current + 1) % productSectionSlides.length);

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
      <div className={styles.rightBackground} aria-hidden="true">
        <img
          className={styles.drumPattern}
          src="/assets/drum-pattern.svg"
          alt=""
          aria-hidden="true"
        />
      </div>
      <div className={styles.noiseWash} aria-hidden="true" />
      <img className={styles.patternTextureTop} src="/assets/drum-pattern.svg" alt="" aria-hidden="true" />
      <img className={styles.patternTextureBottom} src="/assets/drum-pattern.svg" alt="" aria-hidden="true" />
      <img className={styles.goldFlower} src={goldFlower} alt="" aria-hidden="true" />
      <img className={styles.bottomFloral} src={redFloral} alt="" aria-hidden="true" />

      <div className={styles.contentGrid}>
        <ProductSectionCopy slide={activeSlide} />
        <ProductSectionHero
          active={active}
          prefersReducedMotion={prefersReducedMotion}
          slide={activeSlide}
          slides={productSectionSlides}
          onNext={next}
          onPrevious={previous}
          onSelect={setActive}
        />
        <ProductSectionPreview
          active={active}
          slide={activeSlide}
          slides={productSectionSlides}
          onNext={next}
          onPrevious={previous}
          onSelect={setActive}
        />
      </div>
    </motion.section>
  );
}
