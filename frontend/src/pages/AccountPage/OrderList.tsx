import { useEffect, useState } from 'react';
import type { UserOrder } from '../../types/order';
import { getOrders, cancelOrder } from '../../api/user';
import { resolveAssetUrl } from '../../api/client';
import { useToast } from '../../components/Toast/useToast';
import styles from './OrderList.module.css';

const STATUS_LABELS: Record<string, string> = {
  pending: 'Chờ xác nhận',
  confirmed: 'Đã xác nhận',
  processing: 'Đang chuẩn bị',
  shipping: 'Đang giao hàng',
  completed: 'Hoàn thành',
  cancelled: 'Đã hủy',
  returned: 'Đã trả hàng',
};

const STATUS_STEPS = ['pending', 'confirmed', 'processing', 'shipping', 'completed'];

function StatusBadge({ status }: { status: string }) {
  const colorMap: Record<string, { bg: string; text: string }> = {
    pending: { bg: '#f3f3f5', text: '#575767' },
    confirmed: { bg: '#eff6ff', text: '#1d4ed8' },
    processing: { bg: '#eff6ff', text: '#1d4ed8' },
    shipping: { bg: '#fff7ed', text: '#c2410c' },
    completed: { bg: '#ecfdf5', text: '#047857' },
    cancelled: { bg: '#fef2f2', text: '#b91c1c' },
    returned: { bg: '#fef2f2', text: '#b91c1c' },
  };
  const colors = colorMap[status] ?? colorMap.pending;
  return (
    <span
      className={styles.statusBadge}
      style={{ background: colors.bg, color: colors.text }}
    >
      {STATUS_LABELS[status] ?? status}
    </span>
  );
}

function StatusStepper({ status }: { status: string }) {
  const activeIndex = STATUS_STEPS.indexOf(status);
  if (activeIndex < 0) return null;

  return (
    <div className={styles.stepper}>
      {STATUS_STEPS.map((step, i) => {
        const isActive = i <= activeIndex;
        const isCurrent = i === activeIndex;
        return (
          <div key={step} className={styles.stepWrapper}>
            {i > 0 && (
              <div className={`${styles.stepLine} ${isActive ? styles.stepLineActive : ''}`} />
            )}
            <div className={`${styles.stepDot} ${isActive ? styles.stepDotActive : ''} ${isCurrent ? styles.stepDotCurrent : ''}`} />
            <span className={`${styles.stepLabel} ${isActive ? styles.stepLabelActive : ''}`}>
              {STATUS_LABELS[step]}
            </span>
          </div>
        );
      })}
    </div>
  );
}

export default function OrderList() {
  const { showToast } = useToast();
  const [orders, setOrders] = useState<UserOrder[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [cancellingId, setCancellingId] = useState<string | null>(null);
  const [expandedId, setExpandedId] = useState<string | null>(null);

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

  async function handleCancel(orderId: string) {
    if (!confirm('Bạn có chắc muốn hủy đơn hàng này?')) return;
    try {
      setCancellingId(orderId);
      await cancelOrder(orderId);
      setOrders((prev) =>
        prev.map((o) => o.id === orderId ? { ...o, orderStatus: 'cancelled' } : o)
      );
      showToast('Hủy đơn hàng thành công.');
    } catch (value) {
      showToast(value instanceof Error ? value.message : 'Không thể hủy đơn hàng.', 'error');
    } finally {
      setCancellingId(null);
    }
  }

  const canCancel = (status: string) => status === 'pending' || status === 'confirmed';

  return (
    <div className={styles.container}>
      <h1 className={styles.title}>ĐƠN HÀNG CỦA BẠN</h1>
      {loading ? <p className={styles.empty}>Đang tải đơn hàng...</p> : null}
      {error ? <p className={styles.empty}>{error}</p> : null}

      {!loading && !error && orders.length === 0 && (
        <p className={styles.empty}>Bạn chưa có đơn hàng nào.</p>
      )}

      <div className={styles.orderList}>
        {orders.map((order) => (
          <div key={order.id} className={styles.orderCard}>
            <div className={styles.orderHeader}>
              <span className={styles.orderCode}>{order.orderCode}</span>
              <StatusBadge status={order.orderStatus} />
              <span className={styles.orderDate}>
                {formatDate(order.placedAt)}
              </span>
            </div>

            {/* Status stepper — visible for non-cancelled orders */}
            {order.orderStatus !== 'cancelled' && order.orderStatus !== 'returned' && (
              <StatusStepper status={order.orderStatus} />
            )}

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
                    Loại áo dài: {item.productName}
                  </p>
                  {item.size && (
                    <p className={styles.itemDetail}>Size: {item.size}</p>
                  )}
                  {item.color && (
                    <p className={styles.itemDetail}>Màu: {item.color}</p>
                  )}
                  <p className={styles.itemDetail}>
                    Số lượng: {item.quantity}
                  </p>
                </div>
                <div className={styles.itemPrice}>
                  <p>{formatPrice(item.lineTotal)}</p>
                </div>
              </div>
            ))}

            <div className={styles.orderFooter}>
              <div className={styles.footerActions}>
                {canCancel(order.orderStatus) && (
                  <button
                    className={styles.cancelBtn}
                    type="button"
                    onClick={() => handleCancel(order.id)}
                    disabled={cancellingId === order.id}
                  >
                    {cancellingId === order.id ? 'Đang hủy...' : 'Hủy đơn'}
                  </button>
                )}
              </div>
              <span className={styles.total}>
                Tổng: {formatPrice(order.totalAmount)}
              </span>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
