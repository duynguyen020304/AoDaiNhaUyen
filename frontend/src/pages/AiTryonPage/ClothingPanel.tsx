import { motion } from 'framer-motion';
import { listStagger, cardReveal } from '../../utils/motion';
import CategoryTabs from './CategoryTabs';
import PaginationControls from './PaginationControls';
import type { AiTryOnCatalogCategory, AiTryOnCatalogItem, AiTryOnCatalogPage } from '../../api/aiTryon';
import { resolveAssetUrl } from '../../api/client';
import styles from './ClothingPanel.module.css';

interface ClothingPanelProps {
  selectedCategory: string;
  selectedGarment: number | null;
  garments: AiTryOnCatalogItem[];
  garmentPage: AiTryOnCatalogPage;
  categories: AiTryOnCatalogCategory[];
  onCategoryChange: (key: string) => void;
  onPageChange: (page: number) => void;
  onSelectGarment: (item: AiTryOnCatalogItem) => void;
}

export default function ClothingPanel({
  selectedCategory,
  selectedGarment,
  garments,
  garmentPage,
  categories,
  onCategoryChange,
  onPageChange,
  onSelectGarment,
}: ClothingPanelProps) {
  return (
    <div className={styles.panel}>
      <div className={styles.stepHeader}>
        <span className={styles.stepBadge}>2</span>
        <h2>CHỌN TRANG PHỤC</h2>
      </div>

      <CategoryTabs
        categories={categories}
        selected={selectedCategory}
        onChange={onCategoryChange}
        layoutId="ai-garment-category-pill"
      />

      <motion.div
        className={styles.grid}
        variants={listStagger}
        initial="hidden"
        animate="show"
        key={`${selectedCategory}-${garmentPage.page}`}
      >
        {garments.map((item) => {
          const isSelected = selectedGarment === item.productId;
          return (
            <motion.button
              key={item.productId}
              className={`${styles.card} ${isSelected ? styles.selected : ''}`}
              type="button"
              onClick={() => onSelectGarment(item)}
              variants={cardReveal}
              layout
            >
              <div className={styles.cardImage}>
                <img src={resolveAssetUrl(item.thumbnailUrl) ?? item.thumbnailUrl} alt={item.name} />
                <div className={styles.cardOverlay} />
              </div>
              <span className={styles.cardName}>{item.name}</span>
            </motion.button>
          );
        })}
      </motion.div>

      <PaginationControls
        page={garmentPage.page}
        pageSize={garmentPage.pageSize}
        totalItems={garmentPage.totalItems}
        totalPages={garmentPage.totalPages}
        onPageChange={onPageChange}
      />
    </div>
  );
}
