import { motion, AnimatePresence } from 'framer-motion';
import { fadeScale } from '../../utils/motion';
import styles from './ResultPanel.module.css';

interface ResultPanelProps {
  tryonResult: string | null;
  selectedGarment: string | null;
  canTryOn: boolean;
  isProcessing: boolean;
  errorMessage?: string | null;
  onTryonClick: () => void;
}

export default function ResultPanel({
  tryonResult,
  canTryOn,
  isProcessing,
  errorMessage,
  onTryonClick,
}: ResultPanelProps) {
  const isEnabled = canTryOn && !isProcessing;

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
        className={`${styles.tryonButton} ${isEnabled ? styles.enabled : ''}`}
        disabled={!isEnabled}
        onClick={onTryonClick}
      >
        {isProcessing ? 'ĐANG XỬ LÝ...' : 'THỬ ĐỒ NGAY'}
      </button>
      {errorMessage ? <p className={styles.errorMessage}>{errorMessage}</p> : null}
    </div>
  );
}
