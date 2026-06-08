import { useMemo, useState } from 'react';
import { motion } from 'framer-motion';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import styles from './ProductsPage.module.css';
import { sectionReveal, staggerContainer, viewportOnce } from '../../utils/motion';
import { SIZES, type Category, type Product } from './data';
import CategoryBanner from '../../components/CategoryBanner/CategoryBanner';
import ProductCard from '../../components/ProductCard/ProductCard';
import { useAddCartItemMutation } from '../../hooks/cart/useCartMutations';
import { useCategoryProductsQueries, useHeaderCategoriesQuery } from '../../hooks/catalog/useCatalogQueries';
import { mapProductListItem } from '../../utils/productMapping';
import { useToast } from '../../components/Toast/useToast';
import { useAuthModal } from '../../auth/AuthModalContext';
import { useAuth } from '../../auth/useAuth';

const PRODUCT_PAGE_SIZE = 100;
const PRODUCT_CATEGORY_TITLES: Record<string, string> = {
  'ao-dai-truyen-thong': 'Áo dài truyền thống',
  'ao-dai-cach-tan': 'Áo dài cách tân',
  'ao-gami': 'Áo Gấm',
  'ao-dui-hoi-nu': 'Áo hội nữ',
  'ao-dui-hoi-nam': 'Áo hội nam',
};

function getSizeParamKey(categorySlug: string) {
  return `size_${categorySlug}`;
}

function isValidSize(size: string | null): size is (typeof SIZES)[number] {
  return SIZES.includes(size as (typeof SIZES)[number]);
}

