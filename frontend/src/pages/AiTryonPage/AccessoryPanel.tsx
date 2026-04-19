import { motion } from 'framer-motion';
import { listStagger, fadeUp } from '../../utils/motion';
import type { AiTryOnCatalogItem } from '../../api/aiTryon';
import { resolveAssetUrl } from '../../api/client';
import styles from './AccessoryPanel.module.css';

interface AccessoryPanelProps {
  accessories: AiTryOnCatalogItem[];
  selectedAccessories: number[];
  onToggleAccessory: (id: number) => void;
}

export default function AccessoryPanel({
  accessories,
  selectedAccessories,
  onToggleAccessory,
}: AccessoryPanelProps) {
  return (
    <div className={styles.panel}>
      <div className={styles.stepHeader}>
        <span className={styles.stepBadge}>3</span>
        <h2>CHỌN PHỤ KIỆN</h2>
      </div>

      <motion.div
        className={styles.grid}
        variants={listStagger}
        initial="hidden"
        animate="show"
      >
        {accessories.map((item) => {
          const isSelected = selectedAccessories.includes(item.productId);
          return (
            <motion.button
              key={item.productId}
              className={`${styles.card} ${isSelected ? styles.selected : ''}`}
              type="button"
              onClick={() => onToggleAccessory(item.productId)}
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
    </div>
  );
}
