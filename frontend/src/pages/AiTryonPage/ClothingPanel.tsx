import { useMemo } from 'react';
import { motion } from 'framer-motion';
import { listStagger, cardReveal } from '../../utils/motion';
import CategoryTabs from './CategoryTabs';
import type { AiTryOnCatalogItem } from '../../api/aiTryon';
import { resolveAssetUrl } from '../../api/client';
import styles from './ClothingPanel.module.css';

interface ClothingPanelProps {
  selectedCategory: string;
  selectedGarment: number | null;
  garments: AiTryOnCatalogItem[];
  onCategoryChange: (key: string) => void;
  onSelectGarment: (id: number) => void;
}

export default function ClothingPanel({
  selectedCategory,
  selectedGarment,
  garments,
  onCategoryChange,
  onSelectGarment,
}: ClothingPanelProps) {
  const garmentCategories = useMemo(() => {
    const mappedCategories = garments
      .reduce((accumulator, garment) => {
        if (!accumulator.some((item) => item.key === garment.categorySlug)) {
          accumulator.push({
            key: garment.categorySlug,
            label: formatCategoryLabel(garment.categorySlug),
          });
        }

        return accumulator;
      }, [{ key: 'all', label: 'All' }, { key: 'bestseller', label: 'Bestseller' }] as Array<{ key: string; label: string }>);

    return mappedCategories;
  }, [garments]);

  const filteredGarments = useMemo(() => {
    if (selectedCategory === 'all') return garments;
    if (selectedCategory === 'bestseller') return garments.filter((garment) => garment.isFeatured);
    return garments.filter((garment) => garment.categorySlug === selectedCategory);
  }, [garments, selectedCategory]);

  return (
    <div className={styles.panel}>
      <div className={styles.stepHeader}>
        <span className={styles.stepBadge}>2</span>
        <h2>CHỌN TRANG PHỤC</h2>
      </div>

      <CategoryTabs
        categories={garmentCategories}
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
          const isSelected = selectedGarment === item.productId;
          return (
            <motion.button
              key={item.productId}
              className={`${styles.card} ${isSelected ? styles.selected : ''}`}
              type="button"
              onClick={() => onSelectGarment(item.productId)}
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
    </div>
  );
}

function formatCategoryLabel(categorySlug: string) {
  switch (categorySlug) {
    case 'ao-dai-truyen-thong':
      return 'Áo dài truyền thống';
    case 'ao-dai-lua-tron':
      return 'Áo dài lụa trơn';
    case 'ao-dai-theu-hoa':
      return 'Áo dài thêu hoa';
    case 'ao-dai-cach-tan':
      return 'Áo dài cách tân';
    default:
      return categorySlug;
  }
}
