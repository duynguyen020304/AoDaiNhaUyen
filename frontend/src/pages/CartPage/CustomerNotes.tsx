import styles from './CustomerNotes.module.css';

export default function CustomerNotes() {
  return (
    <div className={styles.card}>
      <div className={styles.cardHeader}>
        <svg width="17.5" height="17.5" viewBox="0 0 17.5 17.5" fill="none">
          <path
            d="M12.03 2.19a2.63 2.63 0 014.28 2.81l-.29.87a1.31 1.31 0 01-.33.53L6.56 15.53l-4.37.73.73-4.37L12.05 2.76a1.31 1.31 0 01.53-.33l.87-.29-.42.05z"
            stroke="#0A0A0A"
            strokeWidth="1.3"
            strokeLinecap="round"
            strokeLinejoin="round"
          />
        </svg>
        <h3 className={styles.cardTitle}>Ghi chú của khách hàng</h3>
      </div>
      <div className={styles.cardContent}>
        <div className={styles.label}>
          Thêm ghi chú cho đơn hàng của bạn (lựa chọn)
        </div>
        <textarea
          className={styles.textarea}
          placeholder="Bức cứ những yêu cầu đặc biệt, lời nhắn gửi và vấn đề đóng gói"
          rows={3}
        />
        <p className={styles.hint}>
          Ví dụ: "Xin hãy gói quà cho sản phẩm", "Quà này dành cho mẹ tôi", "Bảo quản với chất lượng tốt nhất",...
        </p>
      </div>
    </div>
  );
}
