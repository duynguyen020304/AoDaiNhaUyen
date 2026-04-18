import { useEffect, useState } from 'react';
import type { UserOrder } from '../../types/order';
import { getOrders } from '../../api/user';
import { resolveAssetUrl } from '../../api/client';
import styles from './OrderList.module.css';

export default function OrderList() {
  const [orders, setOrders] = useState<UserOrder[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    getOrders()
      .then(setOrders)
      .catch((value: Error) => setError(value.message))
      .finally(() => setLoading(false));
  }, []);

  function formatDate(iso: string) {
    return new Date(iso).toLocaleDateString('vi-VN');
  }

  function formatPrice(amount: number) {
    return amount.toLocaleString('vi-VN') + ' VND';
  }

  return (
    <div className={styles.container}>
      <h1 className={styles.title}>DON HANG CUA BAN</h1>
      {loading ? <p className={styles.empty}>Đang tải đơn hàng...</p> : null}
      {error ? <p className={styles.empty}>{error}</p> : null}

      {!loading && !error && orders.length === 0 && (
        <p className={styles.empty}>Ban chua co don hang nao.</p>
      )}

      <div className={styles.orderList}>
        {orders.map((order) => (
          <div key={order.id} className={styles.orderCard}>
            <div className={styles.orderHeader}>
              <span className={styles.orderCode}>{order.orderCode}</span>
              <span className={styles.orderDate}>
                {formatDate(order.placedAt)}
              </span>
            </div>

            {order.items.map((item) => (
              <div key={item.id} className={styles.itemRow}>
                <div className={styles.itemImage}>
                  {item.imageUrl ? (
                    <img
                      src={resolveAssetUrl(item.imageUrl) ?? ''}
                      alt={item.productName}
                    />
                  ) : (
                    <div className={styles.imagePlaceholder} />
                  )}
                </div>
                <div className={styles.itemInfo}>
                  <p className={styles.itemName}>
                    Loai ao dai: {item.productName}
                  </p>
                  {item.size && (
                    <p className={styles.itemDetail}>Size: {item.size}</p>
                  )}
                  {item.color && (
                    <p className={styles.itemDetail}>Mau: {item.color}</p>
                  )}
                  <p className={styles.itemDetail}>
                    So luong: {item.quantity}
                  </p>
                </div>
                <div className={styles.itemPrice}>
                  <p>{formatPrice(item.lineTotal)}</p>
                </div>
              </div>
            ))}

            <div className={styles.orderFooter}>
              <span className={styles.status}>
                Tinh trang: {order.paymentStatus ?? order.orderStatus}
              </span>
              <span className={styles.total}>
                Tong: {formatPrice(order.totalAmount)}
              </span>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
