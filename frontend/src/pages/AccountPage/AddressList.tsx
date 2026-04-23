import { useEffect, useState } from 'react';
import type { UserAddress } from '../../types/address';
import type { CreateAddressPayload } from '../../types/address';
import { getAddresses, createAddress, deleteAddress } from '../../api/user';
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
  const [addresses, setAddresses] = useState<UserAddress[]>([]);
  const [isAdding, setIsAdding] = useState(false);
  const [form, setForm] = useState<CreateAddressPayload>(EMPTY_FORM);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    getAddresses()
      .then(setAddresses)
      .catch((value: Error) => setError(value.message))
      .finally(() => setLoading(false));
  }, []);

  function handleChange(field: keyof CreateAddressPayload, value: string) {
    setForm((prev) => ({ ...prev, [field]: value }));
  }

  async function handleAdd(e: React.FormEvent) {
    e.preventDefault();
    try {
      const newAddr = await createAddress(form);
      setAddresses((prev) => [...prev, newAddr]);
      setForm(EMPTY_FORM);
      setIsAdding(false);
      setError(null);
    } catch (value) {
      setError(value instanceof Error ? value.message : 'Không thể tạo địa chỉ.');
    }
  }

  async function handleDelete(id: number) {
    try {
      await deleteAddress(id);
      setAddresses((prev) => prev.filter((a) => a.id !== id));
    } catch (value) {
      setError(value instanceof Error ? value.message : 'Không thể xóa địa chỉ.');
    }
  }

  return (
    <div className={styles.container}>
      <h1 className={styles.title}>THÔNG TIN ĐỊA CHỈ</h1>
      {loading ? <p>Đang tải địa chỉ...</p> : null}
      {error ? <p>{error}</p> : null}

      <div className={styles.header}>
        <button
          type="button"
          className={styles.addButton}
          onClick={() => setIsAdding(!isAdding)}
        >
          {isAdding ? 'HỦY' : 'NHẬP ĐỊA CHỈ MỚI'}
        </button>
      </div>

      {isAdding && (
        <form className={styles.addressForm} onSubmit={handleAdd}>
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
          <button type="submit" className={styles.saveButton}>
            LƯU ĐỊA CHỈ
          </button>
        </form>
      )}

      <div className={styles.addressList}>
        {addresses.map((addr) => (
          <div key={addr.id} className={styles.addressCard}>
            <p className={styles.recipient}>
              {addr.recipientName} - {addr.recipientPhone}
            </p>
            <p className={styles.addressText}>
              {addr.addressLine}, {addr.ward ?? ''} {addr.district}, {addr.province}
            </p>
            {addr.isDefault && <span className={styles.badge}>Mặc định</span>}
            <div className={styles.cardActions}>
              {!addr.isDefault && (
                <button
                  type="button"
                  className={styles.deleteButton}
                  onClick={() => handleDelete(addr.id)}
                >
                  Xóa
                </button>
              )}
            </div>
          </div>
        ))}
        {!loading && addresses.length === 0 ? <p>Chưa có địa chỉ nào.</p> : null}
      </div>
    </div>
  );
}
