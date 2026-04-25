import { motion } from 'framer-motion';
import styles from './AiSection.module.css';
import { fadeUp, staggerContainer } from '../../utils/motion';

const benefits = [
  {
    title: 'Thử Đồ Ảo Thông Minh',
    desc: 'Upload ảnh của bạn và xem ngay kết quả với bất kỳ mẫu áo dài nào',
  },
  {
    title: 'Kết Quả Tức Thì',
    desc: 'Nhận được hình ảnh chỉ trong vài giây với độ chính xác cao',
  },
  {
    title: '100% Miễn Phí',
    desc: 'Không giới hạn số lần thử, hoàn toàn miễn phí cho khách hàng',
  },
];

export default function AiCopy() {
  return (
    <motion.div className={styles.aiCopy} variants={staggerContainer}>
      <motion.p className={styles.eyebrow} variants={fadeUp}>Áo Dài Nhã Uyên</motion.p>
      <motion.h1 id="ai-title" variants={fadeUp}>
        Trải Nghiệm Áo Dài
        <span>Với Công Nghệ AI</span>
      </motion.h1>
      <motion.p className={styles.aiIntro} variants={fadeUp}>
        Khám phá vẻ đẹp của bạn trong tà áo dài Nhã Uyên mà không cần thử trực tiếp. Công nghệ AI
        giúp bạn xem trước cách mỗi thiết kế tôn lên nét đẹp riêng chỉ trong vài giây.
      </motion.p>

      <motion.ul className={styles.aiBenefits} aria-label="Lợi ích thử đồ AI" variants={staggerContainer}>
        {benefits.map((b) => (
          <motion.li key={b.title} variants={fadeUp}>
            <span className={styles.benefitDot} />
            <div>
              <strong>{b.title}</strong>
              <p>{b.desc}</p>
            </div>
          </motion.li>
        ))}
      </motion.ul>

      <motion.a className={`${styles.primaryCta} hover-lift`} href="/collection" variants={fadeUp}>
        Thử đồ AI Ngay <span aria-hidden="true">&rarr;</span>
      </motion.a>
      <motion.p className={styles.ctaNote} variants={fadeUp}>Dùng thử hoàn toàn miễn phí</motion.p>
    </motion.div>
  );
}
