import { useState } from 'react';
import type { CreateAddressPayload } from '../../types/address';
import { useAddressesQuery } from '../../hooks/user/useUserQueries';
import { useCreateAddressMutation, useDeleteAddressMutation } from '../../hooks/user/useUserMutations';
import styles from './AddressList.module.css';

const EMPTY_FORM: CreateAddressPayload = {
  recipientName: '',
  recipientPhone: '',
  province: '',
  district: '',
  ward: '',
  addressLine: '',
};

export default function AddressList() {
  const addressesQuery = useAddressesQuery();
  const createAddressMutation = useCreateAddressMutation();
  const deleteAddressMutation = useDeleteAddressMutation();
  const addresses = addressesQuery.data ?? [];
  const [isAdding, setIsAdding] = useState(false);
  const [form, setForm] = useState<CreateAddressPayload>(EMPTY_FORM);
  const [error, setError] = useState<string | null>(null);
  const loadError = addressesQuery.error instanceof Error ? addressesQuery.error.message : null;
  const loading = addressesQuery.isPending;

  function handleChange(field: keyof CreateAddressPayload, value: string) {
    setForm((prev) => ({ ...prev, [field]: value }));
  }

  async function handleAdd(e: React.FormEvent) {
    e.preventDefault();
    try {
      await createAddressMutation.mutateAsync(form);
      setForm(EMPTY_FORM);
      setIsAdding(false);
      setError(null);
    } catch (value) {
      setError(value instanceof Error ? value.message : 'Không thể tạo địa chỉ.');
    }
  }

  async function handleDelete(id: string) {
    try {
      await deleteAddressMutation.mutateAsync(id);
    } catch (value) {
      setError(value instanceof Error ? value.message : 'Không thể xóa địa chỉ.');
    }
  }

  function formatFullAddress(addr: typeof addresses[number]) {
    return [addr.addressLine, addr.ward, addr.district, addr.province]
      .filter(Boolean)
      .join(', ');
  }

  return (
    <div className={styles.container}>
      <header className={styles.pageHeader}>
        <h1>Danh sách địa chỉ</h1>
        <button
          type="button"
          className={styles.addButton}
          onClick={() => setIsAdding(!isAdding)}
        >
          <span aria-hidden="true">+</span>
          {isAdding ? 'Hủy' : 'Thêm địa chỉ mới'}
        </button>
      </header>

      {loading ? <p className={styles.stateNote}>Đang tải địa chỉ...</p> : null}
      {loadError ? <p className={styles.stateNote}>{loadError}</p> : null}
      {error ? <p className={styles.stateNote}>{error}</p> : null}

      {isAdding && (
        <form className={styles.form} onSubmit={handleAdd}>
          <div className={styles.formGrid}>
            <input
              className={styles.input}
              placeholder="Tên người nhận"
              value={form.recipientName}
              onChange={(e) => handleChange('recipientName', e.target.value)}
              required
            />
            <input
              className={styles.input}
              placeholder="Số điện thoại"
              value={form.recipientPhone}
              onChange={(e) => handleChange('recipientPhone', e.target.value)}
              required
            />
            <input
              className={styles.input}
              placeholder="Tỉnh/Thành phố"
              value={form.province}
              onChange={(e) => handleChange('province', e.target.value)}
              required
            />
            <input
              className={styles.input}
              placeholder="Quận/Huyện"
              value={form.district}
              onChange={(e) => handleChange('district', e.target.value)}
              required
            />
            <input
              className={styles.input}
              placeholder="Phường/Xã"
              value={form.ward ?? ''}
              onChange={(e) => handleChange('ward', e.target.value)}
            />
            <input
              className={styles.input}
              placeholder="Số nhà, tên đường"
              value={form.addressLine}
              onChange={(e) => handleChange('addressLine', e.target.value)}
              required
            />
          </div>
          <button type="submit" className={styles.saveButton}>
            Lưu địa chỉ
          </button>
        </form>
      )}

      <div className={styles.grid}>
        {addresses.map((addr) => (
          <article key={addr.id} className={styles.card}>
            <div className={styles.cardBody}>
              <div className={styles.cardHeader}>
                <div>
                  <h3>{addr.recipientName}</h3>
                  {addr.isDefault ? (
                    <span className={styles.defaultBadge}>Mặc định</span>
                  ) : null}
                </div>
              </div>

              <div className={styles.cardFields}>
                <div className={styles.fieldRow}>
                  <span className={styles.fieldIcon} aria-hidden="true">✆</span>
                  <span>{addr.recipientPhone}</span>
                </div>
                <div className={styles.fieldRow}>
                  <span className={styles.fieldIcon} aria-hidden="true">⌂</span>
                  <span>{formatFullAddress(addr)}</span>
                </div>
              </div>
            </div>

            <div className={styles.cardActions}>
              <button type="button" className={styles.editButton}>
                <span aria-hidden="true">✎</span> Chỉnh sửa
              </button>
              <button
                type="button"
                className={styles.deleteButton}
                onClick={() => handleDelete(addr.id)}
              >
                <span aria-hidden="true">✕</span> Xóa
              </button>
            </div>
          </article>
        ))}

        {!loading && !isAdding && (
          <button
            type="button"
            className={styles.addCard}
            onClick={() => setIsAdding(true)}
          >
            <span className={styles.addCardIcon} aria-hidden="true">+</span>
            <span>Thêm địa chỉ giao hàng mới</span>
          </button>
        )}
      </div>

      {!loading && addresses.length === 0 && !isAdding ? (
        <p className={styles.stateNote}>Chưa có địa chỉ nào.</p>
      ) : null}
    </div>
  );
}
