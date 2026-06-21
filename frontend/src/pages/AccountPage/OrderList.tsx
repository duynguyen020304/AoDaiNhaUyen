import { useState } from 'react';
import { useOrdersQuery } from '../../hooks/user/useUserQueries';
import { useCancelOrderMutation } from '../../hooks/user/useUserMutations';
import { resolveAssetUrl } from '../../api/client';
import { useToast } from '../../components/Toast/useToast';
import type { UserOrder } from '../../types/order';
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
  const ordersQuery = useOrdersQuery();
  const cancelOrderMutation = useCancelOrderMutation();
  const orders = ordersQuery.data ?? [];
  const loading = ordersQuery.isPending;
  const error = ordersQuery.error instanceof Error ? ordersQuery.error.message : null;
  const [cancellingId, setCancellingId] = useState<string | null>(null);
  const [detailOrder, setDetailOrder] = useState<UserOrder | null>(null);

  function formatDate(iso: string) {
    return new Date(iso).toLocaleDateString('vi-VN');
  }

  function formatPrice(amount: number) {
    return amount.toLocaleString('vi-VN') + ' VND';
  }

  function statusLabel(status: string | null) {
    if (!status) return 'Chưa cập nhật';
    return STATUS_LABELS[status] ?? status;
  }

  async function handleCancel(orderId: string) {
    if (!confirm('Bạn có chắc muốn hủy đơn hàng này?')) return;
    try {
      setCancellingId(orderId);
      await cancelOrderMutation.mutateAsync(orderId);
      setDetailOrder((current) => current?.id === orderId ? { ...current, orderStatus: 'cancelled' } : current);
      showToast('Hủy đơn hàng thành công.');
    } catch (value) {
      showToast(value instanceof Error ? value.message : 'Không thể hủy đơn hàng.', 'error');
    } finally {
      setCancellingId(null);
    }
  }

  const canCancel = (status: string) => status === 'pending' || status === 'confirmed';

  function formatFullAddress(order: UserOrder) {
    return [order.addressLine, order.ward, order.district, order.province]
      .filter(Boolean)
      .join(', ');
  }

  if (detailOrder) {
    const activeIndex = STATUS_STEPS.indexOf(detailOrder.orderStatus);

    return (
      <div className={styles.container}>
        <button type="button" className={styles.backLink} onClick={() => setDetailOrder(null)}>
          ← Quay lại danh sách
        </button>

        <div className={styles.detailSheet}>
          <header className={styles.detailHero}>
            <div className={styles.detailHeroLeft}>
              <p className={styles.detailEyebrow}>Chi tiết đơn hàng</p>
              <h1>{detailOrder.orderCode}</h1>
              <p className={styles.detailDesc}>
                Theo dõi tiến trình, thông tin giao hàng và toàn bộ sản phẩm trong đơn hàng.
              </p>
              {canCancel(detailOrder.orderStatus) ? (
                <button
                  type="button"
                  className={styles.cancelBtn}
                  onClick={() => void handleCancel(detailOrder.id)}
                  disabled={cancellingId === detailOrder.id}
                >
                  {cancellingId === detailOrder.id ? 'Đang hủy...' : 'Hủy đơn'}
                </button>
              ) : null}
            </div>
            <div className={styles.detailSummary}>
              <span className={styles.detailSummaryBadge}>{statusLabel(detailOrder.orderStatus)}</span>
              <strong>{formatPrice(detailOrder.totalAmount)}</strong>
              <small>Ngày đặt: {formatDate(detailOrder.placedAt)}</small>
            </div>
          </header>

          {activeIndex >= 0 && (
            <div className={styles.detailTimeline}>
              {STATUS_STEPS.map((step, index) => {
                const isActive = index <= activeIndex;
                const isCurrent = index === activeIndex;
                return (
                  <div
                    key={step}
                    className={`${styles.detailStep} ${isActive ? styles.detailStepActive : ''} ${isCurrent ? styles.detailStepCurrent : ''}`}
                  >
                    <span className={styles.detailStepDot} />
                    <span>{STATUS_LABELS[step]}</span>
                  </div>
                );
              })}
            </div>
          )}

          <div className={styles.detailContent}>
            <section className={styles.detailPanel}>
              <header className={styles.detailPanelHeader}>
                <div>
                  <p className={styles.detailPanelEyebrow}>Đơn hàng</p>
                  <h2>Sản phẩm</h2>
                </div>
              </header>

              <div className={styles.detailItemList}>
                {detailOrder.items.map((item) => (
                  <article key={item.id} className={styles.detailItemCard}>
                    <div className={styles.detailItemImage}>
                      {item.imageUrl ? (
                        <img src={resolveAssetUrl(item.imageUrl) ?? ''} alt={item.productName} />
                      ) : (
                        <span className={styles.detailImagePlaceholder} />
                      )}
                    </div>
                    <div className={styles.detailItemInfo}>
                      <h3>{item.productName}</h3>
                      <div className={styles.detailItemMeta}>
                        <span>Số lượng: {item.quantity}</span>
                        {item.size ? <span>Size: {item.size}</span> : null}
                        {item.color ? <span>Màu: {item.color}</span> : null}
                      </div>
                    </div>
                    <strong className={styles.detailItemPrice}>{formatPrice(item.lineTotal)}</strong>
                  </article>
                ))}
              </div>
            </section>

            <aside className={styles.detailSideColumn}>
              <section className={styles.detailPanel}>
                <header className={styles.detailPanelHeader}>
                  <div>
                    <p className={styles.detailPanelEyebrow}>Giao hàng</p>
                    <h2>Người nhận</h2>
                  </div>
                </header>

                <dl className={styles.detailInfoList}>
                  <div className={styles.detailInfoRow}>
                    <dt>Họ tên</dt>
                    <dd>{detailOrder.recipientName}</dd>
                  </div>
                  <div className={styles.detailInfoRow}>
                    <dt>Số điện thoại</dt>
                    <dd>{detailOrder.recipientPhone}</dd>
                  </div>
                  <div className={styles.detailInfoRow}>
                    <dt>Địa chỉ</dt>
                    <dd>{formatFullAddress(detailOrder)}</dd>
                  </div>
                  {detailOrder.note ? (
                    <div className={styles.detailInfoRow}>
                      <dt>Ghi chú</dt>
                      <dd>{detailOrder.note}</dd>
                    </div>
                  ) : null}
                </dl>
              </section>

              <section className={styles.detailPanel}>
                <header className={styles.detailPanelHeader}>
                  <div>
                    <p className={styles.detailPanelEyebrow}>Tài chính</p>
                    <h2>Thanh toán</h2>
                  </div>
                </header>

                <dl className={styles.detailPriceList}>
                  <div className={styles.detailPriceRow}>
                    <dt>Tạm tính</dt>
                    <dd>{formatPrice(detailOrder.subtotal)}</dd>
                  </div>
                  <div className={styles.detailPriceRow}>
                    <dt>Phí vận chuyển</dt>
                    <dd>{formatPrice(detailOrder.shippingFee)}</dd>
                  </div>
                  <div className={styles.detailPriceRow}>
                    <dt>Giảm giá</dt>
                    <dd>-{formatPrice(detailOrder.discountAmount)}</dd>
                  </div>
                  <div className={`${styles.detailPriceRow} ${styles.detailPriceTotal}`}>
                    <dt>Tổng cộng</dt>
                    <dd>{formatPrice(detailOrder.totalAmount)}</dd>
                  </div>
                </dl>

                <p className={styles.detailPaymentStatus}>{statusLabel(detailOrder.paymentStatus)}</p>
              </section>
            </aside>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <h1 className={styles.title}>Đơn hàng của bạn</h1>
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
                  <p className={styles.itemName}>{item.productName}</p>
                  <div className={styles.itemMeta}>
                    {item.size && <span>Size: {item.size}</span>}
                    {item.color && <span>Màu: {item.color}</span>}
                    <span className={styles.itemQty}>SL: {item.quantity}</span>
                  </div>
                </div>
                <div className={styles.itemPrice}>
                  <p>{formatPrice(item.lineTotal)}</p>
                </div>
              </div>
            ))}

            <div className={styles.orderFooter}>
              <div className={styles.footerActions}>
                <button
                  className={styles.detailBtn}
                  type="button"
                  onClick={() => setDetailOrder(order)}
                >
                  Xem chi tiết
                </button>
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
