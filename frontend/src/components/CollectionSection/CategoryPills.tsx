import { useState } from 'react';
import { motion } from 'framer-motion';
import styles from './CollectionSection.module.css';
import { fadeUp, staggerContainer } from '../../utils/motion';

const categories = [
  'Áo dài thêu hoa',
  'Áo dài cách tân',
  'Áo dài lụa trơn',
  'Áo dài truyền thống',
];

export default function CategoryPills() {
  const [selected, setSelected] = useState(0);

  return (
    <motion.div className={styles.categoryPills} aria-label="Danh mục áo dài" variants={staggerContainer}>
      {categories.map((cat, i) => (
        <motion.button
          key={cat}
          className={`${styles.categoryButton} ${i === selected ? styles.selected : ''}`}
          type="button"
          onClick={() => setSelected(i)}
          variants={fadeUp}
          whileHover={{ y: -2 }}
          whileTap={{ scale: 0.97 }}
        >
          {i === selected ? <motion.span className={styles.selectedPill} layoutId="category-selected-pill" /> : null}
          <span>{cat}</span>
        </motion.button>
      ))}
    </motion.div>
  );
}
