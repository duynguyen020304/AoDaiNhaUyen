import { Link, useParams } from 'react-router-dom';
import { resolveAssetUrl } from '../../api/client';
import { useOrdersQuery } from '../../hooks/user/useUserQueries';
import styles from './OrderDetailPage.module.css';

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

function formatDate(iso: string | null) {
  if (!iso) return 'Chưa cập nhật';
  return new Date(iso).toLocaleDateString('vi-VN');
}

function formatPrice(amount: number) {
  return amount.toLocaleString('vi-VN') + ' VND';
}

function statusLabel(status: string | null) {
  if (!status) return 'Chưa cập nhật';
  return STATUS_LABELS[status] ?? status;
}

export default function OrderDetailPage() {
  const { orderId } = useParams();
  const ordersQuery = useOrdersQuery();
  const orders = ordersQuery.data ?? [];
  const order = orders.find((item) => item.id === orderId);
  const activeIndex = order ? STATUS_STEPS.indexOf(order.orderStatus) : -1;
  const address = order
    ? [order.addressLine, order.ward, order.district, order.province].filter(Boolean).join(', ')
    : '';

  if (ordersQuery.isPending) {
    return (
      <main className={styles.page}>
        <section className={styles.shell}>
          <div className={styles.skeleton}>
            <span />
            <span />
            <span />
            <span />
          </div>
        </section>
      </main>
    );
  }

  if (ordersQuery.error || !order) {
    return (
      <main className={styles.page}>
        <section className={styles.emptyCard}>
          <p className={styles.eyebrow}>Không tìm thấy</p>
          <h1>Không tìm thấy đơn hàng</h1>
          <p>Đơn hàng không tồn tại hoặc bạn không có quyền xem thông tin này.</p>
          <Link className={styles.primaryLink} to="/account/orders">
            Quay lại danh sách
          </Link>
        </section>
      </main>
    );
  }

  return (
    <main className={styles.page}>
      <section className={styles.shell}>
        <Link className={styles.backLink} to="/account/orders">
          ← Quay lại đơn hàng
        </Link>

        <div className={styles.heroGrid}>
          <div className={styles.heroCard}>
            <p className={styles.eyebrow}>Chi tiết đơn hàng</p>
            <h1>{order.orderCode}</h1>
            <p className={styles.heroDesc}>Theo dõi tiến trình, thông tin giao hàng và toàn bộ sản phẩm trong đơn hàng.</p>
          </div>
          <div className={styles.summaryCard}>
            <span className={styles.summaryBadge}>{statusLabel(order.orderStatus)}</span>
            <strong>{formatPrice(order.totalAmount)}</strong>
            <small>Ngày đặt: {formatDate(order.placedAt)}</small>
          </div>
        </div>

        {activeIndex >= 0 && (
          <div className={styles.timelineCard}>
            {STATUS_STEPS.map((step, index) => {
              const isActive = index <= activeIndex;
              const isCurrent = index === activeIndex;
              return (
                <div
                  key={step}
                  className={`${styles.step} ${isActive ? styles.stepActive : ''} ${isCurrent ? styles.stepCurrent : ''}`}
                >
                  <span className={styles.stepDot} />
                  <span>{STATUS_LABELS[step]}</span>
                </div>
              );
            })}
          </div>
        )}

        <div className={styles.contentGrid}>
          <section className={styles.card}>
            <header className={styles.cardHeader}>
              <div>
                <p className={styles.cardEyebrow}>Đơn hàng</p>
                <h2>Sản phẩm</h2>
              </div>
            </header>

            <div className={styles.itemList}>
              {order.items.map((item) => (
                <article key={item.id} className={styles.itemCard}>
                  <div className={styles.itemImage}>
                    {item.imageUrl ? (
                      <img src={resolveAssetUrl(item.imageUrl) ?? ''} alt={item.productName} />
                    ) : (
                      <span className={styles.imagePlaceholder} />
                    )}
                  </div>
                  <div className={styles.itemInfo}>
                    <h3>{item.productName}</h3>
                    <div className={styles.itemMeta}>
                      <span>Số lượng: {item.quantity}</span>
                      {item.size ? <span>Size: {item.size}</span> : null}
                      {item.color ? <span>Màu: {item.color}</span> : null}
                    </div>
                  </div>
                  <strong className={styles.itemPrice}>{formatPrice(item.lineTotal)}</strong>
                </article>
              ))}
            </div>
          </section>

          <aside className={styles.sideColumn}>
            <section className={styles.card}>
              <header className={styles.cardHeader}>
                <div>
                  <p className={styles.cardEyebrow}>Giao hàng</p>
                  <h2>Người nhận</h2>
                </div>
              </header>

              <dl className={styles.infoList}>
                <div className={styles.infoRow}>
                  <dt>Họ tên</dt>
                  <dd>{order.recipientName}</dd>
                </div>
                <div className={styles.infoRow}>
                  <dt>Số điện thoại</dt>
                  <dd>{order.recipientPhone}</dd>
                </div>
                <div className={styles.infoRow}>
                  <dt>Địa chỉ</dt>
                  <dd>{address}</dd>
                </div>
                {order.note ? (
                  <div className={styles.infoRow}>
                    <dt>Ghi chú</dt>
                    <dd>{order.note}</dd>
                  </div>
                ) : null}
              </dl>
            </section>

            <section className={styles.card}>
              <header className={styles.cardHeader}>
                <div>
                  <p className={styles.cardEyebrow}>Tài chính</p>
                  <h2>Thanh toán</h2>
                </div>
              </header>

              <dl className={styles.priceList}>
                <div className={styles.priceRow}>
                  <dt>Tạm tính</dt>
                  <dd>{formatPrice(order.subtotal)}</dd>
                </div>
                <div className={styles.priceRow}>
                  <dt>Phí vận chuyển</dt>
                  <dd>{formatPrice(order.shippingFee)}</dd>
                </div>
                <div className={styles.priceRow}>
                  <dt>Giảm giá</dt>
                  <dd>-{formatPrice(order.discountAmount)}</dd>
                </div>
                <div className={`${styles.priceRow} ${styles.priceTotal}`}>
                  <dt>Tổng cộng</dt>
                  <dd>{formatPrice(order.totalAmount)}</dd>
                </div>
              </dl>

              <p className={styles.paymentStatus}>{statusLabel(order.paymentStatus)}</p>
            </section>
          </aside>
        </div>
      </section>
    </main>
  );
}
