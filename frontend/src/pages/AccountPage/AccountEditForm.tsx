import { useState } from 'react';
import type { UpdateProfilePayload } from '../../types/user';
import { useUserProfileQuery } from '../../hooks/user/useUserQueries';
import { useUpdateProfileMutation } from '../../hooks/user/useUserMutations';
import styles from './AccountEditForm.module.css';

interface AccountEditFormProps {
  onSaved: () => void;
}

export default function AccountEditForm({ onSaved }: AccountEditFormProps) {
  const profileQuery = useUserProfileQuery();
  const profile = profileQuery.data ?? null;
  const loadError = profileQuery.error instanceof Error ? profileQuery.error.message : null;

  if (profileQuery.isPending) return null;
  if (loadError) return <div className={styles.container}>{loadError}</div>;
  if (!profile) return null;

  return <AccountEditFields key={profile.id} initialForm={{
    fullName: profile.fullName,
    phone: profile.phone ?? '',
    dateOfBirth: profile.dateOfBirth ?? '',
    gender: profile.gender ?? '',
  }} email={profile.email ?? ''} onSaved={onSaved} />;
}

function AccountEditFields({
  initialForm,
  email,
  onSaved,
}: {
  initialForm: UpdateProfilePayload;
  email: string;
  onSaved: () => void;
}) {
  const updateProfileMutation = useUpdateProfileMutation();
  const [form, setForm] = useState<UpdateProfilePayload>(initialForm);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function handleChange(field: keyof UpdateProfilePayload, value: string) {
    setForm((prev) => ({ ...prev, [field]: value }));
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSaving(true);
    setError(null);
    try {
      await updateProfileMutation.mutateAsync(form);
      onSaved();
    } catch (value) {
      setError(value instanceof Error ? value.message : 'Không thể cập nhật tài khoản.');
    } finally {
      setSaving(false);
    }
  }

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
            value={email}
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
