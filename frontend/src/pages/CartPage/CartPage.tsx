import { useMemo, useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { motion } from 'framer-motion';
import { useNavigate } from 'react-router-dom';
import styles from './CartPage.module.css';
import CartItem from './CartItem';
import CustomerNotes from './CustomerNotes';
import CartSummary from './CartSummary';
import { fadeUp, sectionReveal } from '../../utils/motion';
import { checkout } from '../../api/checkout';
import type { Cart } from '../../types/cart';
import { useCartQuery } from '../../hooks/cart/useCartQueries';
import { useRemoveCartItemMutation, useUpdateCartItemMutation } from '../../hooks/cart/useCartMutations';
import { useAddressesQuery } from '../../hooks/user/useUserQueries';
import { queryKeys } from '../../lib/queryKeys';
import { useToast } from '../../components/Toast/useToast';
import { trackEvent } from '../../api/events';
import { useAuth } from '../../auth/useAuth';

export default function CartPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { status } = useAuth();
  const { showToast } = useToast();
  const enabled = status === 'authenticated';
  const cartQuery = useCartQuery(enabled);
  const addressesQuery = useAddressesQuery(enabled);
  const updateCartItemMutation = useUpdateCartItemMutation();
  const removeCartItemMutation = useRemoveCartItemMutation();
  const [selectedAddressId, setSelectedAddressId] = useState<string | null>(null);
  const [note, setNote] = useState('');
  const [checkingOut, setCheckingOut] = useState(false);
  const [updatingItemId, setUpdatingItemId] = useState<string | null>(null);
  const [appliedPromoCode, setAppliedPromoCode] = useState<string | null>(null);
  const [discountAmount, setDiscountAmount] = useState(0);
  const [discountLabel, setDiscountLabel] = useState<string | null>(null);
  const [promoFreeShipping, setPromoFreeShipping] = useState(false);

  const cart = cartQuery.data ?? null;
  const addresses = addressesQuery.data ?? [];
  const defaultAddress = addresses.find((value) => value.isDefault) ?? addresses[0];
  const effectiveSelectedAddressId = selectedAddressId ?? defaultAddress?.id ?? null;
  const firstError = cartQuery.error ?? addressesQuery.error;
  const error = firstError instanceof Error ? firstError.message : null;
  const isLoadingCart = status === 'loading' || (status === 'authenticated' && (cartQuery.isPending || addressesQuery.isPending));
  const shippingFee = useMemo(() => (cart && cart.items.length > 0 ? 25000 : 0), [cart]);

  function handlePromoApplied(code: string, amount: number, label: string, freeShipping: boolean) {
    setAppliedPromoCode(code);
    setDiscountAmount(amount);
    setDiscountLabel(label);
    setPromoFreeShipping(freeShipping);
  }

  function handlePromoCleared() {
    setAppliedPromoCode(null);
    setDiscountAmount(0);
    setDiscountLabel(null);
    setPromoFreeShipping(false);
  }

  async function handleUpdateQuantity(itemId: string, quantity: number) {
    try {
      setUpdatingItemId(itemId);
      await updateCartItemMutation.mutateAsync({ itemId, payload: { quantity } });
    } catch (value) {
      showToast(value instanceof Error ? value.message : 'Không thể cập nhật giỏ hàng.', 'error');
    } finally {
      setUpdatingItemId(null);
    }
  }

  async function handleRemoveItem(itemId: string) {
    try {
      setUpdatingItemId(itemId);
      await removeCartItemMutation.mutateAsync(itemId);
    } catch (value) {
      showToast(value instanceof Error ? value.message : 'Không thể xóa sản phẩm.', 'error');
    } finally {
      setUpdatingItemId(null);
    }
  }

  async function handleCheckout() {
    if (!effectiveSelectedAddressId) {
      showToast('Vui lòng chọn địa chỉ giao hàng.', 'error');
      return;
    }

    void trackEvent({ eventType: 'checkout_started', metadata: { promoCode: appliedPromoCode, itemCount: cart?.totalItemCount ?? 0 } });
    try {
      setCheckingOut(true);
      const result = await checkout({
        addressId: effectiveSelectedAddressId,
        note: note.trim() || undefined,
        paymentMethod: 'cash',
        promoCode: appliedPromoCode ?? undefined,
      });
      void trackEvent({ eventType: 'checkout_completed', orderId: result.orderId, metadata: { orderCode: result.orderCode, totalAmount: result.totalAmount, promoCode: appliedPromoCode } });
      queryClient.setQueryData<Cart | null>(queryKeys.cart.current, (current) => current ? { ...current, items: [], subtotal: 0, totalItemCount: 0 } : current);
      void queryClient.invalidateQueries({ queryKey: queryKeys.orders.list });
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
            selectedAddressId={effectiveSelectedAddressId}
            onSelectAddress={setSelectedAddressId}
            onCheckout={handleCheckout}
            checkingOut={checkingOut}
            disabled={!cart || cart.items.length === 0 || addresses.length === 0}
            appliedPromoCode={appliedPromoCode}
            discountAmount={discountAmount}
            discountLabel={discountLabel}
            promoFreeShipping={promoFreeShipping}
            onPromoApplied={handlePromoApplied}
            onPromoCleared={handlePromoCleared}
          />
        </motion.div>
      </div>
    </motion.main>
  );
}
