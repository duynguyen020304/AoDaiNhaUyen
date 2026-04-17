import { useLocation, useNavigate } from 'react-router-dom';
import { motion } from 'framer-motion';
import styles from './Header.module.css';
import { fadeUp, staggerContainer } from '../../utils/motion';

interface NavLinkConfig {
  label: string;
  to: string;
  matchPath: string;
}

const navLinks: NavLinkConfig[] = [
  { label: 'TRANG CHỦ', to: '/', matchPath: '/' },
  { label: 'BỘ SƯU TẬP', to: '/collection', matchPath: '/collection' },
  { label: '\u2728 THỬ ĐỒ AI', to: '/ai-tryon', matchPath: '/ai-tryon' },
  { label: 'Áo dài', to: '/products', matchPath: '/products' },
  { label: 'Phụ kiện', to: '/accessories', matchPath: '/accessories' },
];

export default function Header() {
  const location = useLocation();
  const navigate = useNavigate();

  const handleClick = (link: NavLinkConfig, e: React.MouseEvent) => {
    if (location.pathname !== link.to) {
      e.preventDefault();
      navigate(link.to);
    }
  };

  return (
    <motion.header
      className={styles.siteHeader}
      initial={{ opacity: 0, y: -18 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.52, ease: [0.22, 1, 0.36, 1] }}
    >
      <motion.a
        className={styles.brandMark}
        href="/"
        aria-label="Áo dài Nhã Uyên"
        onClick={(e) => { e.preventDefault(); navigate('/'); }}
        whileHover={{ scale: 1.04 }}
        whileTap={{ scale: 0.96 }}
      >
        <img src="/assets/footer-logo.png" alt="" />
      </motion.a>

      <motion.nav
        className={styles.mainNav}
        aria-label="Điều hướng chính"
        variants={staggerContainer}
        initial="hidden"
        animate="show"
      >
        {navLinks.map((link) => {
          const isActive = location.pathname === link.matchPath;
          return (
            <motion.a
              key={link.to}
              className={`${styles.navLink} ${isActive ? styles.isActive : ''}`}
              href={link.to}
              onClick={(e) => handleClick(link, e)}
              variants={fadeUp}
              whileHover={{ y: -1 }}
              whileTap={{ scale: 0.97 }}
            >
              {isActive ? (
                <motion.span className={styles.activePill} layoutId="header-active-pill" />
              ) : null}
              <span className={styles.navLabel}>{link.label}</span>
            </motion.a>
          );
        })}
        <motion.a
          className={styles.loginLink}
          href="#footer"
          variants={fadeUp}
          whileHover={{ y: -1, backgroundColor: 'rgba(255, 255, 255, 0.12)' }}
          whileTap={{ scale: 0.97 }}
        >
          ĐĂNG NHẬP
        </motion.a>
        <motion.a
          className={styles.cartLink}
          href="#footer"
          aria-label="Giỏ hàng"
          variants={fadeUp}
          whileHover={{ y: -1, scale: 1.03 }}
          whileTap={{ scale: 0.95 }}
        >
          <svg className={styles.cartIcon} xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <circle cx="9" cy="21" r="1" />
            <circle cx="20" cy="21" r="1" />
            <path d="M1 1h4l2.68 13.39a2 2 0 0 0 2 1.61h9.72a2 2 0 0 0 2-1.61L23 6H6" />
          </svg>
        </motion.a>
      </motion.nav>
    </motion.header>
  );
}
