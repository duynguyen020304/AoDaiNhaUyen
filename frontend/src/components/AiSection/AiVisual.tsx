import { motion, useReducedMotion } from 'framer-motion';
import styles from './AiSection.module.css';
import { fadeScale } from '../../utils/motion';

export default function AiVisual() {
  const prefersReducedMotion = useReducedMotion();

  return (
    <motion.div className={styles.aiVisual} aria-hidden="true" variants={fadeScale}>
      <div className={styles.aiPhotoStage}>
        <img className={styles.aiBackdrop} src="/assets/ai-visual-backdrop.svg" alt="" />
        <motion.div
          className={styles.scanBeam}
          animate={prefersReducedMotion ? undefined : { x: [0, 84, 0], opacity: [0.28, 0.72, 0.28] }}
          transition={{ duration: 4.8, repeat: Infinity, ease: 'easeInOut' }}
        />
        <motion.div
          className={`${styles.transferDot} ${styles.transferDotOne}`}
          animate={prefersReducedMotion ? undefined : { x: [0, 210, 420], y: [0, 24, 88], opacity: [0, 1, 0] }}
          transition={{ duration: 3.6, repeat: Infinity, ease: 'easeInOut' }}
        />
        <motion.div
          className={`${styles.transferDot} ${styles.transferDotTwo}`}
          animate={prefersReducedMotion ? undefined : { x: [0, 188, 376], y: [0, -18, 42], opacity: [0, 1, 0] }}
          transition={{ duration: 3.6, repeat: Infinity, ease: 'easeInOut', delay: 0.8 }}
        />
        <motion.img
          className={styles.aiModel}
          src="/assets/ai-scene.png"
          alt=""
          animate={prefersReducedMotion ? undefined : { y: [0, -12, 0] }}
          transition={{ duration: 6.2, repeat: Infinity, ease: 'easeInOut' }}
        />
        <motion.img
          className={`${styles.aiCard} ${styles.cardYellow}`}
          src="/assets/ai-card-yellow.jpg"
          alt=""
          animate={prefersReducedMotion ? undefined : { rotate: [13, 10, 13], y: [0, -10, 0] }}
          transition={{ duration: 5.4, repeat: Infinity, ease: 'easeInOut' }}
        />
        <motion.img
          className={`${styles.aiCard} ${styles.cardBlue}`}
          src="/assets/ai-card-blue.jpg"
          alt=""
          animate={prefersReducedMotion ? undefined : { rotate: [-12.5, -9, -12.5], y: [0, 12, 0] }}
          transition={{ duration: 5.8, repeat: Infinity, ease: 'easeInOut' }}
        />
      </div>
    </motion.div>
  );
}
