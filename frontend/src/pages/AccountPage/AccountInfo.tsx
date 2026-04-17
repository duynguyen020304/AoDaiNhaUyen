import { useEffect, useState } from 'react';
import type { UserProfile } from '../../types/user';
import { getUserProfile } from '../../api/user';
import styles from './AccountInfo.module.css';

interface AccountInfoProps {
  onEdit: () => void;
}

export default function AccountInfo({ onEdit }: AccountInfoProps) {
  const [profile, setProfile] = useState<UserProfile | null>(null);

  useEffect(() => {
    getUserProfile().then(setProfile);
  }, []);

  if (!profile) return null;

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
          <span className={styles.label}>Dia chi: </span>
          <span className={styles.value}>{profile.address ?? 'Chua cap nhat'}</span>
        </div>
        <div className={styles.field}>
          <span className={styles.label}>Ngay sinh: </span>
          <span className={styles.value}>
            {profile.dateOfBirth
              ? new Date(profile.dateOfBirth).toLocaleDateString('vi-VN')
              : 'Chua cap nhat'}
          </span>
        </div>
      </div>

      <button type="button" className={styles.editButton} onClick={onEdit}>
        CAP NHAT THONG TIN TAI KHOAN
      </button>
    </div>
  );
}
