import { useMemo } from 'react';
import { motion } from 'framer-motion';
import { useLocation } from 'react-router-dom';
import { useAddCartItemMutation } from '../../hooks/cart/useCartMutations';
import { useCategoryProductsQueries, useHeaderCategoriesQuery } from '../../hooks/catalog/useCatalogQueries';
import { mapProductListItem } from '../../utils/productMapping';
import CategoryBanner from '../../components/CategoryBanner/CategoryBanner';
import ProductCard from '../../components/ProductCard/ProductCard';
import { useToast } from '../../components/Toast/useToast';
import { useAuthModal } from '../../auth/AuthModalContext';
import { useAuth } from '../../auth/useAuth';
import { sectionReveal, staggerContainer, viewportOnce } from '../../utils/motion';
import type { Category, Product } from '../ProductsPage/data';
import styles from './AccessoriesPage.module.css';

const ACCESSORY_PAGE_SIZE = 100;
const ACCESSORY_CATEGORY_TITLES: Record<string, string> = {
  'tram-cai': 'Trâm cài',
  'tui-sach': 'Túi sách',
  quat: 'Quạt',
  giay: 'Giày',
};

export default function AccessoriesPage() {
  const location = useLocation();
  const { status } = useAuth();
  const { openAuthModal } = useAuthModal();
  const { showToast } = useToast();
  const activeCategorySlug = useMemo(() => {
    return new URLSearchParams(location.search).get('category');
  }, [location.search]);
  const loadingBannerTitle = activeCategorySlug
    ? ACCESSORY_CATEGORY_TITLES[activeCategorySlug] ?? null
    : null;
  const headerCategoriesQuery = useHeaderCategoriesQuery();
  const accessoryCategory = headerCategoriesQuery.data?.find((category) => category.slug === 'phu-kien');
  const childCategories = accessoryCategory?.children ?? [];
  const visibleCategories = activeCategorySlug
    ? childCategories.filter((category) => category.slug === activeCategorySlug)
    : childCategories;
  const productQueries = useCategoryProductsQueries(visibleCategories, (category) => ({
    categorySlug: category.slug,
    page: 1,
    pageSize: ACCESSORY_PAGE_SIZE,
  }));
  const addCartItemMutation = useAddCartItemMutation();
  const loading = headerCategoriesQuery.isPending || productQueries.some((query) => query.isPending);
  const firstError = headerCategoriesQuery.error ?? productQueries.find((query) => query.error)?.error;
  const loadError = firstError instanceof Error ? firstError.message : null;
  const categories: Category[] = visibleCategories.map((category, index) => ({
    id: category.slug,
    name: category.name,
    products: productQueries[index]?.data?.data.map(mapProductListItem) ?? [],
  })).filter((category) => category.products.length > 0);

  const handleAddToCart = async (product: Product) => {
    if (status !== 'authenticated') {
      openAuthModal({ from: location.pathname + location.search });
      return;
    }

    if (!product.variantId) {
      showToast('Phụ kiện này hiện chưa sẵn sàng để thêm vào giỏ.', 'error');
      return;
    }

    try {
      await addCartItemMutation.mutateAsync({ variantId: product.variantId, quantity: 1 });
      showToast('Đã thêm phụ kiện vào giỏ hàng.');
    } catch (error) {
      showToast(error instanceof Error ? error.message : 'Không thể thêm vào giỏ hàng.', 'error');
    }
  };

  return (
    <main className={styles.page}>
      {loadError ? (
        <div className={styles.statusMessage}>
          <p>Không thể tải danh sách phụ kiện.</p>
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
        <p className={styles.emptyMessage}>Chưa có phụ kiện trong danh mục này.</p>
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
            <motion.div className={styles.productGrid} variants={staggerContainer}>
              {category.products.map((product) => (
                <ProductCard key={product.id} data={product} onAddToCart={handleAddToCart} />
              ))}
            </motion.div>
          </motion.section>
        </div>
      ))}
    </main>
  );
}
