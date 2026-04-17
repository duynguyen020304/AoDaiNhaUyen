import { useEffect, useMemo, useState } from 'react';
import { motion } from 'framer-motion';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import styles from './ProductsPage.module.css';
import { sectionReveal, staggerContainer, viewportOnce } from '../../utils/motion';
import { CATEGORIES, SIZES, type Badge, type Category, type Product } from './data';
import CategoryBanner from '../../components/CategoryBanner/CategoryBanner';
import ProductCard from '../../components/ProductCard/ProductCard';
import { getHeaderCategories, getProducts } from '../../api/catalog';
import { resolveAssetUrl } from '../../api/client';
import type { HeaderCategoryChild, ProductListItem } from '../../types/catalog';

const PRODUCT_PAGE_SIZE = 100;

const vndFormatter = new Intl.NumberFormat('vi-VN', {
  style: 'currency',
  currency: 'VND',
  maximumFractionDigits: 0,
});

function formatPrice(value: number) {
  return vndFormatter.format(value).replace('₫', 'đ');
}

function getBadge(product: ProductListItem, index: number): Badge | undefined {
  if (product.isFeatured) {
    return index % 2 === 0 ? 'HOT' : 'BÁN CHẠY';
  }

  return product.status.toLowerCase() === 'active' && index < 2 ? 'MỚI' : undefined;
}

function mapProduct(product: ProductListItem, index: number): Product {
  const price = product.salePrice ?? product.price;
  const image = resolveAssetUrl(product.primaryImageUrl) ?? '/assets/products/product-truyen-thong-1.png';

  return {
    id: String(product.id),
    name: product.name,
    image,
    badge: getBadge(product, index),
    reviews: 32 + ((product.id * 7) % 28),
    price: formatPrice(price),
    originalPrice: product.salePrice ? formatPrice(product.price) : undefined,
  };
}

function getSizeParamKey(categorySlug: string) {
  return `size_${categorySlug}`;
}

function isValidSize(size: string | null): size is (typeof SIZES)[number] {
  return SIZES.includes(size as (typeof SIZES)[number]);
}

export default function ProductsPage() {
  const location = useLocation();
  const navigate = useNavigate();
  const searchParams = useMemo(() => new URLSearchParams(location.search), [location.search]);
  const activeCategorySlug = searchParams.get('category');
  const selectedSizesByCategory = useMemo(() => {
    const selectedSizes = new Map<string, string>();

    searchParams.forEach((value, key) => {
      if (key.startsWith('size_') && isValidSize(value)) {
        selectedSizes.set(key.replace('size_', ''), value);
      }
    });

    return selectedSizes;
  }, [searchParams]);
  const [categories, setCategories] = useState<Category[]>(CATEGORIES);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  const hasSelectedSize = selectedSizesByCategory.size > 0;

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

  useEffect(() => {
    let ignore = false;

    async function loadProducts() {
      setLoading(true);
      setLoadError(null);

      try {
        const headerCategories = await getHeaderCategories();
        const aoDaiCategory = headerCategories.find((category) => category.slug === 'ao-dai');
        const childCategories = aoDaiCategory?.children ?? [];
        const visibleCategories = activeCategorySlug
          ? childCategories.filter((category) => category.slug === activeCategorySlug)
          : childCategories;

        if (visibleCategories.length === 0) {
          if (!ignore) {
            setCategories(CATEGORIES);
          }
          return;
        }

        const groups = await Promise.all(
          visibleCategories.map(async (category: HeaderCategoryChild) => {
            const selectedSize = selectedSizesByCategory.get(category.slug);
            const result = await getProducts({
              categorySlug: category.slug,
              size: selectedSize ?? undefined,
              page: 1,
              pageSize: PRODUCT_PAGE_SIZE,
            });

            return {
              id: category.slug,
              name: category.name,
              products: result.items.map(mapProduct),
            };
          }),
        );

        if (!ignore) {
          const hasProducts = groups.some((category) => category.products.length > 0);
          setCategories(hasProducts || hasSelectedSize ? groups : CATEGORIES);
        }
      } catch (error) {
        if (!ignore) {
          setCategories(CATEGORIES);
          setLoadError(error instanceof Error ? error.message : 'Không thể tải sản phẩm.');
        }
      } finally {
        if (!ignore) {
          setLoading(false);
        }
      }
    }

    loadProducts();

    return () => {
      ignore = true;
    };
  }, [activeCategorySlug, hasSelectedSize, selectedSizesByCategory]);

  return (
    <main className={styles.page}>
      {loadError ? (
        <p className={styles.statusMessage}>
          Đang hiển thị dữ liệu mẫu. {loadError}
        </p>
      ) : null}
      {loading ? <p className={styles.statusMessage}>Đang tải sản phẩm...</p> : null}
      {!loading && categories.length === 0 ? (
        <div className={styles.emptyState}>
          <p>Không có sản phẩm phù hợp với size đã chọn.</p>
          <Link className={styles.resetLink} to="/products">
            Xem tất cả sản phẩm
          </Link>
        </div>
      ) : null}
      {categories.map((category) => (
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
                  <ProductCard key={product.id} data={product} />
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
