import { resolveAssetUrl } from '../../api/client';
import { useUserProfileQuery, useOrdersQuery } from '../../hooks/user/useUserQueries';
import type { AccountView } from './AccountPage';
import { formatAccountDisplayName, getAccountInitial } from '../../utils/accountDisplay';
import styles from './AccountInfo.module.css';

interface AccountInfoProps {
  onEdit: () => void;
  onNavigate: (view: AccountView) => void;
}

const STATUS_LABELS: Record<string, string> = {
  pending: 'Chờ xác nhận',
  confirmed: 'Đã xác nhận',
  processing: 'Đang chuẩn bị',
  shipping: 'Đang giao hàng',
  completed: 'Hoàn thành',
  cancelled: 'Đã hủy',
  returned: 'Đã trả hàng',
};

const GENDER_LABELS: Record<string, string> = {
  male: 'Nam',
  female: 'Nữ',
  other: 'Khác',
  nam: 'Nam',
  nu: 'Nữ',
  nữ: 'Nữ',
  khac: 'Khác',
  khác: 'Khác',
};

function formatGender(gender: string | null) {
  if (!gender) return 'Chưa cập nhật';
  return GENDER_LABELS[gender.trim().toLowerCase()] ?? gender;
}

function formatDate(iso: string | null) {
  if (!iso) return 'Chưa cập nhật';
  return new Date(iso).toLocaleDateString('vi-VN');
}

function statusLabel(status: string) {
  return STATUS_LABELS[status] ?? status;
}

function splitName(fullName: string) {
  const parts = fullName.trim().split(/\s+/);
  if (parts.length <= 1) {
    return { firstName: fullName, lastName: 'Chưa cập nhật' };
  }

  return {
    firstName: parts.slice(0, -1).join(' '),
    lastName: parts.at(-1) ?? '',
  };
}

export default function AccountInfo({ onEdit, onNavigate }: AccountInfoProps) {
  const profileQuery = useUserProfileQuery();
  const ordersQuery = useOrdersQuery();
  const profile = profileQuery.data ?? null;
  const orders = ordersQuery.data ?? [];
  const recentOrders = orders.slice(0, 2);
  const error = profileQuery.error instanceof Error ? profileQuery.error.message : null;

  if (profileQuery.isPending) return <div className={styles.stateCard}>Đang tải thông tin tài khoản...</div>;
  if (error) return <div className={styles.stateCard}>{error}</div>;
  if (!profile) return <div className={styles.stateCard}>Không tìm thấy thông tin tài khoản.</div>;

  const avatarSrc = resolveAssetUrl(profile.avatarUrl);
  const displayName = formatAccountDisplayName(profile);
  const initial = getAccountInitial(profile);
  const { firstName, lastName } = splitName(profile.fullName);

  return (
    <div className={styles.container}>
      <section className={styles.grid}>
        <div className={styles.mainColumn}>
          <article className={styles.card}>
            <header className={styles.cardHeader}>
              <div>
                <p className={styles.eyebrow}>Hồ sơ</p>
                <h2>Chi tiết cá nhân</h2>
              </div>
              <button type="button" className={styles.secondaryButton} onClick={onEdit}>
                <span aria-hidden="true">✎</span>
                Chỉnh sửa
              </button>
            </header>

            <div className={styles.formGrid}>
              <div className={styles.fieldBlock}>
                <span className={styles.label}>Tên</span>
                <span className={styles.value}>{lastName}</span>
              </div>
              <div className={styles.fieldBlock}>
                <span className={styles.label}>Họ và tên đệm</span>
                <span className={styles.value}>{firstName}</span>
              </div>
              <div className={`${styles.fieldBlock} ${styles.fullWidth}`}>
                <span className={styles.label}>Địa chỉ email</span>
                <span className={styles.valueRow}>
                  <span>{profile.email ?? 'Chưa cập nhật'}</span>
                  {profile.email ? <span className={styles.verified}>Đã có</span> : null}
                </span>
              </div>
              <div className={`${styles.fieldBlock} ${styles.fullWidth}`}>
                <span className={styles.label}>Số điện thoại</span>
                <span className={styles.value}>{profile.phone ?? 'Chưa cập nhật'}</span>
              </div>
              <div className={styles.fieldBlock}>
                <span className={styles.label}>Ngày sinh</span>
                <span className={styles.value}>{formatDate(profile.dateOfBirth)}</span>
              </div>
              <div className={styles.fieldBlock}>
                <span className={styles.label}>Giới tính</span>
                <span className={styles.value}>{formatGender(profile.gender)}</span>
              </div>
              <div className={`${styles.fieldBlock} ${styles.fullWidth}`}>
                <span className={styles.label}>Trạng thái</span>
                <span className={styles.value}>{profile.status ?? 'active'}</span>
              </div>
            </div>
          </article>
        </div>

        <aside className={styles.sideColumn}>
          <article className={styles.card}>
            <div className={styles.profileMini}>
              <div className={styles.avatarSmall}>
                {avatarSrc ? (
                  <img src={avatarSrc} alt={displayName} />
                ) : (
                  <span>{initial}</span>
                )}
              </div>
              <div>
                <h3>{displayName}</h3>
                <p>{profile.email ?? 'Chưa cập nhật email'}</p>
              </div>
            </div>
          </article>

          <article className={styles.card}>
            <header className={styles.widgetHeaderCompact}>
              <span className={styles.iconBadge} aria-hidden="true">◆</span>
              <h3>Bảo mật tài khoản</h3>
            </header>
            <p className={styles.helperText}>Giữ thông tin liên hệ đúng để đặt hàng và khôi phục tài khoản dễ hơn.</p>
            <button type="button" className={styles.ghostButton} onClick={onEdit}>
              Kiểm tra thông tin
            </button>
          </article>

          <article className={styles.card}>
            <header className={styles.widgetHeader}>
              <h3>Đơn hàng hiện tại</h3>
              <button type="button" onClick={() => onNavigate('orders')}>Xem tất cả</button>
            </header>

            <div className={styles.orderStack}>
              {ordersQuery.isPending ? <p className={styles.helperText}>Đang tải đơn hàng...</p> : null}
              {!ordersQuery.isPending && recentOrders.length === 0 ? (
                <p className={styles.helperText}>Bạn chưa có đơn hàng nào.</p>
              ) : null}
              {recentOrders.map((order) => (
                <button key={order.id} type="button" className={styles.orderItem} onClick={() => onNavigate('orders')}>
                  <span className={styles.orderIcon} aria-hidden="true">▣</span>
                  <span>
                    <strong>{order.orderCode}</strong>
                    <small>{statusLabel(order.orderStatus)} • {formatDate(order.placedAt)}</small>
                  </span>
                  <span className={styles.chevron} aria-hidden="true">›</span>
                </button>
              ))}
            </div>
          </article>
        </aside>
      </section>
    </div>
  );
}
