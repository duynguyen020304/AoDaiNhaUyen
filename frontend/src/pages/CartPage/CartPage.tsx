import { useEffect, useMemo, useState } from 'react';
import { motion } from 'framer-motion';
import { useNavigate } from 'react-router-dom';
import styles from './CartPage.module.css';
import CartItem from './CartItem';
import CustomerNotes from './CustomerNotes';
import CartSummary from './CartSummary';
import { fadeUp, sectionReveal } from '../../utils/motion';
import { getCart, removeCartItem, updateCartItem } from '../../api/cart';
import { checkout } from '../../api/checkout';
import { resolveAssetUrl } from '../../api/client';
import { getAddresses } from '../../api/user';
import type { Cart } from '../../types/cart';
import type { UserAddress } from '../../types/address';
import { useToast } from '../../components/Toast/useToast';
import { useAuth } from '../../auth/useAuth';

export default function CartPage() {
  const navigate = useNavigate();
  const { status } = useAuth();
  const { showToast } = useToast();
  const [cart, setCart] = useState<Cart | null>(null);
  const [addresses, setAddresses] = useState<UserAddress[]>([]);
  const [selectedAddressId, setSelectedAddressId] = useState<number | null>(null);
  const [note, setNote] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [checkingOut, setCheckingOut] = useState(false);
  const [updatingItemId, setUpdatingItemId] = useState<number | null>(null);

  useEffect(() => {
    if (status === 'anonymous') {
      return;
    }

    if (status !== 'authenticated') {
      return;
    }

    Promise.all([getCart(), getAddresses()])
      .then(([cartValue, addressValues]) => {
        setCart({
          ...cartValue,
          items: cartValue.items.map((item) => ({
            ...item,
            imageUrl: resolveAssetUrl(item.imageUrl),
          })),
        });
        setAddresses(addressValues);
        const defaultAddress = addressValues.find((value) => value.isDefault) ?? addressValues[0];
        setSelectedAddressId(defaultAddress?.id ?? null);
      })
      .catch((value: Error) => setError(value.message))
      .finally(() => setLoading(false));
  }, [status]);

  const isLoadingCart = status === 'loading' || (status === 'authenticated' && loading);
  const shippingFee = useMemo(() => (cart && cart.items.length > 0 ? 25000 : 0), [cart]);

  async function handleUpdateQuantity(itemId: number, quantity: number) {
    try {
      setUpdatingItemId(itemId);
      const nextCart = await updateCartItem(itemId, { quantity });
      setCart({
        ...nextCart,
        items: nextCart.items.map((item) => ({
          ...item,
          imageUrl: resolveAssetUrl(item.imageUrl),
        })),
      });
    } catch (value) {
      showToast(value instanceof Error ? value.message : 'Không thể cập nhật giỏ hàng.', 'error');
    } finally {
      setUpdatingItemId(null);
    }
  }

  async function handleRemoveItem(itemId: number) {
    try {
      setUpdatingItemId(itemId);
      const nextCart = await removeCartItem(itemId);
      setCart({
        ...nextCart,
        items: nextCart.items.map((item) => ({
          ...item,
          imageUrl: resolveAssetUrl(item.imageUrl),
        })),
      });
    } catch (value) {
      showToast(value instanceof Error ? value.message : 'Không thể xóa sản phẩm.', 'error');
    } finally {
      setUpdatingItemId(null);
    }
  }

  async function handleCheckout() {
    if (!selectedAddressId) {
      showToast('Vui lòng chọn địa chỉ giao hàng.', 'error');
      return;
    }

    try {
      setCheckingOut(true);
      const result = await checkout({
        addressId: selectedAddressId,
        note: note.trim() || undefined,
        paymentMethod: 'cash',
      });
      setCart((current) => current ? { ...current, items: [], subtotal: 0, totalItemCount: 0 } : current);
      showToast(`Thanh toán thành công. Mã đơn hàng: ${result.orderCode}`);
      navigate('/account/orders');
    } catch (value) {
      showToast(value instanceof Error ? value.message : 'Không thể thanh toán.', 'error');
    } finally {
      setCheckingOut(false);
    }
  }

  return (
    <motion.main
      className={styles.page}
      variants={sectionReveal}
      initial="hidden"
      animate="show"
    >
      <div className={styles.container}>
        <div className={styles.leftColumn}>
          <motion.div className={styles.card} variants={fadeUp}>
            <div className={styles.cardHeader}>
              <svg width="17.5" height="17.5" viewBox="0 0 17.5 17.5" fill="none" aria-hidden="true" role="img">
                <path d="M2.19 1.46h3.5l1.82 9.19a1.75 1.75 0 001.72 1.4h6.35a1.75 1.75 0 001.72-1.4L18.38 5.25H5.25" stroke="#0A0A0A" strokeWidth="1.3" strokeLinecap="round" strokeLinejoin="round" />
                <circle cx="7" cy="15.75" r="1.17" stroke="#0A0A0A" strokeWidth="1.3" />
                <circle cx="15.75" cy="15.75" r="1.17" stroke="#0A0A0A" strokeWidth="1.3" />
              </svg>
              <span className={styles.cardHeaderText}>Giỏ hàng &nbsp;({cart?.totalItemCount ?? 0} items)</span>
            </div>
            <div className={styles.cardContent}>
              {isLoadingCart ? <p>Đang tải giỏ hàng...</p> : null}
              {error ? <p>{error}</p> : null}
              {cart?.items.map((item) => (
                <div key={item.id}>
                  <CartItem
                    item={item}
                    onDecrease={() => handleUpdateQuantity(item.id, item.quantity - 1)}
                    onIncrease={() => handleUpdateQuantity(item.id, item.quantity + 1)}
                    onRemove={() => handleRemoveItem(item.id)}
                    updating={updatingItemId === item.id}
                  />
                  <div className={styles.separator} />
                </div>
              ))}
              {!isLoadingCart && !error && cart && cart.items.length === 0 ? <p>Giỏ hàng đang trống.</p> : null}
            </div>
          </motion.div>

          <motion.div variants={fadeUp}>
            <CustomerNotes value={note} onChange={setNote} />
          </motion.div>
        </div>

        <motion.div className={styles.rightColumn} variants={fadeUp}>
          <CartSummary
            subtotal={cart?.subtotal ?? 0}
            shippingFee={shippingFee}
            totalItemCount={cart?.totalItemCount ?? 0}
            addresses={addresses}
            selectedAddressId={selectedAddressId}
            onSelectAddress={setSelectedAddressId}
            onCheckout={handleCheckout}
            checkingOut={checkingOut}
            disabled={!cart || cart.items.length === 0 || addresses.length === 0}
          />
        </motion.div>
      </div>
    </motion.main>
  );
}
