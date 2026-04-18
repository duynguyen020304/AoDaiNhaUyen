import { useState } from 'react';
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
  const [previewOpen, setPreviewOpen] = useState(false);

  const handleDownload = () => {
    if (!tryonResult) return;

    const link = document.createElement('a');
    link.href = tryonResult;
    link.download = `ai-tryon-result-${Date.now()}.png`;
    link.rel = 'noopener';
    document.body.appendChild(link);
    link.click();
    link.remove();
  };

  return (
    <>
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
                <div className={styles.resultActions}>
                  <button
                    type="button"
                    className={styles.resultAction}
                    onClick={() => setPreviewOpen(true)}
                  >
                    Xem ảnh
                  </button>
                  <button
                    type="button"
                    className={styles.resultActionSecondary}
                    onClick={handleDownload}
                  >
                    Tải xuống
                  </button>
                </div>
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

      <AnimatePresence>
        {previewOpen && tryonResult ? (
          <motion.div
            className={styles.previewOverlay}
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            onClick={() => setPreviewOpen(false)}
          >
            <motion.img
              src={tryonResult}
              alt="Xem trước kết quả thử đồ"
              className={styles.previewImage}
              initial={{ scale: 0.96, opacity: 0 }}
              animate={{ scale: 1, opacity: 1 }}
              exit={{ scale: 0.96, opacity: 0 }}
              onClick={(event) => event.stopPropagation()}
            />
            <div className={styles.previewActions} onClick={(event) => event.stopPropagation()}>
              <button
                type="button"
                className={styles.resultAction}
                onClick={handleDownload}
              >
                Tải xuống
              </button>
            </div>
            <button
              type="button"
              className={styles.previewClose}
              onClick={() => setPreviewOpen(false)}
            >
              ×
            </button>
          </motion.div>
        ) : null}
      </AnimatePresence>
    </>
  );
}
