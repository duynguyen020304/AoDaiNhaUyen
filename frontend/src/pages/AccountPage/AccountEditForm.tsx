import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import type { UserProfile, UpdateProfilePayload } from '../../types/user';
import { getUserProfile, updateProfile } from '../../api/user';
import styles from './AccountEditForm.module.css';

export default function AccountEditForm() {
  const navigate = useNavigate();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [form, setForm] = useState<UpdateProfilePayload>({
    fullName: '',
    phone: '',
    dateOfBirth: '',
    gender: '',
    address: '',
  });
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    getUserProfile().then((p) => {
      setProfile(p);
      setForm({
        fullName: p.fullName,
        phone: p.phone ?? '',
        dateOfBirth: p.dateOfBirth ?? '',
        gender: p.gender ?? '',
        address: p.address ?? '',
      });
    });
  }, []);

  function handleChange(field: keyof UpdateProfilePayload, value: string) {
    setForm((prev) => ({ ...prev, [field]: value }));
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSaving(true);
    try {
      await updateProfile(form);
      navigate('/account/profile');
    } finally {
      setSaving(false);
    }
  }

  if (!profile) return null;

  return (
    <div className={styles.container}>
      <h1 className={styles.title}>CAP NHAT TAI KHOAN</h1>

      <form className={styles.form} onSubmit={handleSubmit}>
        <label className={styles.fieldLabel}>
          Ho va ten
          <input
            className={styles.fieldInput}
            type="text"
            value={form.fullName}
            onChange={(e) => handleChange('fullName', e.target.value)}
          />
        </label>

        <label className={styles.fieldLabel}>
          So dien thoai
          <input
            className={styles.fieldInput}
            type="tel"
            value={form.phone}
            onChange={(e) => handleChange('phone', e.target.value)}
          />
        </label>

        <label className={styles.fieldLabel}>
          Dia chi
          <input
            className={styles.fieldInput}
            type="text"
            value={form.address}
            onChange={(e) => handleChange('address', e.target.value)}
          />
        </label>

        <label className={styles.fieldLabel}>
          Ngay sinh
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
            value={profile.email ?? ''}
            readOnly
          />
        </label>

        <button
          type="submit"
          className={styles.submitButton}
          disabled={saving}
        >
          {saving ? 'Dang luu...' : 'LUU THONG TIN'}
        </button>
      </form>
    </div>
  );
}
