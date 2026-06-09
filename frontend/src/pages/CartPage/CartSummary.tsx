import { useState } from 'react';
import styles from './CartSummary.module.css';
import type { UserAddress } from '../../types/address';
import { formatCurrency } from './currency';
import { trackEvent } from '../../api/events';
import { validatePromo } from '../../api/checkout';

interface CartSummaryProps {
  subtotal: number;
  shippingFee: number;
  totalItemCount: number;
  addresses: UserAddress[];
  selectedAddressId: string | null;
  onSelectAddress: (addressId: string) => void;
  onCheckout: () => void;
  checkingOut: boolean;
  disabled: boolean;
  appliedPromoCode: string | null;
  discountAmount: number;
  discountLabel: string | null;
  promoFreeShipping: boolean;
  onPromoApplied: (code: string, discountAmount: number, discountLabel: string, freeShipping: boolean) => void;
  onPromoCleared: () => void;
}

export default function CartSummary({
  subtotal,
  shippingFee,
  totalItemCount,
  addresses,
  selectedAddressId,
  onSelectAddress,
  onCheckout,
  checkingOut,
  disabled,
  appliedPromoCode,
  discountAmount,
  discountLabel,
  promoFreeShipping,
  onPromoApplied,
  onPromoCleared,
}: CartSummaryProps) {
  const [promoInput, setPromoInput] = useState('');
  const [promoLoading, setPromoLoading] = useState(false);
  const [promoError, setPromoError] = useState<string | null>(null);

  const effectiveShipping = promoFreeShipping ? 0 : shippingFee;
  const total = subtotal - discountAmount + effectiveShipping;

  async function handleApplyPromo() {
    if (!promoInput.trim()) return;
    setPromoLoading(true);
    setPromoError(null);
    try {
      const result = await validatePromo(promoInput.trim(), subtotal);
      void trackEvent({ eventType: 'promo_validated', metadata: { code: promoInput.trim(), subtotal, isValid: result.isValid } });
      if (result.isValid) {
        void trackEvent({ eventType: 'promo_applied', metadata: { code: promoInput.trim(), discountAmount: result.discountAmount, freeShipping: result.freeShipping } });
        onPromoApplied(promoInput.trim(), result.discountAmount, result.discountLabel ?? 'Giảm giá', result.freeShipping);
      } else {
        setPromoError(result.errorMessage ?? 'Mã không hợp lệ.');
      }
    } catch (err) {
      setPromoError(err instanceof Error ? err.message : 'Không thể kiểm tra mã.');
    } finally {
      setPromoLoading(false);
    }
  }

  return (
    <div className={styles.sidebar}>
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
          {appliedPromoCode ? (
            <div className={styles.promoApplied}>
              <span className={styles.promoAppliedCode}>{appliedPromoCode}</span>
              <span className={styles.promoAppliedLabel}>{discountLabel}</span>
              <button className={styles.promoRemoveBtn} type="button" onClick={onPromoCleared}>×</button>
            </div>
          ) : (
            <>
              <div className={styles.promoRow}>
                <input
                  id="cart-promo-input"
                  className={styles.promoInput}
                  type="text"
                  placeholder="Nhập mã"
                  value={promoInput}
                  onChange={(e) => setPromoInput(e.target.value)}
                  onKeyDown={(e) => e.key === 'Enter' && handleApplyPromo()}
                  disabled={promoLoading}
                />
                <button
                  className={styles.promoBtn}
                  type="button"
                  onClick={handleApplyPromo}
                  disabled={promoLoading || !promoInput.trim()}
                >
                  {promoLoading ? '...' : 'Áp dụng'}
                </button>
              </div>
              {promoError && <p className={styles.promoError}>{promoError}</p>}
              <p className={styles.promoHint}>Nhập mã giảm giá nếu bạn có.</p>
            </>
          )}
        </div>
      </div>

      <div className={styles.card}>
        <div className={styles.cardHeader}>
          <h3 className={styles.summaryTitle}>Tổng đơn hàng</h3>
        </div>
        <div className={styles.cardBody}>
          <div className={styles.priceRow}>
            <span>Giá tiền ({totalItemCount} sản phẩm)</span>
            <span className={styles.priceValue}>{formatCurrency(subtotal)}</span>
          </div>
          {discountAmount > 0 && (
            <div className={styles.priceRow}>
              <span>Giảm giá{discountLabel ? ` (${discountLabel})` : ''}</span>
              <span className={styles.discountValue}>-{formatCurrency(discountAmount)}</span>
            </div>
          )}
          <div className={styles.priceRow}>
            <span>Phí vận chuyển</span>
            <span className={promoFreeShipping ? styles.freeShippingValue : styles.priceValueSmall}>
              {promoFreeShipping ? 'Miễn phí' : formatCurrency(shippingFee)}
            </span>
          </div>
          <div className={styles.divider} />
          <div className={styles.totalRow}>
            <span>Tổng</span>
            <span className={styles.totalValue}>{formatCurrency(total)}</span>
          </div>
          <label className={styles.cardTitle} htmlFor="cart-address-select">Địa chỉ nhận hàng</label>
          <select
            id="cart-address-select"
            className={styles.promoInput}
            value={selectedAddressId ?? ''}
            onChange={(event) => onSelectAddress(event.target.value)}
          >
            <option value="" disabled>Chọn địa chỉ</option>
            {addresses.map((address) => (
              <option key={address.id} value={address.id}>
                {address.recipientName} - {address.addressLine}, {address.district}
              </option>
            ))}
          </select>
          <button className={styles.checkoutBtn} onClick={onCheckout} type="button" disabled={disabled || checkingOut}>
            <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
              <path d="M1.33 3.33h13.34" stroke="#fff" strokeWidth="1.3" strokeLinecap="round" />
              <path d="M2 3.33l1.17 10a1.33 1.33 0 001.33 1.17h7a1.33 1.33 0 001.33-1.17L14 3.33" stroke="#fff" strokeWidth="1.3" strokeLinecap="round" />
            </svg>
            {checkingOut ? 'Đang thanh toán...' : 'Thanh toán'}
          </button>
        </div>
      </div>

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
