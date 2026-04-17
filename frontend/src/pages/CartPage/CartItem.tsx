import styles from './CartItem.module.css';
import type { CartItemData } from './data';

interface CartItemProps {
  item: CartItemData;
}

export default function CartItem({ item }: CartItemProps) {
  return (
    <div className={styles.cartItem}>
      <div className={styles.imageWrapper}>
        <img src={item.image} alt={item.name} className={styles.productImage} />
      </div>
      <div className={styles.itemDetails}>
        <div className={styles.topRow}>
          <div className={styles.info}>
            <h3 className={styles.productName}>{item.name}</h3>
            <div className={styles.variantRow}>
              <span className={styles.variant}>{item.variant}</span>
            </div>
          </div>
          <div className={styles.priceCol}>
            <span className={styles.originalPrice}>{item.originalPrice}</span>
            <span className={styles.price}>{item.price}</span>
          </div>
        </div>
        <div className={styles.bottomRow}>
          <div className={styles.qtyControls}>
            <button className={styles.qtyBtn} aria-label="Giảm số lượng">
              <svg width="14" height="14" viewBox="0 0 14 14" fill="none">
                <path d="M3 7h8" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
              </svg>
            </button>
            <div className={styles.qtyInput}>{item.quantity}</div>
            <button className={styles.qtyBtn} aria-label="Tăng số lượng">
              <svg width="14" height="14" viewBox="0 0 14 14" fill="none">
                <path d="M7 3v8M3 7h8" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
              </svg>
            </button>
          </div>
          <button className={styles.removeBtn}>
            <svg width="14" height="14" viewBox="0 0 14 14" fill="none">
              <path d="M2.33 3.5h9.34" stroke="currentColor" strokeWidth="1.3" strokeLinecap="round" />
              <path d="M5.25 5.83v3.5M8.17 5.83v3.5" stroke="currentColor" strokeWidth="1.3" strokeLinecap="round" />
              <path d="M3.5 3.5l.65 7.58a1.17 1.17 0 001.16 1.09h3.38a1.17 1.17 0 001.16-1.09L10.5 3.5" stroke="currentColor" strokeWidth="1.3" strokeLinecap="round" />
              <path d="M5.25 3.5V2.33a.58.58 0 01.58-.59h1.75a.58.58 0 01.59.59V3.5" stroke="currentColor" strokeWidth="1.3" strokeLinecap="round" />
            </svg>
            <span>Remove</span>
          </button>
        </div>
      </div>
    </div>
  );
}
