import { motion } from 'framer-motion';
import styles from './ProductCard.module.css';
import { cardReveal, cardHover, viewportOnce } from '../../utils/motion';
import type { Product, Badge } from '../../pages/ProductsPage/data';

const BADGE_COLORS: Record<Badge, string> = {
  'HOT': '#e3302c',
  'MỚI': '#22a06b',
  'BÁN CHẠY': '#e58e26',
};

function StarIcon() {
  return (
    <svg width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden="true" role="img">
      <path d="M8 1.333l2.067 4.18 4.6.67-3.334 3.248.787 4.587L8 11.863 3.88 14.018l.787-4.587L1.333 6.183l4.6-.67z" fill="#FFD400" stroke="#FFD400" strokeWidth="0.5"/>
    </svg>
  );
}

function CartIcon() {
  return (
    <svg width="18" height="18" viewBox="0 0 18 18" fill="none" aria-hidden="true" role="img">
      <path d="M5.667 14.333a.833.833 0 110 1.667.833.833 0 010-1.667M13.667 14.333a.833.833 0 110 1.667.833.833 0 010-1.667M2.5 3h2.167l1.5 8h7.5l1.5-5.333H6" stroke="currentColor" strokeWidth="1.2" strokeLinecap="round" strokeLinejoin="round"/>
    </svg>
  );
}

interface ProductCardProps {
  data: Product;
  onAddToCart: (product: Product) => void;
}

export default function ProductCard({ data, onAddToCart }: ProductCardProps) {
  return (
    <motion.article
      className={styles.card}
      variants={cardReveal}
      initial="hidden"
      whileInView="show"
      viewport={viewportOnce}
      whileHover={cardHover.hover}
      transition={{ duration: 0.24, ease: 'easeOut' }}
    >
      <div className={styles.imageWrap}>
        <img src={data.image} alt={data.name} className={styles.image} loading="lazy" />
        {data.badge && (
          <span className={styles.badge} style={{ background: BADGE_COLORS[data.badge] }}>
            {data.badge}
          </span>
        )}
      </div>
      <div className={styles.info}>
        <h3 className={styles.name}>{data.name}</h3>
        <div className={styles.rating}>
          {[1, 2, 3, 4, 5].map((n) => <StarIcon key={n} />)}
          <span className={styles.reviewCount}>({data.reviews})</span>
        </div>
        <div className={styles.priceRow}>
          <span className={styles.price}>{data.price}</span>
          {data.originalPrice && (
            <span className={styles.originalPrice}>{data.originalPrice}</span>
          )}
        </div>
        <motion.button
          className={styles.cartBtn}
          whileTap={{ scale: 0.96 }}
          onClick={() => onAddToCart(data)}
          type="button"
        >
          <CartIcon />
          Thêm vào giỏ
        </motion.button>
      </div>
    </motion.article>
  );
}
