import { useEffect, useState } from 'react';
import type { UserProfile } from '../../types/user';
import { getUserProfile } from '../../api/user';
import styles from './AccountInfo.module.css';

interface AccountInfoProps {
  onEdit: () => void;
}

export default function AccountInfo({ onEdit }: AccountInfoProps) {
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    getUserProfile()
      .then(setProfile)
      .catch((value: Error) => setError(value.message))
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <div className={styles.container}>Đang tải thông tin tài khoản...</div>;
  if (error) return <div className={styles.container}>{error}</div>;
  if (!profile) return <div className={styles.container}>Không tìm thấy thông tin tài khoản.</div>;

  return (
    <div className={styles.container}>
      <h1 className={styles.title}>THONG TIN TAI KHOAN</h1>

      <div className={styles.fields}>
        <div className={styles.field}>
          <span className={styles.label}>Ho va ten: </span>
          <span className={styles.value}>{profile.fullName}</span>
        </div>
        <div className={styles.field}>
          <span className={styles.label}>Email: </span>
          <span className={styles.value}>{profile.email ?? 'Chua cap nhat'}</span>
        </div>
        <div className={styles.field}>
          <span className={styles.label}>Dien thoai: </span>
          <span className={styles.value}>{profile.phone ?? 'Chua cap nhat'}</span>
        </div>
        <div className={styles.field}>
          <span className={styles.label}>Ngay sinh: </span>
          <span className={styles.value}>
            {profile.dateOfBirth
              ? new Date(profile.dateOfBirth).toLocaleDateString('vi-VN')
              : 'Chua cap nhat'}
          </span>
        </div>
        <div className={styles.field}>
          <span className={styles.label}>Trang thai: </span>
          <span className={styles.value}>{profile.status ?? 'active'}</span>
        </div>
      </div>

      <button type="button" className={styles.editButton} onClick={onEdit}>
        CAP NHAT THONG TIN TAI KHOAN
      </button>
    </div>
  );
}
