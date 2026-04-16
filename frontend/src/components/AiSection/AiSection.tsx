import { motion } from 'framer-motion';
import styles from './AiSection.module.css';
import AiCopy from './AiCopy';
import AiVisual from './AiVisual';
import { sectionReveal, viewportOnce } from '../../utils/motion';

export default function AiSection() {
  return (
    <motion.section
      className={styles.aiSection}
      id="ai"
      aria-labelledby="ai-title"
      variants={sectionReveal}
      initial="hidden"
      whileInView="show"
      viewport={viewportOnce}
    >
      <AiCopy />
      <AiVisual />
      <img className={styles.aiWave} src="/assets/ai-wave.svg" alt="" aria-hidden="true" />
    </motion.section>
  );
}
