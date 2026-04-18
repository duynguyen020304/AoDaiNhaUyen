import { motion } from 'framer-motion';
import styles from './CollectionHero.module.css';
import { fadeScale } from '../../utils/motion';

export default function CollectionHero() {
  return (
    <section className={styles.hero}>
      <motion.video
        className={styles.heroMedia}
        src="/assets/collection/banner 3.mp4"
        autoPlay
        loop
        muted
        playsInline
        preload="metadata"
        aria-hidden="true"
        variants={fadeScale}
        initial="hidden"
        animate="show"
      />
      <div className={styles.gradient} />
      <div className={styles.separator} />
    </section>
  );
}
