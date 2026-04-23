import { useEffect, useState } from 'react';
import type { UserProfile, UpdateProfilePayload } from '../../types/user';
import { getUserProfile, updateProfile } from '../../api/user';
import styles from './AccountEditForm.module.css';

interface AccountEditFormProps {
  onSaved: () => void;
}

export default function AccountEditForm({ onSaved }: AccountEditFormProps) {
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [form, setForm] = useState<UpdateProfilePayload>({
    fullName: '',
    phone: '',
    dateOfBirth: '',
    gender: '',
  });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    getUserProfile()
      .then((p) => {
        setProfile(p);
        setForm({
          fullName: p.fullName,
          phone: p.phone ?? '',
          dateOfBirth: p.dateOfBirth ?? '',
          gender: p.gender ?? '',
        });
      })
      .catch((value: Error) => setError(value.message));
  }, []);

  function handleChange(field: keyof UpdateProfilePayload, value: string) {
    setForm((prev) => ({ ...prev, [field]: value }));
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSaving(true);
    setError(null);
    try {
      await updateProfile(form);
      onSaved();
    } catch (value) {
      setError(value instanceof Error ? value.message : 'Không thể cập nhật tài khoản.');
    } finally {
      setSaving(false);
    }
  }

  if (!profile && !error) return null;

  return (
    <div className={styles.container}>
      <h1 className={styles.title}>CẬP NHẬT TÀI KHOẢN</h1>
      {error ? <p>{error}</p> : null}

      <form className={styles.form} onSubmit={handleSubmit}>
        <label className={styles.fieldLabel}>
          Họ và tên
          <input
            className={styles.fieldInput}
            type="text"
            value={form.fullName}
            onChange={(e) => handleChange('fullName', e.target.value)}
          />
        </label>

        <label className={styles.fieldLabel}>
          Số điện thoại
          <input
            className={styles.fieldInput}
            type="tel"
            value={form.phone}
            onChange={(e) => handleChange('phone', e.target.value)}
          />
        </label>

        <label className={styles.fieldLabel}>
          Ngày sinh
          <input
            className={styles.fieldInput}
            type="date"
            value={form.dateOfBirth}
            onChange={(e) => handleChange('dateOfBirth', e.target.value)}
          />
        </label>

        <label className={styles.fieldLabel}>
          Email
          <input
            className={styles.fieldInput}
            type="email"
            value={profile?.email ?? ''}
            readOnly
          />
        </label>

        <button
          type="submit"
          className={styles.submitButton}
          disabled={saving}
        >
          {saving ? 'Đang lưu...' : 'LƯU THÔNG TIN'}
        </button>
      </form>
    </div>
  );
}
