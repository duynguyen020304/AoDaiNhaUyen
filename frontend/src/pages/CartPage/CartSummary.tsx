import styles from './CartSummary.module.css';

export default function CartSummary() {
  return (
    <div className={styles.sidebar}>
      {/* Promo Code Card */}
      <div className={styles.card}>
        <div className={styles.cardHeader}>
          <svg width="17.5" height="17.5" viewBox="0 0 17.5 17.5" fill="none">
            <path
              d="M15.31 2.19L10.21 15.31a.44.44 0 01-.82.02L7.25 10.25 2.17 8.11a.44.44 0 01.02-.82L15.31 2.19z"
              stroke="#0A0A0A"
              strokeWidth="1.3"
              strokeLinecap="round"
              strokeLinejoin="round"
            />
          </svg>
          <h3 className={styles.cardTitle}>Mã giảm giá</h3>
        </div>
        <div className={styles.cardBody}>
          <div className={styles.promoRow}>
            <input
              className={styles.promoInput}
              type="text"
              placeholder="Nhập mã"
            />
            <button className={styles.promoBtn}>Nhập</button>
          </div>
          <p className={styles.promoHint}>Ví dụ: NHAUYEN25, DAILE15,...</p>
        </div>
      </div>

      {/* Order Summary Card */}
      <div className={styles.card}>
        <div className={styles.cardHeader}>
          <h3 className={styles.summaryTitle}>Tổng đơn hàng</h3>
        </div>
        <div className={styles.cardBody}>
          <div className={styles.priceRow}>
            <span>Giá tiền (1 sản phẩm)</span>
            <span className={styles.priceValue}>600.000</span>
          </div>
          <div className={styles.priceRow}>
            <span>Phí vận chuyển</span>
            <span className={styles.priceValueSmall}>25.000</span>
          </div>
          <div className={styles.divider} />
          <div className={styles.totalRow}>
            <span>Tổng</span>
            <span className={styles.totalValue}>625.000</span>
          </div>
          <button className={styles.checkoutBtn}>
            <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
              <path d="M1.33 3.33h13.34" stroke="#fff" strokeWidth="1.3" strokeLinecap="round" />
              <path d="M2 3.33l1.17 10a1.33 1.33 0 001.33 1.17h7a1.33 1.33 0 001.33-1.17L14 3.33" stroke="#fff" strokeWidth="1.3" strokeLinecap="round" />
            </svg>
            Thanh toán
          </button>
        </div>
      </div>

      {/* Shipping Info Card */}
      <div className={styles.card}>
        <div className={styles.shippingList}>
          <div className={styles.shippingItem}>
            <svg width="17.5" height="17.5" viewBox="0 0 17.5 17.5" fill="none">
              <path
                d="M1.46 12.13h8.75M5.1 5.83h5.84a1.46 1.46 0 011.46 1.46v5.1M1.46 7.29v-2.2A2.19 2.19 0 013.65 2.92h3.65a2.19 2.19 0 012.19 2.19v2.18"
                stroke="#0A0A0A"
                strokeWidth="1.2"
                strokeLinecap="round"
                strokeLinejoin="round"
              />
            </svg>
            <div>
              <p className={styles.shippingTitle}>Miễn phí vận chuyển</p>
              <p className={styles.shippingSub}>Cho đơn hàng từ 5 triệu</p>
            </div>
          </div>
          <div className={styles.shippingItem}>
            <svg width="17.5" height="17.5" viewBox="0 0 17.5 17.5" fill="none">
              <path
                d="M8.75 14.58A5.83 5.83 0 108.75 2.92a5.83 5.83 0 000 11.66z"
                stroke="#0A0A0A"
                strokeWidth="1.2"
                strokeLinecap="round"
              />
              <path d="M8.75 5.83v2.92" stroke="#0A0A0A" strokeWidth="1.2" strokeLinecap="round" />
              <circle cx="8.75" cy="11.67" r="0.73" fill="#0A0A0A" />
            </svg>
            <div>
              <p className={styles.shippingTitle}>Bảo mật & Đóng gói</p>
              <p className={styles.shippingSub}>Sản phẩm được đóng gói và bảo mật an toàn</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
