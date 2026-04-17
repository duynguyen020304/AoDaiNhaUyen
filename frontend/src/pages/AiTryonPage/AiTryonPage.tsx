import { motion } from 'framer-motion';
import { sectionReveal, fadeUp, viewportOnce } from '../../utils/motion';
import styles from './AiTryonPage.module.css';

export default function AiTryonPage() {
  return (
    <main className={styles.page}>
      <motion.section
        className={styles.hero}
        variants={sectionReveal}
        initial="hidden"
        whileInView="show"
        viewport={viewportOnce}
      >
        <motion.span className={styles.badge} variants={fadeUp}>
          BETA FEATURE
        </motion.span>
        <motion.h1 variants={fadeUp}>Phòng Thử Đồ Ảo AI</motion.h1>
        <motion.p className={styles.description} variants={fadeUp}>
          Tải lên ảnh khuôn mặt của bạn và để trí tuệ nhân tạo của MaryMy giúp bạn
          thử những thiết kế Áo Dài lộng lẫy nhất trước khi quyết định.
        </motion.p>
      </motion.section>

      <motion.section
        className={styles.tryonSection}
        variants={sectionReveal}
        initial="hidden"
        whileInView="show"
        viewport={viewportOnce}
      >
        <motion.div className={styles.uploadPanel} variants={fadeUp}>
          <div className={styles.stepHeader}>
            <span className={styles.stepBadge}>1</span>
            <h2>TẢI LÊN ẢNH CỦA BẠN</h2>
          </div>
          <div className={styles.dropZone}>
            <img src="/assets/ai-tryon/upload-icon.svg" alt="" className={styles.uploadIcon} />
            <p>Nhấn để tải ảnh lên</p>
            <span>Hỗ trợ JPG, PNG (Khuyến khích ảnh chụp chân dung)</span>
          </div>
        </motion.div>

        <motion.div className={styles.resultPanel} variants={fadeUp}>
          <h2>KẾT QUẢ</h2>
          <div className={styles.resultPlaceholder}>
            <p>Kết quả thử đồ sẽ hiển thị tại đây</p>
          </div>
          <button className={styles.tryonButton}>THỬ ĐỒ NGAY</button>
        </motion.div>
      </motion.section>
    </main>
  );
}
