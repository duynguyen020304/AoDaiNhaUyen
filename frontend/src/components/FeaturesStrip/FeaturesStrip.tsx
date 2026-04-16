import { motion } from 'framer-motion';
import styles from './FeaturesStrip.module.css';
import { cardHover, fadeUp, sectionReveal, viewportOnce } from '../../utils/motion';

const features = [
  { icon: '\u25B1', title: 'MIỄN PHÍ VẬN CHUYỂN', desc: 'Đơn hàng trên 1 triệu' },
  { icon: '\u25A1', title: 'THANH TOÁN AN TOÀN', desc: 'Bảo mật tuyệt đối' },
  { icon: '\u21BB', title: 'ĐỔI TRẢ 7 NGÀY', desc: 'Nếu có lỗi nhà sản xuất' },
];

export default function FeaturesStrip() {
  return (
    <motion.section
      className={styles.featuresStrip}
      aria-label="Dịch vụ"
      variants={sectionReveal}
      initial="hidden"
      whileInView="show"
      viewport={viewportOnce}
    >
      {features.map((f) => (
        <motion.article
          key={f.title}
          className={styles.article}
          variants={fadeUp}
          whileHover={cardHover.hover}
          transition={{ duration: 0.24, ease: 'easeOut' }}
        >
          <motion.span
            className={styles.icon}
            aria-hidden="true"
            whileHover={{ rotate: 8, scale: 1.08 }}
          >
            {f.icon}
          </motion.span>
          <div>
            <h2 className={styles.title}>{f.title}</h2>
            <p className={styles.desc}>{f.desc}</p>
          </div>
        </motion.article>
      ))}
    </motion.section>
  );
}
