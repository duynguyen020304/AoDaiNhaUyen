import { useMemo, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { motion } from 'framer-motion';
import { formatAccountDisplayName } from '../../utils/accountDisplay';
import styles from './Header.module.css';
import { fadeUp, staggerContainer } from '../../utils/motion';
import type { HeaderCategory } from '../../types/catalog';
import { useAuth } from '../../auth/useAuth';
import { useCartQuery } from '../../hooks/cart/useCartQueries';
import { useHeaderCategoriesQuery } from '../../hooks/catalog/useCatalogQueries';
import { useBlogCategories } from '../../hooks/blog/useBlogQueries';

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
  { label: 'Bài viết', to: '/blog/', matchPath: '/blog' },
];

const NAV_FALLBACK_CATEGORIES: HeaderCategory[] = [
  {
    id: '1',
    name: 'Áo dài',
    slug: 'ao-dai',
    sortOrder: 1,
    children: [
      { id: '3', name: 'Áo dài truyền thống', slug: 'ao-dai-truyen-thong', sortOrder: 1 },
      { id: '4', name: 'Áo dài cách tân', slug: 'ao-dai-cach-tan', sortOrder: 2 },
      { id: '5', name: 'Áo dài lụa trơn', slug: 'ao-dai-lua-tron', sortOrder: 3 },
      { id: '6', name: 'Áo dài thêu hoa', slug: 'ao-dai-theu-hoa', sortOrder: 4 },
    ],
  },
  {
    id: '2',
    name: 'Phụ kiện',
    slug: 'phu-kien',
    sortOrder: 2,
    children: [
      { id: '7', name: 'Trâm cài', slug: 'tram-cai', sortOrder: 1 },
      { id: '8', name: 'Túi sách', slug: 'tui-sach', sortOrder: 2 },
      { id: '9', name: 'Quạt', slug: 'quat', sortOrder: 3 },
      { id: '10', name: 'Giày', slug: 'giay', sortOrder: 4 },
    ],
  },
];


interface HeaderProps {
  onOpenAccount: () => void;
  onOpenAuth: () => void;
}

