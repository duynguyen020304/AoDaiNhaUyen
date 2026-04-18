import { motion } from 'framer-motion';
import styles from './CollectionSection.module.css';
import { fadeUp, staggerContainer } from '../../utils/motion';

type CategoryPillsProps = {
  categories: string[];
  selected: number;
  onSelect: (index: number) => void;
};

export default function CategoryPills({ categories, selected, onSelect }: CategoryPillsProps) {
  return (
    <motion.div className={styles.categoryPills} aria-label="Danh mục áo dài" variants={staggerContainer}>
      {categories.map((cat, i) => (
        <motion.button
          key={cat}
          className={`${styles.categoryButton} ${i === selected ? styles.selected : ''}`}
          type="button"
          onClick={() => onSelect(i)}
          aria-pressed={i === selected}
          variants={fadeUp}
          whileHover={{ y: -2 }}
          whileTap={{ scale: 0.97 }}
          transition={{ duration: 0.24, ease: 'easeOut' }}
        >
          {i === selected ? (
            <motion.span
              className={styles.selectedPill}
              layoutId="collection-selected-pill"
              transition={{ type: 'spring', stiffness: 420, damping: 34 }}
            />
          ) : null}
          <span>{cat}</span>
        </motion.button>
      ))}
    </motion.div>
  );
}
