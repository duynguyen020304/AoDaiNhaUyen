import { useUserProfileQuery } from '../../hooks/user/useUserQueries';
import styles from './AccountInfo.module.css';

interface AccountInfoProps {
  onEdit: () => void;
}

export default function AccountInfo({ onEdit }: AccountInfoProps) {
  const profileQuery = useUserProfileQuery();
  const profile = profileQuery.data ?? null;
  const error = profileQuery.error instanceof Error ? profileQuery.error.message : null;

  if (profileQuery.isPending) return <div className={styles.container}>Đang tải thông tin tài khoản...</div>;
  if (error) return <div className={styles.container}>{error}</div>;
  if (!profile) return <div className={styles.container}>Không tìm thấy thông tin tài khoản.</div>;

  return (
    <div className={styles.container}>
      <h1 className={styles.title}>THÔNG TIN TÀI KHOẢN</h1>

      <div className={styles.fields}>
        <div className={styles.field}>
          <span className={styles.label}>Họ và tên: </span>
          <span className={styles.value}>{profile.fullName}</span>
        </div>
        <div className={styles.field}>
          <span className={styles.label}>Email: </span>
          <span className={styles.value}>{profile.email ?? 'Chưa cập nhật'}</span>
        </div>
        <div className={styles.field}>
          <span className={styles.label}>Điện thoại: </span>
          <span className={styles.value}>{profile.phone ?? 'Chưa cập nhật'}</span>
        </div>
        <div className={styles.field}>
          <span className={styles.label}>Ngày sinh: </span>
          <span className={styles.value}>
            {profile.dateOfBirth
              ? new Date(profile.dateOfBirth).toLocaleDateString('vi-VN')
              : 'Chưa cập nhật'}
          </span>
        </div>
        <div className={styles.field}>
          <span className={styles.label}>Trạng thái: </span>
          <span className={styles.value}>{profile.status ?? 'active'}</span>
        </div>
      </div>

      <button type="button" className={styles.editButton} onClick={onEdit}>
        CẬP NHẬT THÔNG TIN TÀI KHOẢN
      </button>
    </div>
  );
}
