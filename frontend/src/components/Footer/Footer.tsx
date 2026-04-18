import { motion } from 'framer-motion';
import styles from './Footer.module.css';
import NewsletterForm from './NewsletterForm';
import { cardHover, fadeUp, sectionReveal, staggerContainer, viewportOnce } from '../../utils/motion';

export default function Footer() {
  return (
    <motion.footer
      className={styles.siteFooter}
      id="footer"
      variants={sectionReveal}
      initial="hidden"
      whileInView="show"
      viewport={viewportOnce}
    >
      <motion.h2 className={styles.siteFooterTitle} variants={fadeUp}>Áo dài Nhã Uyên</motion.h2>
      <motion.div className={styles.footerGrid} variants={staggerContainer}>
        <motion.section variants={fadeUp} whileHover={cardHover.hover} transition={{ duration: 0.24, ease: 'easeOut' }}>
          <img className={styles.footerLogo} src="/assets/footer-logo.png" alt="Nhã Uyên" />
          <p>
            Tôn vinh vẻ đẹp truyền thống của người phụ nữ Việt Nam qua những tà áo dài được thiết
            kế tinh xảo, hiện đại nhưng vẫn giữ gìn bản sắc dân tộc.
          </p>
        </motion.section>
        <motion.section variants={fadeUp} whileHover={cardHover.hover} transition={{ duration: 0.24, ease: 'easeOut' }}>
          <h3>Liên Kết</h3>
          <a href="/collection">Về MaryMy</a>
          <a href="#collection">Chính sách đổi trả</a>
          <a href="#collection">Hướng dẫn chọn size</a>
          <a href="#collection">Blog thời trang</a>
        </motion.section>
        <motion.section variants={fadeUp} whileHover={cardHover.hover} transition={{ duration: 0.24, ease: 'easeOut' }}>
          <h3>Pháp Lý</h3>
          <a href="/privacy-policy">Chính sách quyền riêng tư</a>
          <a href="/data-deletion">Xóa dữ liệu người dùng</a>
        </motion.section>
        <motion.section variants={fadeUp} whileHover={cardHover.hover} transition={{ duration: 0.24, ease: 'easeOut' }}>
          <h3>Liên Hệ</h3>
          <p>123 Nguyễn Huệ, Quận 1, TP. Hồ Chí Minh</p>
          <p>1900 123 456</p>
          <p>support@marymy.vn</p>
        </motion.section>
        <motion.section variants={fadeUp} whileHover={cardHover.hover} transition={{ duration: 0.24, ease: 'easeOut' }}>
          <h3>Đăng Ký Nhận Tin</h3>
          <p>Nhận thông tin về bộ sưu tập mới nhất và ưu đãi đặc biệt.</p>
          <NewsletterForm />
        </motion.section>
      </motion.div>
      <motion.div className={styles.footerBottom} variants={fadeUp}>
        <p>&copy; 2026 Áo Dài Nhã Uyên. All rights reserved.</p>
        <div className={styles.socials} aria-label="Mạng xã hội">
          <a className="hover-lift" href="/" aria-label="Facebook">f</a>
          <a className="hover-lift" href="/" aria-label="Instagram">◎</a>
        </div>
      </motion.div>
    </motion.footer>
  );
}
