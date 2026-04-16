import { useState } from 'react';
import { motion } from 'framer-motion';
import styles from './Header.module.css';
import { fadeUp, staggerContainer } from '../../utils/motion';
import type { Page } from '../../App';

const navLinks = [
  { label: 'TRANG CHỦ', href: '#top', page: 'home' as Page },
  { label: 'BỘ SƯU TẬP', href: '#collection', page: 'collection' as Page },
  { label: '\u2728 THỬ ĐỒ AI', href: '#ai', page: undefined },
  { label: 'Áo dài', href: '#product', page: undefined },
  { label: 'Phụ kiện', href: '#accessories', page: undefined },
];

interface HeaderProps {
  currentPage: Page;
  onNavigate: (page: Page) => void;
}

export default function Header({ currentPage, onNavigate }: HeaderProps) {
  const [activeHref, setActiveHref] = useState(navLinks[0].href);

  const handleClick = (link: typeof navLinks[number]) => {
    setActiveHref(link.href);
    if (link.page) {
      onNavigate(link.page);
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
        href="#top"
        aria-label="Áo dài Nhã Uyên"
        onClick={(e) => { e.preventDefault(); onNavigate('home'); }}
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
            className={`${styles.navLink} ${(currentPage === 'collection' && link.page === 'collection') || (currentPage === 'home' && activeHref === link.href && !link.page) ? styles.isActive : ''}`}
            href={link.href}
            onClick={(e) => {
              if (link.page) {
                e.preventDefault();
                handleClick(link);
              } else {
                handleClick(link);
              }
            }}
            variants={fadeUp}
            whileHover={{ y: -1 }}
            whileTap={{ scale: 0.97 }}
          >
            {(currentPage === 'collection' && link.page === 'collection') || (currentPage === 'home' && activeHref === link.href) ? (
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
