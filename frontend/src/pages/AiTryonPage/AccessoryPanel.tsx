import { motion } from 'framer-motion';
import { listStagger, fadeUp } from '../../utils/motion';
import { ACCESSORIES } from './data';
import styles from './AccessoryPanel.module.css';

interface AccessoryPanelProps {
  selectedAccessories: string[];
  onToggleAccessory: (id: string) => void;
}

export default function AccessoryPanel({
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
        {ACCESSORIES.map((item) => {
          const isSelected = selectedAccessories.includes(item.id);
          return (
            <motion.button
              key={item.id}
              className={`${styles.card} ${isSelected ? styles.selected : ''}`}
              type="button"
              onClick={() => onToggleAccessory(item.id)}
              variants={fadeUp}
              whileHover={{ y: -2 }}
              whileTap={{ scale: 0.97 }}
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
