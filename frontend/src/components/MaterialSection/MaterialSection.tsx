import { useState } from 'react';
import { motion, AnimatePresence, useReducedMotion } from 'framer-motion';
import styles from './MaterialSection.module.css';
import { coverReveal, easeOutQuart, sectionReveal, viewportOnce } from '../../utils/motion';

const materials = [
  {
    name: 'Vải lụa',
    description: 'Có độ mềm mại, rũ, tạo nên vẻ đẹp thướt tha, duyên dáng cho người mặc.',
    swatch: '/assets/material-swatch-1.svg',
    barLeft: 243,
  },
  {
    name: 'Lụa tơ tằm',
    description: 'Có hoa văn in giúp tôn lên những đường nét tinh tế trên tà áo dài.',
    swatch: '/assets/material-swatch-2.svg',
    barLeft: 535,
  },
  {
    name: 'Vải gấm',
    description: 'Có độ mềm mại, rũ, tạo nên vẻ đẹp thướt tha, duyên dáng cho người mặc.',
    swatch: '/assets/material-swatch-3.svg',
    barLeft: 829,
  },
];

const contentVariants = {
  initial: { opacity: 0, y: 24, filter: 'blur(8px)', clipPath: 'inset(0 0 28% 0)' },
  animate: { opacity: 1, y: 0, filter: 'blur(0px)', clipPath: 'inset(0 0 0% 0)' },
  exit: { opacity: 0, y: -18, filter: 'blur(6px)', clipPath: 'inset(24% 0 0 0)' },
};

export default function MaterialSection() {
  const [active, setActive] = useState(0);
  const prefersReducedMotion = useReducedMotion();

  return (
    <motion.section
      className={`red-texture ${styles.materialSection}`}
      aria-labelledby="material-title"
      variants={sectionReveal}
      initial="hidden"
      whileInView="show"
      viewport={viewportOnce}
    >
      <motion.div
        className={styles.materialCover}
        aria-hidden="true"
        variants={coverReveal}
      />

      <motion.div
        className={styles.textureSheen}
        aria-hidden="true"
        initial={{ opacity: 0 }}
        whileInView={prefersReducedMotion ? { opacity: 0.32 } : { opacity: 0.56, x: ['-18%', '14%', '-18%'] }}
        viewport={viewportOnce}
        transition={{ duration: 8.5, repeat: Infinity, ease: 'easeInOut' }}
      />

      <motion.div
        className={styles.silkRibbonTop}
        aria-hidden="true"
        initial={{ opacity: 0, x: -80 }}
        whileInView={prefersReducedMotion ? { opacity: 0.24, x: 0 } : { opacity: [0.18, 0.42, 0.18], x: [-40, 48, -40] }}
        viewport={viewportOnce}
        transition={{ duration: 7.2, repeat: Infinity, ease: 'easeInOut' }}
      />

      <motion.div
        className={styles.silkRibbonBottom}
        aria-hidden="true"
        initial={{ opacity: 0, x: 80 }}
        whileInView={prefersReducedMotion ? { opacity: 0.18, x: 0 } : { opacity: [0.14, 0.32, 0.14], x: [52, -44, 52] }}
        viewport={viewportOnce}
        transition={{ duration: 8.4, repeat: Infinity, ease: 'easeInOut' }}
      />

      {/* Full-width decorative vector overlay */}
      <div className={styles.overlay} aria-hidden="true" />

      {/* Blurred background texture */}
      <div className={styles.bgTexture} aria-hidden="true" />

      {/* Side gradient panels */}
      <div className={styles.panelLeft} aria-hidden="true" />
      <div className={styles.panelRight} aria-hidden="true" />

      {/* Title */}
      <div
        className={`${styles.scriptTitleWrap} script-title`}
        id="material-title"
      >
        Chất liệu
      </div>

      {/* Decorative ornament near title */}
      <div className={styles.ornament} aria-hidden="true" />

      {/* Material copy — animated on swap */}
      <AnimatePresence mode="wait">
        <motion.div
          key={active}
          className={styles.materialCopy}
          variants={contentVariants}
          initial="initial"
          animate="animate"
          exit="exit"
          transition={{ duration: 0.42, ease: easeOutQuart }}
        >
          <h3>{materials[active].name}</h3>
          <p>{materials[active].description}</p>
        </motion.div>
      </AnimatePresence>

      {/* Swatches */}
      <div className={styles.swatches} aria-label="Mẫu chất liệu">
        {materials.map((m, i) => (
          <motion.button
            key={i}
            onClick={() => setActive(i)}
            className={`${styles.swatch} ${active === i ? styles.active : ''}`}
            whileHover={prefersReducedMotion ? undefined : { scale: 1.08, rotate: i === 1 ? -3 : 3 }}
            whileTap={{ scale: 0.95 }}
            aria-label={m.name}
            aria-pressed={active === i}
          >
            {active === i ? (
              <motion.span
                className={styles.activeSwatchRing}
                layoutId="material-active-swatch"
                transition={{ type: 'spring', stiffness: 420, damping: 32 }}
              />
            ) : null}
            <img src={m.swatch} alt={m.name} width={145} height={145} />
          </motion.button>
        ))}
      </div>

      {/* Animated selection bar */}
      <motion.div
        className={styles.selectionBar}
        animate={{ left: materials[active].barLeft, scaleX: [0.72, 1.08, 1] }}
        transition={{ type: 'spring', stiffness: 300, damping: 30 }}
      >
        <motion.span
          className={styles.selectionBarShine}
          animate={prefersReducedMotion ? undefined : { x: ['-60%', '120%'] }}
          transition={{ duration: 1.4, repeat: Infinity, repeatDelay: 0.8, ease: 'easeInOut' }}
        />
      </motion.div>

      {/* Decorative circles */}
      <motion.div
        className={styles.circleMain}
        initial={{ opacity: 0, scale: 0.9 }}
        animate={prefersReducedMotion ? { opacity: 1, scale: 1 } : { opacity: 1, scale: [0.96, 1.02, 1], rotate: [0, 4, 0] }}
        transition={{ duration: 0.6, delay: 0.2 }}
        aria-hidden="true"
      />
      <motion.div
        className={styles.circleRotated}
        initial={{ opacity: 0, scale: 0.9 }}
        animate={prefersReducedMotion ? { opacity: 1, scale: 1 } : { opacity: 1, scale: [0.92, 1, 0.98], rotate: [-12, -8, -12] }}
        transition={{ duration: 0.6, delay: 0.4 }}
        aria-hidden="true"
      />
      <motion.div
        className={styles.circleBottom}
        initial={{ opacity: 0, scale: 0.9 }}
        animate={prefersReducedMotion ? { opacity: 1, scale: 1 } : { opacity: 1, scale: [0.9, 1.02, 1] }}
        transition={{ duration: 0.6, delay: 0.5 }}
        aria-hidden="true"
      />

      {/* Drum pattern */}
      <motion.img
        className={styles.drumPattern}
        src="/assets/drum-pattern.png"
        alt=""
        aria-hidden="true"
        animate={prefersReducedMotion ? undefined : { rotate: [0, 2, 0], scale: [1, 1.015, 1] }}
        transition={{ duration: 9, repeat: Infinity, ease: 'easeInOut' }}
      />
    </motion.section>
  );
}
