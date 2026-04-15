import styles from './Header.module.css';

const navLinks = [
  { label: 'TRANG CHỦ', href: '#top', active: true },
  { label: 'BỘ SƯU TẬP', href: '#collection', active: false },
  { label: '\u2728 THỬ ĐỒ AI', href: '#ai', active: false },
  { label: 'Áo dài', href: '#product', active: false },
  { label: 'Phụ kiện', href: '#accessories', active: false },
];

export default function Header() {
  return (
    <header className={styles.siteHeader}>
      <a className={styles.brandMark} href="#top" aria-label="Áo dài Nhã Uyên">
        <img src="/assets/footer-logo.png" alt="" />
      </a>

      <nav className={styles.mainNav} aria-label="Điều hướng chính">
        {navLinks.map((link) => (
          <a
            key={link.href}
            className={`${link.active ? styles.isActive : ''} hover-lift`}
            href={link.href}
          >
            {link.label}
          </a>
        ))}
        <a className={`${styles.loginLink} hover-lift`} href="#footer">ĐĂNG NHẬP</a>
        <a className={styles.cartLink} href="#footer" aria-label="Giỏ hàng">0</a>
      </nav>
    </header>
  );
}
