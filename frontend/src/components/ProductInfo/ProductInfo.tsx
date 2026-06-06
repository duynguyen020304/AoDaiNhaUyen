import { useState, useCallback } from 'react';
import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import StarRating from '../StarRating/StarRating';
import { fadeUp, staggerContainer, cardReveal } from '../../utils/motion';
import type { ProductDetail, ProductVariant } from '../../types/catalog';
import styles from './ProductInfo.module.css';

interface ProductInfoProps {
  product: ProductDetail;
}

function getDefaultVariant(variants: ProductVariant[]): ProductVariant {
  const def = variants.find((v) => v.isDefault);
  return def ?? variants[0];
}

function getDisplayPrice(variant: ProductVariant): { current: number; original: number | null } {
  if (variant.salePrice != null && variant.salePrice < variant.price) {
    return { current: variant.salePrice, original: variant.price };
  }
  return { current: variant.price, original: null };
}

function formatPrice(amount: number): string {
  return amount.toLocaleString('vi-VN') + '₫';
}



export default function ProductInfo({ product }: ProductInfoProps) {
  const [selectedVariant, setSelectedVariant] = useState<ProductVariant>(() =>
    getDefaultVariant(product.variants),
  );
  const [quantity, setQuantity] = useState(1);

  const price = getDisplayPrice(selectedVariant);
  const maxQty = selectedVariant.stockQty;

  const uniqueSizes = (() => {
    const seen = new Set<string>();
    return product.variants
      .filter((v) => v.size != null && v.size !== '')
      .filter((v) => {
        if (seen.has(v.size!)) return false;
        seen.add(v.size!);
        return true;
      });
  })();

  const colors = (() => {
    const seen = new Set<string>();
    return product.variants
      .filter((v) => v.color != null && v.color !== '')
      .filter((v) => {
        if (seen.has(v.color!)) return false;
        seen.add(v.color!);
        return true;
      });
  })();

  const handleSizeSelect = useCallback(
    (size: string) => {
      const match = product.variants.find(
        (v) =>
          v.size === size &&
          (selectedVariant.color == null || v.color === selectedVariant.color),
      );
      if (match) {
        setSelectedVariant(match);
        if (quantity > match.stockQty) setQuantity(Math.max(1, match.stockQty));
      }
    },
    [product.variants, selectedVariant.color, quantity],
  );

  const handleColorSelect = useCallback(
    (color: string) => {
      const match = product.variants.find(
        (v) =>
          v.color === color &&
          (selectedVariant.size == null || v.size === selectedVariant.size),
      );
      if (match) {
        setSelectedVariant(match);
        if (quantity > match.stockQty) setQuantity(Math.max(1, match.stockQty));
      }
    },
    [product.variants, selectedVariant.size, quantity],
  );

  const incrementQty = useCallback(() => {
    setQuantity((q) => Math.min(q + 1, maxQty));
  }, [maxQty]);

  const decrementQty = useCallback(() => {
    setQuantity((q) => Math.max(1, q - 1));
  }, []);

  const inStock = maxQty > 0;

  return (
    <motion.div
      className={styles.container}
      variants={staggerContainer}
      initial="hidden"
      animate="show"
    >
      {/* Breadcrumb */}
      <motion.nav className={styles.breadcrumb} variants={fadeUp} aria-label="Breadcrumb">
        <Link to="/" className={styles.breadcrumbLink}>
          Trang chủ
        </Link>
        <span className={styles.breadcrumbSep}>/</span>
        <Link
          to={`/danh-muc/${product.categorySlug}`}
          className={styles.breadcrumbLink}
        >
          {product.categoryName}
        </Link>
        <span className={styles.breadcrumbSep}>/</span>
        <span className={styles.breadcrumbCurrent}>{product.name}</span>
      </motion.nav>

      {/* Product name */}
      <motion.h1 className={styles.name} variants={fadeUp}>
        {product.name}
      </motion.h1>

      {/* Star rating */}
      {product.reviewSummary && (
        <motion.div className={styles.ratingRow} variants={fadeUp}>
          <StarRating rating={product.reviewSummary.averageRating} showValue />
          <a href="#reviews" className={styles.reviewCount}>
            ({product.reviewSummary.totalReviews} đánh giá)
          </a>
        </motion.div>
      )}

      {/* Price */}
      <motion.div className={styles.priceRow} variants={fadeUp}>
        <span className={styles.currentPrice}>{formatPrice(price.current)}</span>
        {price.original != null && (
          <span className={styles.originalPrice}>{formatPrice(price.original)}</span>
        )}
      </motion.div>

      {/* Variant selector */}
      {uniqueSizes.length > 0 && (
        <motion.div className={styles.variantGroup} variants={fadeUp}>
          <span className={styles.variantLabel}>Kích thước:</span>
          <div className={styles.sizeOptions}>
            {uniqueSizes.map((v) => (
              <button
                key={v.size}
                type="button"
                className={`${styles.sizeBtn} ${
                  selectedVariant.size === v.size ? styles.sizeBtnActive : ''
                }`}
                onClick={() => handleSizeSelect(v.size!)}
              >
                {v.size}
              </button>
            ))}
          </div>
        </motion.div>
      )}

      {colors.length > 0 && (
        <motion.div className={styles.variantGroup} variants={fadeUp}>
          <span className={styles.variantLabel}>Màu sắc:</span>
          <div className={styles.colorOptions}>
            {colors.map((v) => (
              <button
                key={v.color}
                type="button"
                className={`${styles.colorSwatch} ${
                  selectedVariant.color === v.color ? styles.colorSwatchActive : ''
                }`}
                title={v.color!}
                onClick={() => handleColorSelect(v.color!)}
              >
                <span
                  className={styles.colorSwatchInner}
                  style={{ backgroundColor: v.color!.toLowerCase() }}
                />
              </button>
            ))}
          </div>
        </motion.div>
      )}

      {/* Stock status */}
      <motion.div className={styles.stockRow} variants={fadeUp}>
        {inStock ? (
          <span className={styles.inStock}>Còn hàng</span>
        ) : (
          <span className={styles.outOfStock}>Hết hàng</span>
        )}
      </motion.div>

      {/* Quantity */}
      <motion.div className={styles.quantityRow} variants={fadeUp}>
        <span className={styles.variantLabel}>Số lượng:</span>
        <div className={styles.quantityControls}>
          <button
            type="button"
            className={styles.qtyBtn}
            onClick={decrementQty}
            disabled={quantity <= 1}
            aria-label="Giảm số lượng"
          >
            −
          </button>
          <span className={styles.qtyValue}>{quantity}</span>
          <button
            type="button"
            className={styles.qtyBtn}
            onClick={incrementQty}
            disabled={quantity >= maxQty}
            aria-label="Tăng số lượng"
          >
            +
          </button>
        </div>
      </motion.div>

      {/* Add to cart */}
      <motion.button
        type="button"
        className={styles.addToCartBtn}
        variants={cardReveal}
        disabled={!inStock}
        whileTap={inStock ? { scale: 0.97 } : undefined}
      >
        Thêm vào giỏ hàng
      </motion.button>

      {/* Product details accordion */}
      <motion.div className={styles.accordion} variants={fadeUp}>
        <details className={styles.detailsSection} open>
          <summary className={styles.detailsSummary}>Mô tả</summary>
          <div className={styles.detailsContent}>
            {product.description ? (
              <p className={styles.descriptionText}>{product.description}</p>
            ) : (
              <p className={styles.noContent}>Chưa có mô tả cho sản phẩm này.</p>
            )}
          </div>
        </details>

        <details className={styles.detailsSection}>
          <summary className={styles.detailsSummary}>Chi tiết sản phẩm</summary>
          <div className={styles.detailsContent}>
            <dl className={styles.specGrid}>
              {product.material && (
                <>
                  <dt className={styles.specLabel}>Chất liệu</dt>
                  <dd className={styles.specValue}>{product.material}</dd>
                </>
              )}
              {product.brand && (
                <>
                  <dt className={styles.specLabel}>Thương hiệu</dt>
                  <dd className={styles.specValue}>{product.brand}</dd>
                </>
              )}
              {product.origin && (
                <>
                  <dt className={styles.specLabel}>Xuất xứ</dt>
                  <dd className={styles.specValue}>{product.origin}</dd>
                </>
              )}
              {product.careInstruction && (
                <>
                  <dt className={styles.specLabel}>Hướng dẫn bảo quản</dt>
                  <dd className={styles.specValue}>{product.careInstruction}</dd>
                </>
              )}
            </dl>
          </div>
        </details>
      </motion.div>
    </motion.div>
  );
}
