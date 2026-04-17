import { useEffect, useMemo, useState } from 'react';
import { motion } from 'framer-motion';
import { useLocation } from 'react-router-dom';
import { getHeaderCategories, getProducts } from '../../api/catalog';
import { resolveAssetUrl } from '../../api/client';
import CategoryBanner from '../../components/CategoryBanner/CategoryBanner';
import ProductCard from '../../components/ProductCard/ProductCard';
import { sectionReveal, staggerContainer, viewportOnce } from '../../utils/motion';
import type { HeaderCategoryChild, ProductListItem } from '../../types/catalog';
import type { Badge, Category, Product } from '../ProductsPage/data';
import styles from './AccessoriesPage.module.css';

const ACCESSORY_PAGE_SIZE = 100;

const vndFormatter = new Intl.NumberFormat('vi-VN', {
  style: 'currency',
  currency: 'VND',
  maximumFractionDigits: 0,
});

const fallbackCategories: Category[] = [
  {
    id: 'phu-kien',
    name: 'Phụ kiện',
    products: [],
  },
];

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
  return {
    id: String(product.id),
    name: product.name,
    image: resolveAssetUrl(product.primaryImageUrl) ?? '/assets/products/product-truyen-thong-1.png',
    badge: getBadge(product, index),
    reviews: 28 + ((product.id * 5) % 31),
    price: formatPrice(product.salePrice ?? product.price),
    originalPrice: product.salePrice ? formatPrice(product.price) : undefined,
  };
}

export default function AccessoriesPage() {
  const location = useLocation();
  const activeCategorySlug = useMemo(() => {
    return new URLSearchParams(location.search).get('category');
  }, [location.search]);
  const [categories, setCategories] = useState<Category[]>(fallbackCategories);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  useEffect(() => {
    let ignore = false;

    async function loadAccessories() {
      setLoading(true);
      setLoadError(null);

      try {
        const headerCategories = await getHeaderCategories();
        const accessoryCategory = headerCategories.find((category) => category.slug === 'phu-kien');
        const childCategories = accessoryCategory?.children ?? [];
        const visibleCategories = activeCategorySlug
          ? childCategories.filter((category) => category.slug === activeCategorySlug)
          : childCategories;

        if (visibleCategories.length === 0) {
          if (!ignore) {
            setCategories(fallbackCategories);
          }
          return;
        }

        const groups = await Promise.all(
          visibleCategories.map(async (category: HeaderCategoryChild) => {
            const result = await getProducts({
              categorySlug: category.slug,
              page: 1,
              pageSize: ACCESSORY_PAGE_SIZE,
            });

            return {
              id: category.slug,
              name: category.name,
              products: result.items.map(mapProduct),
            };
          }),
        );

        if (!ignore) {
          setCategories(groups.filter((category) => category.products.length > 0));
        }
      } catch (error) {
        if (!ignore) {
          setCategories(fallbackCategories);
          setLoadError(error instanceof Error ? error.message : 'Không thể tải phụ kiện.');
        }
      } finally {
        if (!ignore) {
          setLoading(false);
        }
      }
    }

    loadAccessories();

    return () => {
      ignore = true;
    };
  }, [activeCategorySlug]);

  return (
    <main className={styles.page}>
      {loadError ? (
        <p className={styles.statusMessage}>
          Chưa tải được dữ liệu phụ kiện. {loadError}
        </p>
      ) : null}
      {loading ? <p className={styles.statusMessage}>Đang tải phụ kiện...</p> : null}
      {!loading && categories.length === 0 ? (
        <p className={styles.emptyMessage}>Chưa có phụ kiện trong danh mục này.</p>
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
            <motion.div className={styles.productGrid} variants={staggerContainer}>
              {category.products.map((product) => (
                <ProductCard key={product.id} data={product} />
              ))}
            </motion.div>
          </motion.section>
        </div>
      ))}
    </main>
  );
}
