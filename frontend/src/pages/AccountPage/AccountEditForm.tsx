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

  if (profileQuery.isPending) return <div className={styles.stateCard}>Đang tải thông tin...</div>;
  if (loadError) return <div className={styles.stateCard}>{loadError}</div>;
  if (!profile) return <div className={styles.stateCard}>Không tìm thấy thông tin tài khoản.</div>;

  return <AccountEditFields key={profile.id} initialForm={{
    fullName: profile.fullName,
    phone: profile.phone ?? '',
    dateOfBirth: profile.dateOfBirth ?? '',
    gender: profile.gender ?? '',
  }} email={profile.email ?? ''} onSaved={onSaved} />;
}

function splitName(fullName: string) {
  const parts = fullName.trim().split(/\s+/);
  if (parts.length <= 1) {
    return { firstName: fullName, lastName: '' };
  }

  return {
    firstName: parts.slice(0, -1).join(' '),
    lastName: parts.at(-1) ?? '',
  };
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
  const { firstName, lastName } = splitName(form.fullName);

  function handleChange(field: keyof UpdateProfilePayload, value: string) {
    setForm((prev) => ({ ...prev, [field]: value }));
  }

  function handleNamePart(field: 'firstName' | 'lastName', value: string) {
    const newFirstName = field === 'firstName' ? value : firstName;
    const newLastName = field === 'lastName' ? value : lastName;
    setForm((prev) => ({ ...prev, fullName: [newFirstName, newLastName].filter(Boolean).join(' ') }));
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
      <section className={styles.grid}>
        <div className={styles.mainColumn}>
          <article className={styles.card}>
            <header className={styles.cardHeader}>
              <div>
                <p className={styles.eyebrow}>Hồ sơ</p>
                <h2>Chỉnh sửa thông tin</h2>
              </div>
            </header>

            {error ? <p className={styles.errorNote}>{error}</p> : null}

            <form className={styles.formGrid} onSubmit={handleSubmit}>
              <div className={styles.fieldBlock}>
                <span className={styles.label}>Tên</span>
                <input
                  className={styles.fieldInput}
                  type="text"
                  value={lastName}
                  onChange={(e) => handleNamePart('lastName', e.target.value)}
                  placeholder="Nguyễn"
                />
              </div>

              <div className={styles.fieldBlock}>
                <span className={styles.label}>Họ và tên đệm</span>
                <input
                  className={styles.fieldInput}
                  type="text"
                  value={firstName}
                  onChange={(e) => handleNamePart('firstName', e.target.value)}
                  placeholder="Văn A"
                />
              </div>

              <div className={`${styles.fieldBlock} ${styles.fullWidth}`}>
                <span className={styles.label}>Địa chỉ email</span>
                <input
                  className={styles.fieldInput}
                  type="email"
                  value={email}
                  readOnly
                />
              </div>

              <div className={styles.fieldBlock}>
                <span className={styles.label}>Số điện thoại</span>
                <input
                  className={styles.fieldInput}
                  type="tel"
                  value={form.phone}
                  onChange={(e) => handleChange('phone', e.target.value)}
                  placeholder="0912 345 678"
                />
              </div>

              <div className={styles.fieldBlock}>
                <span className={styles.label}>Ngày sinh</span>
                <input
                  className={styles.fieldInput}
                  type="date"
                  value={form.dateOfBirth}
                  onChange={(e) => handleChange('dateOfBirth', e.target.value)}
                />
              </div>

              <div className={styles.fieldBlock}>
                <span className={styles.label}>Giới tính</span>
                <select
                  className={styles.fieldInput}
                  value={form.gender}
                  onChange={(e) => handleChange('gender', e.target.value)}
                >
                  <option value="">Chưa đặt</option>
                  <option value="male">Nam</option>
                  <option value="female">Nữ</option>
                  <option value="other">Khác</option>
                </select>
              </div>

              <div className={styles.formActions}>
                <button
                  type="submit"
                  className={styles.submitButton}
                  disabled={saving}
                >
                  {saving ? 'Đang lưu...' : 'Lưu thông tin'}
                </button>
                <button
                  type="button"
                  className={styles.cancelButton}
                  onClick={onSaved}
                  disabled={saving}
                >
                  Hủy
                </button>
              </div>
            </form>
          </article>
        </div>

        <aside className={styles.sideColumn}>
          <article className={styles.card}>
            <header className={styles.widgetHeaderCompact}>
              <span className={styles.iconBadge} aria-hidden="true">◆</span>
              <h3>Hướng dẫn</h3>
            </header>
            <p className={styles.helperText}>
              Giữ thông tin liên hệ chính xác giúp chúng tôi liên lạc với bạn khi cần xác nhận đơn hàng
              hoặc thông báo khuyến mại.
            </p>
            <p className={styles.helperText}>
              Email không thể thay đổi trực tiếp. Liên hệ hỗ trợ nếu bạn cần cập nhật email.
            </p>
          </article>
        </aside>
      </section>
    </div>
  );
}
