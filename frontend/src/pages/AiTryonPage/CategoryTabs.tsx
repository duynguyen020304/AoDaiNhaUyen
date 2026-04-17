import { motion } from 'framer-motion';
import { fadeUp, staggerContainer } from '../../utils/motion';
import type { GarmentCategory } from './data';
import styles from './CategoryTabs.module.css';

interface CategoryTabsProps {
  categories: GarmentCategory[];
  selected: string;
  onChange: (key: string) => void;
}

export default function CategoryTabs({ categories, selected, onChange }: CategoryTabsProps) {
  return (
    <motion.div className={styles.tabs} variants={staggerContainer} initial="hidden" animate="show">
      {categories.map((cat) => (
        <motion.button
          key={cat.key}
          className={`${styles.tab} ${cat.key === selected ? styles.active : ''}`}
          type="button"
          onClick={() => onChange(cat.key)}
          variants={fadeUp}
          whileHover={{ y: -2 }}
          whileTap={{ scale: 0.97 }}
        >
          {cat.key === selected && (
            <motion.span className={styles.activePill} layoutId="ai-category-pill" />
          )}
          <span className={styles.label}>{cat.label}</span>
        </motion.button>
      ))}
    </motion.div>
  );
}
