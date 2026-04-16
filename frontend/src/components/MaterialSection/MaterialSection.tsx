import { useState } from 'react';
import { AnimatePresence, motion, useReducedMotion } from 'framer-motion';
import styles from './MaterialSection.module.css';
import { easeOutQuart, sectionReveal, viewportOnce } from '../../utils/motion';

const materials = [
  {
    name: 'Vải lụa',
    description: 'Có độ mềm mại, rũ, tạo nên vẻ đẹp thướt tha, duyên dáng cho người mặc.',
    thumbnail: '/assets/material-preview-1-thumb.png',
    preview: '/assets/material-preview-1.png',
  },
  {
    name: 'Lụa hoa',
    description: 'Bề mặt có họa tiết mềm và độ óng nhẹ, tạo chiều sâu tinh tế cho tà áo dài.',
    thumbnail: '/assets/material-preview-2-thumb.png',
    preview: '/assets/material-preview-2-thumb.png',
  },
  {
    name: 'Lụa phối màu',
    description: 'Chất vải mềm, bắt sáng tốt và làm nổi bật các gam màu rực rỡ trên từng nếp gấp.',
    thumbnail: '/assets/material-preview-3-thumb.png',
    preview: '/assets/material-preview-3-thumb.png',
  },
] as const;

const copyVariants = {
  initial: { opacity: 0, y: 24, filter: 'blur(8px)' },
  animate: { opacity: 1, y: 0, filter: 'blur(0px)' },
  exit: { opacity: 0, y: -18, filter: 'blur(6px)' },
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
      <div className={styles.backgroundBase} aria-hidden="true" />
      <div className={styles.backgroundSweep} aria-hidden="true" />
      <div className={styles.floralTexture} aria-hidden="true" />
      <div className={styles.leftGlow} aria-hidden="true" />
      <div className={styles.rightGlow} aria-hidden="true" />

      <motion.div
        className={styles.sheen}
        aria-hidden="true"
        animate={
          prefersReducedMotion
            ? { opacity: 0.22 }
            : { opacity: [0.12, 0.26, 0.12], x: ['-14%', '8%', '-14%'] }
        }
        transition={{ duration: 8.5, repeat: Infinity, ease: 'easeInOut' }}
      />

      <header className={styles.heading}>
        <h2 className="script-title" id="material-title">
          Chất liệu
        </h2>
        <img
          className={styles.flourish}
          src="/assets/material-title-flourish.svg"
          alt=""
          aria-hidden="true"
        />
      </header>

      <div className={styles.contentGrid}>
        <div className={styles.copyColumn}>
          <AnimatePresence mode="wait">
            <motion.div
              key={materials[active].name}
              className={styles.materialCopy}
              variants={copyVariants}
              initial="initial"
              animate="animate"
              exit="exit"
              transition={{ duration: 0.42, ease: easeOutQuart }}
            >
              <h3>{materials[active].name}</h3>
              <p>{materials[active].description}</p>
            </motion.div>
          </AnimatePresence>

          <div className={styles.swatchRail}>
            <div className={styles.swatches} aria-label="Mẫu chất liệu" role="tablist">
              {materials.map((material, index) => (
                <button
                  key={material.name}
                  type="button"
                  role="tab"
                  aria-selected={active === index}
                  aria-label={material.name}
                  className={`${styles.swatch} ${active === index ? styles.active : ''}`}
                  onClick={() => setActive(index)}
                >
                  <img src={material.thumbnail} alt="" aria-hidden="true" />
                </button>
              ))}
            </div>

            <motion.span
              className={styles.selectionBar}
              aria-hidden="true"
              animate={{ x: `${active * 100}%` }}
              transition={{ type: 'spring', stiffness: 360, damping: 32 }}
            />
          </div>
        </div>

        <motion.figure
          className={styles.previewFrame}
          initial={{ opacity: 0, scale: 0.96 }}
          whileInView={{ opacity: 1, scale: 1 }}
          viewport={viewportOnce}
          transition={{ duration: 0.5, ease: easeOutQuart, delay: 0.12 }}
        >
          <div className={styles.previewHalo} aria-hidden="true" />
          <AnimatePresence mode="wait">
            <motion.img
              key={materials[active].preview}
              className={styles.previewImage}
              src={materials[active].preview}
              alt={materials[active].name}
              initial={{ opacity: 0, scale: 1.04 }}
              animate={{ opacity: 1, scale: 1 }}
              exit={{ opacity: 0, scale: 0.96 }}
              transition={{ duration: 0.38, ease: easeOutQuart }}
            />
          </AnimatePresence>
        </motion.figure>
      </div>

      <img
        className={styles.drumPattern}
        src="/assets/drum-pattern.png"
        alt=""
        aria-hidden="true"
      />
    </motion.section>
  );
}
