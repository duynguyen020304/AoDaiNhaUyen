import { motion } from 'framer-motion';
import { listStagger, fadeUp } from '../../utils/motion';
import CategoryTabs from './CategoryTabs';
import PaginationControls from './PaginationControls';
import type { AiTryOnCatalogCategory, AiTryOnCatalogItem, AiTryOnCatalogPage } from '../../api/aiTryon';
import { resolveAssetUrl } from '../../api/client';
import styles from './AccessoryPanel.module.css';

interface AccessoryPanelProps {
  accessories: AiTryOnCatalogItem[];
  accessoryPage: AiTryOnCatalogPage;
  categories: AiTryOnCatalogCategory[];
  selectedCategory: string;
  selectedAccessories: number[];
  onCategoryChange: (key: string) => void;
  onPageChange: (page: number) => void;
  onToggleAccessory: (item: AiTryOnCatalogItem) => void;
}

export default function AccessoryPanel({
  accessories,
  accessoryPage,
  categories,
  selectedCategory,
  selectedAccessories,
  onCategoryChange,
  onPageChange,
  onToggleAccessory,
}: AccessoryPanelProps) {
  return (
    <div className={styles.panel}>
      <div className={styles.stepHeader}>
        <span className={styles.stepBadge}>3</span>
        <h2>CHỌN PHỤ KIỆN</h2>
      </div>

      <CategoryTabs
        categories={categories}
        selected={selectedCategory}
        onChange={onCategoryChange}
        layoutId="ai-accessory-category-pill"
      />

      <motion.div
        className={styles.grid}
        variants={listStagger}
        initial="hidden"
        animate="show"
        key={`${selectedCategory}-${accessoryPage.page}`}
      >
        {accessories.map((item) => {
          const isSelected = selectedAccessories.includes(item.productId);
          return (
            <motion.button
              key={item.productId}
              className={`${styles.card} ${isSelected ? styles.selected : ''}`}
              type="button"
              onClick={() => onToggleAccessory(item)}
              variants={fadeUp}
              whileHover={{ y: -2 }}
              whileTap={{ scale: 0.97 }}
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
        page={accessoryPage.page}
        pageSize={accessoryPage.pageSize}
        totalItems={accessoryPage.totalItems}
        totalPages={accessoryPage.totalPages}
        onPageChange={onPageChange}
      />
    </div>
  );
}
