import { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import styles from './ProductDetailPage.module.css';
import { getProductBySlug } from '../../api/catalog';
import { fadeUp } from '../../utils/motion';
import ImageGallery from '../../components/ImageGallery/ImageGallery';
import ProductInfo from '../../components/ProductInfo/ProductInfo';
import UserFeedbackSection from '../../components/UserFeedbackSection/UserFeedbackSection';
import type { ProductDetail } from '../../types/catalog';

export default function ProductDetailPage() {
  const { slug } = useParams<{ slug: string }>();
  const [product, setProduct] = useState<ProductDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    window.scrollTo({ top: 0, left: 0, behavior: 'instant' });
  }, []);

  useEffect(() => {
    if (!slug) {
      setError('Không tìm thấy sản phẩm.');
      setLoading(false);
      return;
    }

    let cancelled = false;
    setLoading(true);
    setError(null);

    (async () => {
      try {
        const data = await getProductBySlug(slug);
        if (!cancelled) {
          setProduct(data);
          setLoading(false);
        }
      } catch (err) {
        if (!cancelled) {
          setError('Không tìm thấy sản phẩm.');
          setLoading(false);
        }
      }
    })();

    return () => { cancelled = true; };
  }, [slug]);

  /* ── Loading state ── */
  if (loading) {
    return (
      <main className={styles.page}>
        <div className={styles.loadingSkeleton}>
          <div className={styles.skeletonMedia} />
          <div className={styles.skeletonInfo}>
            <div className={styles.skeletonLineLarge} />
            <div className={styles.skeletonLine} style={{ width: '40%' }} />
            <div className={styles.skeletonLine} style={{ width: '30%' }} />
            {[1, 2, 3, 4].map((n) => (
              <div key={n} className={styles.skeletonLine} style={{ width: `${80 + n * 4}%` }} />
            ))}
          </div>
        </div>
      </main>
    );
  }

  /* ── Error state (not found) ── */
  if (error || !product) {
    return (
      <main className={styles.page}>
        <motion.div
          className={styles.notFound}
          initial={{ opacity: 0, y: 24 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.5 }}
        >
          <h1 className={styles.notFoundTitle}>Không tìm thấy sản phẩm</h1>
          <p className={styles.notFoundDesc}>
            Sản phẩm bạn đang tìm kiếm không tồn tại hoặc đã bị gỡ xuống. Vui lòng kiểm tra lại đường dẫn.
          </p>
          <Link to="/" className={styles.backLink}>
            Quay về trang chủ
          </Link>
        </motion.div>
      </main>
    );
  }

  /* ── Success state ── */
  return (
    <motion.main
      className={styles.page}
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      transition={{ duration: 0.35 }}
    >
      {/* Two-column product area */}
      <div className={styles.productLayout} id="product-detail-top">
        <motion.div variants={fadeUp} initial="hidden" animate="show" className={styles.leftCol}>
          <ImageGallery images={product.images} />
        </motion.div>

        <motion.div variants={fadeUp} initial="hidden" animate="show" className={styles.rightCol}>
          <ProductInfo product={product} />
        </motion.div>
      </div>

      <div className={styles.feedbackContainer} id="reviews">
        <UserFeedbackSection
          productId={product.id}
          reviewSummary={product.reviewSummary}
        />
      </div>
    </motion.main>
  );
}