export default function ProductsPage() {
  const location = useLocation();
  const navigate = useNavigate();
  const { status } = useAuth();
  const { openAuthModal } = useAuthModal();
  const { showToast } = useToast();
  const searchParams = useMemo(() => new URLSearchParams(location.search), [location.search]);
  const activeCategorySlug = searchParams.get('category');
  const loadingBannerTitle = activeCategorySlug
    ? PRODUCT_CATEGORY_TITLES[activeCategorySlug] ?? null
    : null;
  const selectedSizesByCategory = useMemo(() => {
    const selectedSizes = new Map<string, string>();

    searchParams.forEach((value, key) => {
      if (key.startsWith('size_') && isValidSize(value)) {
        selectedSizes.set(key.replace('size_', ''), value);
      }
    });

    return selectedSizes;
  }, [searchParams]);
  const headerCategoriesQuery = useHeaderCategoriesQuery();
  const aoDaiCategory = headerCategoriesQuery.data?.find((category) => category.slug === 'ao-dai');
  const childCategories = aoDaiCategory?.children ?? [];
  const visibleCategories = activeCategorySlug
    ? childCategories.filter((category) => category.slug === activeCategorySlug)
    : childCategories;
  const productQueries = useCategoryProductsQueries(visibleCategories, (category) => ({
    categorySlug: category.slug,
    size: selectedSizesByCategory.get(category.slug) ?? undefined,
    page: 1,
    pageSize: PRODUCT_PAGE_SIZE,
  }));
  const addCartItemMutation = useAddCartItemMutation();
  const loading = headerCategoriesQuery.isPending || productQueries.some((query) => query.isPending);
  const firstError = headerCategoriesQuery.error ?? productQueries.find((query) => query.error)?.error;
  const loadError = firstError instanceof Error ? firstError.message : null;
  const categories: Category[] = visibleCategories.map((category, index) => ({
    id: category.slug,
    name: category.name,
    products: productQueries[index]?.data?.data.map(mapProductListItem) ?? [],
  }));

  const handleSelectSize = (categorySlug: string, size: string | null) => {
    const nextParams = new URLSearchParams(location.search);
    const sizeParamKey = getSizeParamKey(categorySlug);

    if (size) {
      nextParams.set(sizeParamKey, size);
    } else {
      nextParams.delete(sizeParamKey);
    }

    const query = nextParams.toString();
    navigate(query ? `/products?${query}` : '/products');
  };

  const handleAddToCart = async (product: Product) => {
    if (status !== 'authenticated') {
      openAuthModal({ from: location.pathname + location.search });
      return;
    }

    if (!product.variantId) {
      showToast('Sản phẩm này hiện chưa sẵn sàng để thêm vào giỏ.', 'error');
      return;
    }

    try {
      await addCartItemMutation.mutateAsync({ variantId: product.variantId, quantity: 1 });
      showToast('Đã thêm sản phẩm vào giỏ hàng.');
    } catch (error) {
      showToast(error instanceof Error ? error.message : 'Không thể thêm vào giỏ hàng.', 'error');
    }
  };

  return (
    <main className={styles.page}>
      {loadError ? (
        <div className={styles.statusMessage}>
          <p>Không thể tải danh sách sản phẩm.</p>
          <p className={styles.errorDetail}>{loadError}</p>
        </div>
      ) : null}
      {loading ? (
        <div className={styles.loadingContainer}>
          {loadingBannerTitle ? <CategoryBanner title={loadingBannerTitle} /> : null}
          <div className={styles.spinner} aria-label="Đang tải" />
        </div>
      ) : null}
      {!loading && !loadError && categories.length === 0 ? (
        <div className={styles.emptyState}>
          <p>Không có sản phẩm phù hợp với size đã chọn.</p>
          <Link className={styles.resetLink} to="/products">
            Xem tất cả sản phẩm
          </Link>
        </div>
      ) : null}
      {!loading && categories.map((category) => (
        <div key={category.id}>
          <CategoryBanner title={category.name} />
          <motion.section
            className={styles.productSection}
            variants={sectionReveal}
            initial="hidden"
            whileInView="show"
            viewport={viewportOnce}
          >
            <div className={styles.filterBar}>
              <SizeDropdown
                selected={selectedSizesByCategory.get(category.id) ?? null}
                onSelect={(size) => handleSelectSize(category.id, size)}
              />
            </div>
            {category.products.length === 0 ? (
              <div className={styles.categoryEmptyState}>
                <p>Không có sản phẩm phù hợp với size đã chọn trong danh mục này.</p>
                <Link className={styles.resetLink} to="/products">
                  Xem tất cả sản phẩm
                </Link>
              </div>
            ) : (
              <motion.div
                className={styles.productGrid}
                variants={staggerContainer}
              >
                {category.products.map((product) => (
                  <ProductCard key={product.id} data={product} onAddToCart={handleAddToCart} />
                ))}
              </motion.div>
            )}
          </motion.section>
        </div>
      ))}
    </main>
  );
}

interface SizeDropdownProps {
  selected: string | null;
  onSelect: (size: string | null) => void;
}

function SizeDropdown({ selected, onSelect }: SizeDropdownProps) {
  const [open, setOpen] = useState(false);

  return (
    <div className={styles.dropdown}>
      <button
        className={styles.dropdownToggle}
        onClick={() => setOpen(!open)}
        type="button"
      >
        {selected ?? 'Chọn size'}
        <svg width="10" height="6" viewBox="0 0 10 6" fill="none" aria-hidden="true" role="img">
          <path d="M1 1l4 4 4-4" stroke="currentColor" strokeWidth="1.2" strokeLinecap="round" strokeLinejoin="round"/>
        </svg>
      </button>
      {open && (
        <ul className={styles.dropdownMenu}>
          <li>
            <button
              type="button"
              className={`${styles.dropdownOption} ${selected === null ? styles.active : ''}`}
              onClick={() => { onSelect(null); setOpen(false); }}
            >
              Tất cả size
            </button>
          </li>
          {SIZES.map((size) => (
            <li key={size}>
              <button
                type="button"
                className={`${styles.dropdownOption} ${selected === size ? styles.active : ''}`}
                onClick={() => { onSelect(size); setOpen(false); }}
              >
                {size}
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
