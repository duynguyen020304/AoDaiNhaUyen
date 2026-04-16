import { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import styles from './MaterialSection.module.css';

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
  initial: { opacity: 0, y: 20 },
  animate: { opacity: 1, y: 0 },
  exit: { opacity: 0, y: -20 },
};

export default function MaterialSection() {
  const [active, setActive] = useState(0);

  return (
    <section
      className={`red-texture ${styles.materialSection}`}
      aria-labelledby="material-title"
    >
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
          transition={{ duration: 0.3, ease: 'easeOut' }}
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
            whileHover={{ scale: 1.08 }}
            whileTap={{ scale: 0.95 }}
            aria-label={m.name}
            aria-pressed={active === i}
          >
            <img src={m.swatch} alt={m.name} width={145} height={145} />
          </motion.button>
        ))}
      </div>

      {/* Animated selection bar */}
      <motion.div
        className={styles.selectionBar}
        animate={{ left: materials[active].barLeft }}
        transition={{ type: 'spring', stiffness: 300, damping: 30 }}
      />

      {/* Decorative circles */}
      <motion.div
        className={styles.circleMain}
        initial={{ opacity: 0, scale: 0.9 }}
        animate={{ opacity: 1, scale: 1 }}
        transition={{ duration: 0.6, delay: 0.2 }}
        aria-hidden="true"
      />
      <motion.div
        className={styles.circleRotated}
        initial={{ opacity: 0, scale: 0.9 }}
        animate={{ opacity: 1, scale: 1 }}
        transition={{ duration: 0.6, delay: 0.4 }}
        aria-hidden="true"
      />
      <motion.div
        className={styles.circleBottom}
        initial={{ opacity: 0, scale: 0.9 }}
        animate={{ opacity: 1, scale: 1 }}
        transition={{ duration: 0.6, delay: 0.5 }}
        aria-hidden="true"
      />

      {/* Drum pattern */}
      <img
        className={styles.drumPattern}
        src="/assets/drum-pattern.png"
        alt=""
        aria-hidden="true"
      />
    </section>
  );
}
