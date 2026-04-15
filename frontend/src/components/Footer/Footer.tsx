import styles from './Footer.module.css';
import NewsletterForm from './NewsletterForm';

export default function Footer() {
  return (
    <footer className={styles.siteFooter} id="footer">
      <h2 className={styles.siteFooterTitle}>Áo dài Nhã Uyên</h2>
      <div className={styles.footerGrid}>
        <section>
          <img className={styles.footerLogo} src="/assets/footer-logo.png" alt="Nhã Uyên" />
          <p>
            Tôn vinh vẻ đẹp truyền thống của người phụ nữ Việt Nam qua những tà áo dài được thiết
            kế tinh xảo, hiện đại nhưng vẫn giữ gìn bản sắc dân tộc.
          </p>
        </section>
        <section>
          <h3>Liên Kết</h3>
          <a href="#collection">Về MaryMy</a>
          <a href="#collection">Chính sách đổi trả</a>
          <a href="#collection">Hướng dẫn chọn size</a>
          <a href="#collection">Blog thời trang</a>
        </section>
        <section>
          <h3>Liên Hệ</h3>
          <p>123 Nguyễn Huệ, Quận 1, TP. Hồ Chí Minh</p>
          <p>1900 123 456</p>
          <p>support@marymy.vn</p>
        </section>
        <section>
          <h3>Đăng Ký Nhận Tin</h3>
          <p>Nhận thông tin về bộ sưu tập mới nhất và ưu đãi đặc biệt.</p>
          <NewsletterForm />
        </section>
      </div>
      <div className={styles.footerBottom}>
        <p>&copy; 2024 MaryMy. All rights reserved.</p>
        <div className={styles.socials} aria-label="Mạng xã hội">
          <a className="hover-lift" href="#top" aria-label="Facebook">f</a>
          <a className="hover-lift" href="#top" aria-label="Instagram">◎</a>
        </div>
      </div>
    </footer>
  );
}