export default function Header({ onOpenAuth }: HeaderProps) {
  const location = useLocation();
  const navigate = useNavigate();
  const { status, user, logout } = useAuth();
  const categoriesQuery = useHeaderCategoriesQuery();
  const blogCategoriesQuery = useBlogCategories();
  const cartQuery = useCartQuery(status === 'authenticated');
  const [openDropdown, setOpenDropdown] = useState<string | null>(null);
  const categories = categoriesQuery.data && categoriesQuery.data.length > 0
    ? categoriesQuery.data
    : NAV_FALLBACK_CATEGORIES;
  const cartItemCount = status === 'authenticated' ? cartQuery.data?.totalItemCount ?? 0 : 0;

  const categoriesBySlug = useMemo(() => {
    return new Map(categories.map((category) => [category.slug, category]));
  }, [categories]);

  const activeCategory = new URLSearchParams(location.search).get('category');

  const handleClick = (link: NavLinkConfig, e: React.MouseEvent) => {
    setOpenDropdown(null);

    if (location.pathname !== link.to) {
      e.preventDefault();
      navigate(link.to);
    }
  };

  async function handleLogout() {
    await logout();
    onOpenAuth();
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
          const isBlog = link.to === '/blog/';
          const blogCategories = blogCategoriesQuery.data ?? [];
          const hasDropdown = category !== undefined || (isBlog && blogCategories.length > 0);
          const currentBlogCategory = isBlog ? new URLSearchParams(location.search).get('category') : null;
          const isCategoryActive = category?.children.some((child) => child.slug === activeCategory) ?? false;
          const isActive = location.pathname === link.matchPath || location.pathname.startsWith(`${link.matchPath}/`) || isCategoryActive;
          return (
            <motion.div
              key={link.to}
              className={`${styles.navItem} ${openDropdown === link.to ? styles.dropdownOpen : ''}`}
              variants={fadeUp}
              whileHover={{ y: -1 }}
              onMouseEnter={() => setOpenDropdown(hasDropdown ? link.to : null)}
              onMouseLeave={() => setOpenDropdown(null)}
              onFocus={() => setOpenDropdown(hasDropdown ? link.to : null)}
              onBlur={(event) => {
                if (!event.currentTarget.contains(event.relatedTarget)) {
                  setOpenDropdown(null);
                }
              }}
            >
              <a
                className={`${styles.navLink} ${isActive ? styles.isActive : ''}`}
                href={link.to}
                onClick={(e) => handleClick(link, e)}
              >
                {isActive ? (
                  <motion.span className={styles.activePill} layoutId="header-active-pill" />
                ) : null}
                <span className={styles.navLabel}>
                  {link.label}
                  {hasDropdown ? <span className={styles.caret}></span> : null}
                </span>
              </a>
              {hasDropdown ? (
                <div className={styles.dropdown}>
                  {category && category.children.length > 0
                    ? category.children.map((child) => {
                        const targetPath = category.slug === 'ao-dai' ? '/products' : '/accessories';
                        const target = `${targetPath}?category=${child.slug}`;
                        return (
                          <a
                            key={child.slug}
                            className={`${styles.dropdownLink} ${activeCategory === child.slug ? styles.dropdownActive : ''}`}
                            href={target}
                            onClick={(event) => {
                              event.preventDefault();
                              setOpenDropdown(null);
                              navigate(target);
                            }}
                          >
                            {child.name}
                          </a>
                        );
                      })
                    : null}
                  {isBlog && blogCategories.length > 0 ? (
                    <>
                      <a
                        className={`${styles.dropdownLink} ${currentBlogCategory === null ? styles.dropdownActive : ''}`}
                        href="/blog/"
                        onClick={(event) => {
                          event.preventDefault();
                          setOpenDropdown(null);
                          navigate('/blog/');
                        }}
                      >
                        Tất cả bài viết
                      </a>
                      {blogCategories.map((blogCategory) => {
                        const target = `/blog/?category=${encodeURIComponent(blogCategory.slug)}`;
                        return (
                          <a
                            key={blogCategory.slug}
                            className={`${styles.dropdownLink} ${currentBlogCategory === blogCategory.slug ? styles.dropdownActive : ''}`}
                            href={target}
                            onClick={(event) => {
                              event.preventDefault();
                              setOpenDropdown(null);
                              navigate(target);
                            }}
                          >
                            {blogCategory.name}
                          </a>
                        );
                      })}
                    </>
                  ) : null}
                </div>
              ) : null}
            </motion.div>
          );
        })}
        {status === 'authenticated' && user ? (
          <motion.div className={styles.authGroup} variants={fadeUp}>
            <a
              className={styles.accountLink}
              href="/account/profile"
              onClick={(event) => {
                event.preventDefault();
                navigate('/account/profile');
              }}
            >
              {formatAccountDisplayName(user)}
            </a>
            <button className={styles.logoutButton} type="button" onClick={handleLogout}>
              Đăng xuất
            </button>
          </motion.div>
        ) : (
          <motion.a
            className={styles.loginLink}
            href="/login"
            onClick={(event) => {
              event.preventDefault();
              onOpenAuth();
            }}
            variants={fadeUp}
            whileHover={{ y: -1 }}
            whileTap={{ scale: 0.97 }}
          >
            ĐĂNG NHẬP
          </motion.a>
        )}
        <motion.a
          className={styles.cartLink}
          href="/cart"
          aria-label={status === 'authenticated' ? `Giỏ hàng, ${cartItemCount} sản phẩm` : 'Giỏ hàng'}
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
          {status === 'authenticated' ? (
            <span className={styles.cartBadge}>{cartItemCount}</span>
          ) : null}
        </motion.a>
      </motion.nav>
    </motion.header>
  );
}
