import { motion } from 'framer-motion';
import styles from './CollectionHero.module.css';
import { fadeScale } from '../../utils/motion';
import { IMG } from './data';

export default function CollectionHero() {
  return (
    <section className={styles.hero}>
      <motion.img
        className={styles.heroImg}
        src={IMG.hero}
        alt=""
        variants={fadeScale}
        initial="hidden"
        animate="show"
      />
      <div className={styles.gradient} />
      <div className={styles.separator} />
    </section>
  );
}
