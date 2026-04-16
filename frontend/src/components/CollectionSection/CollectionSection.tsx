import { motion } from 'framer-motion';
import styles from './CollectionSection.module.css';
import CategoryPills from './CategoryPills';
import DressShowcase from './DressShowcase';
import { fadeScale, fadeUp, sectionReveal, viewportOnce } from '../../utils/motion';

export default function CollectionSection() {
  return (
    <motion.section
      className={`red-texture ${styles.collectionSection}`}
      id="collection"
      aria-labelledby="collection-title"
      variants={sectionReveal}
      initial="hidden"
      whileInView="show"
      viewport={viewportOnce}
    >
      <motion.div className={styles.goldStar} aria-hidden="true" variants={fadeScale} />
      <motion.h2 className="script-title" id="collection-title" variants={fadeUp}>
        Bộ sưu tập
      </motion.h2>
      <CategoryPills />
      <DressShowcase />
    </motion.section>
  );
}
