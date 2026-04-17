import { useEffect, useMemo, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { motion } from 'framer-motion';
import styles from './Header.module.css';
import { fadeUp, staggerContainer } from '../../utils/motion';
import { getHeaderCategories } from '../../api/catalog';
import type { HeaderCategory } from '../../types/catalog';
import { useAuth } from '../../auth/useAuth';

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

const fallbackCategories: HeaderCategory[] = [
  {
    id: 1,
    name: 'Áo dài',
    slug: 'ao-dai',
    sortOrder: 1,
    children: [
      { id: 3, name: 'Áo dài truyền thống', slug: 'ao-dai-truyen-thong', sortOrder: 1 },
      { id: 4, name: 'Áo dài cách tân', slug: 'ao-dai-cach-tan', sortOrder: 2 },
      { id: 5, name: 'Áo dài lụa trơn', slug: 'ao-dai-lua-tron', sortOrder: 3 },
      { id: 6, name: 'Áo dài thêu hoa', slug: 'ao-dai-theu-hoa', sortOrder: 4 },
    ],
  },
  {
    id: 2,
    name: 'Phụ kiện',
    slug: 'phu-kien',
    sortOrder: 2,
    children: [
      { id: 7, name: 'Trâm cài', slug: 'tram-cai', sortOrder: 1 },
      { id: 8, name: 'Túi sách', slug: 'tui-sach', sortOrder: 2 },
      { id: 9, name: 'Quạt', slug: 'quat', sortOrder: 3 },
      { id: 10, name: 'Giày', slug: 'giay', sortOrder: 4 },
    ],
  },
];

const DISMISSED_DROPDOWN_CLASS = 'headerDropdownDismissed';

export default function Header() {
  const location = useLocation();
  const navigate = useNavigate();
  const { status, user, logout } = useAuth();
  const [categories, setCategories] = useState<HeaderCategory[]>(fallbackCategories);

  useEffect(() => {
    let ignore = false;

    getHeaderCategories()
      .then((data) => {
        if (!ignore && data.length > 0) {
          setCategories(data);
        }
      })
      .catch(() => {
        if (!ignore) {
          setCategories(fallbackCategories);
        }
      });

    return () => {
      ignore = true;
    };
  }, []);

  const categoriesBySlug = useMemo(() => {
    return new Map(categories.map((category) => [category.slug, category]));
  }, [categories]);

  const activeCategory = new URLSearchParams(location.search).get('category');

  const handleClick = (link: NavLinkConfig, e: React.MouseEvent) => {
    if (location.pathname !== link.to) {
      e.preventDefault();
      navigate(link.to);
    }
  };

  async function handleLogout() {
    await logout();
    navigate('/login');
  }

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
          const category = link.to === '/products'
            ? categoriesBySlug.get('ao-dai')
            : link.to === '/accessories'
              ? categoriesBySlug.get('phu-kien')
              : undefined;
          const isCategoryActive = category?.children.some((child) => child.slug === activeCategory) ?? false;
          const isActive = location.pathname === link.matchPath || isCategoryActive;
          return (
            <motion.div
              key={link.to}
              className={styles.navItem}
              variants={fadeUp}
              whileHover={{ y: -1 }}
            >
              <a
                className={`${styles.navLink} ${isActive ? styles.isActive : ''}`}
                href={link.to}
                onMouseEnter={() => document.body.classList.remove(DISMISSED_DROPDOWN_CLASS)}
                onClick={(e) => handleClick(link, e)}
              >
                {isActive ? (
                  <motion.span className={styles.activePill} layoutId="header-active-pill" />
                ) : null}
                <span className={styles.navLabel}>
                  {link.label}
                  {category ? <span className={styles.caret}>⌄</span> : null}
                </span>
              </a>
              {category && category.children.length > 0 ? (
                <div className={styles.dropdown}>
                  {category.children.map((child) => {
                    const targetPath = category.slug === 'ao-dai' ? '/products' : '/accessories';
                    const target = `${targetPath}?category=${child.slug}`;
                    return (
                      <a
                        key={child.slug}
                        className={`${styles.dropdownLink} ${activeCategory === child.slug ? styles.dropdownActive : ''}`}
                        href={target}
                        onClick={(event) => {
                          event.preventDefault();
                          document.body.classList.add(DISMISSED_DROPDOWN_CLASS);
                          navigate(target);
                        }}
                      >
                        {child.name}
                      </a>
                    );
                  })}
                </div>
              ) : null}
            </motion.div>
          );
        })}
        {status === 'authenticated' && user ? (
          <motion.div className={styles.authGroup} variants={fadeUp}>
            <a
              className={styles.accountLink}
              href="/account"
              onClick={(event) => {
                event.preventDefault();
                navigate('/account');
              }}
            >
              {user.fullName}
            </a>
            <button className={styles.logoutButton} type="button" onClick={handleLogout}>
              Dang xuat
            </button>
          </motion.div>
        ) : (
          <motion.a
            className={styles.loginLink}
            href="/login"
            onClick={(event) => {
              event.preventDefault();
              navigate('/login');
            }}
            variants={fadeUp}
            whileHover={{ y: -1, backgroundColor: 'rgba(255, 255, 255, 0.12)' }}
            whileTap={{ scale: 0.97 }}
          >
            DANG NHAP
          </motion.a>
        )}
        <motion.a
          className={styles.cartLink}
          href="/cart"
          aria-label="Giỏ hàng"
          onClick={(e) => { e.preventDefault(); navigate('/cart'); }}
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
