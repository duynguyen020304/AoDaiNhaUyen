import { motion, AnimatePresence } from 'framer-motion';
import { fadeScale } from '../../utils/motion';
import styles from './ResultPanel.module.css';

interface ResultPanelProps {
  tryonResult: string | null;
  selectedGarment: string | null;
  isProcessing: boolean;
  onTryonClick: () => void;
}

export default function ResultPanel({
  tryonResult,
  selectedGarment,
  isProcessing,
  onTryonClick,
}: ResultPanelProps) {
  const canTryOn = !!selectedGarment && !isProcessing;

  return (
    <div className={styles.panel}>
      <h2 className={styles.heading}>KẾT QUẢ</h2>

      <div className={styles.resultArea}>
        <AnimatePresence mode="wait">
          {tryonResult ? (
            <motion.div
              key="result"
              className={styles.resultImage}
              variants={fadeScale}
              initial="hidden"
              animate="show"
            >
              <img src={tryonResult} alt="Kết quả thử đồ" />
            </motion.div>
          ) : (
            <motion.div
              key="placeholder"
              className={styles.placeholder}
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
            >
              <p>Kết quả thử đồ sẽ hiển thị tại đây</p>
            </motion.div>
          )}
        </AnimatePresence>
      </div>

      <button
        className={`${styles.tryonButton} ${canTryOn ? styles.enabled : ''}`}
        disabled={!canTryOn}
        onClick={onTryonClick}
      >
        {isProcessing ? 'ĐANG XỬ LÝ...' : 'THỬ ĐỒ NGAY'}
      </button>
    </div>
  );
}
