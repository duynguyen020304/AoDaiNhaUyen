import { useMemo } from 'react';
import { motion } from 'framer-motion';
import { listStagger, cardReveal } from '../../utils/motion';
import CategoryTabs from './CategoryTabs';
import { GARMENT_CATEGORIES, GARMENTS, BESTSELLER_IDS } from './data';
import styles from './ClothingPanel.module.css';

interface ClothingPanelProps {
  selectedCategory: string;
  selectedGarment: string | null;
  onCategoryChange: (key: string) => void;
  onSelectGarment: (id: string) => void;
}

export default function ClothingPanel({
  selectedCategory,
  selectedGarment,
  onCategoryChange,
  onSelectGarment,
}: ClothingPanelProps) {
  const filteredGarments = useMemo(() => {
    if (selectedCategory === 'all') return GARMENTS;
    if (selectedCategory === 'bestseller') return GARMENTS.filter((g) => BESTSELLER_IDS.includes(g.id));
    return GARMENTS.filter((g) => g.category === selectedCategory);
  }, [selectedCategory]);

  return (
    <div className={styles.panel}>
      <div className={styles.stepHeader}>
        <span className={styles.stepBadge}>2</span>
        <h2>CHỌN TRANG PHỤC</h2>
      </div>

      <CategoryTabs
        categories={GARMENT_CATEGORIES}
        selected={selectedCategory}
        onChange={onCategoryChange}
      />

      <motion.div
        className={styles.grid}
        variants={listStagger}
        initial="hidden"
        animate="show"
        key={selectedCategory}
      >
        {filteredGarments.map((item) => {
          const isSelected = selectedGarment === item.id;
          return (
            <motion.button
              key={item.id}
              className={`${styles.card} ${isSelected ? styles.selected : ''}`}
              type="button"
              onClick={() => onSelectGarment(item.id)}
              variants={cardReveal}
              layout
            >
              <div className={styles.cardImage}>
                <img src={item.thumbnail} alt={item.name} />
                <div className={styles.cardOverlay} />
              </div>
              <span className={styles.cardName}>{item.name}</span>
            </motion.button>
          );
        })}
      </motion.div>
    </div>
  );
}
