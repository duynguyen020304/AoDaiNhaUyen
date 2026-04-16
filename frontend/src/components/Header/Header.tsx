import { useState } from 'react';
import { motion } from 'framer-motion';
import styles from './Header.module.css';
import { fadeUp, staggerContainer } from '../../utils/motion';

const navLinks = [
  { label: 'TRANG CHỦ', href: '#top' },
  { label: 'BỘ SƯU TẬP', href: '#collection' },
  { label: '\u2728 THỬ ĐỒ AI', href: '#ai' },
  { label: 'Áo dài', href: '#product' },
  { label: 'Phụ kiện', href: '#accessories' },
];

export default function Header() {
  const [activeHref, setActiveHref] = useState(navLinks[0].href);

  return (
    <motion.header
      className={styles.siteHeader}
      initial={{ opacity: 0, y: -18 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.52, ease: [0.22, 1, 0.36, 1] }}
    >
      <motion.a
        className={styles.brandMark}
        href="#top"
        aria-label="Áo dài Nhã Uyên"
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
        {navLinks.map((link) => (
          <motion.a
            key={link.href}
            className={`${styles.navLink} ${activeHref === link.href ? styles.isActive : ''}`}
            href={link.href}
            onClick={() => setActiveHref(link.href)}
            variants={fadeUp}
            whileHover={{ y: -1 }}
            whileTap={{ scale: 0.97 }}
          >
            {activeHref === link.href ? (
              <motion.span className={styles.activePill} layoutId="header-active-pill" />
            ) : null}
            <span className={styles.navLabel}>{link.label}</span>
          </motion.a>
        ))}
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
          0
        </motion.a>
      </motion.nav>
    </motion.header>
  );
}
