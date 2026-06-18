import { useEffect, useId, useMemo, useState } from 'react';
import type { CreateAddressPayload, UserAddress } from '../../types/address';
import { useAddressesQuery } from '../../hooks/user/useUserQueries';
import { useCreateAddressMutation, useDeleteAddressMutation, useUpdateAddressMutation } from '../../hooks/user/useUserMutations';
import { getProvinces, getWardsByProvince, type ProvinceOption, type WardOption } from '../../api/provinces';
import styles from './AddressList.module.css';

const EMPTY_FORM: CreateAddressPayload = {
  recipientName: '',
  recipientPhone: '',
  province: '',
  district: '',
  ward: '',
  addressLine: '',
  isDefault: false,
};

interface SearchableComboboxProps {
  label: string;
  placeholder: string;
  value: string;
  options: ProvinceOption[];
  disabled?: boolean;
  required?: boolean;
  onSelect: (option: ProvinceOption) => void;
}

function SearchableCombobox({ label, placeholder, value, options, disabled = false, required = false, onSelect }: SearchableComboboxProps) {
  const inputId = useId();
  const listboxId = useId();
  const [comboState, setComboState] = useState({ value, query: value });
  const [open, setOpen] = useState(false);
  const query = comboState.value === value ? comboState.query : value;

  const filteredOptions = useMemo(() => {
    const normalized = query.trim().toLowerCase();
    if (!normalized) return options.slice(0, 80);
    return options
      .filter((option) => option.name.toLowerCase().includes(normalized))
      .slice(0, 80);
  }, [options, query]);

  function handleSelect(option: ProvinceOption) {
    setComboState({ value: option.name, query: option.name });
    setOpen(false);
    onSelect(option);
  }

  function handleQueryChange(nextQuery: string) {
    setComboState({ value, query: nextQuery });
    setOpen(true);

    const exactMatch = options.find((option) => option.name.toLowerCase() === nextQuery.trim().toLowerCase());
    if (exactMatch) {
      handleSelect(exactMatch);
    }
  }

  function handleKeyDown(event: React.KeyboardEvent<HTMLInputElement>) {
    if (event.key === 'Enter' && filteredOptions[0]) {
      event.preventDefault();
      handleSelect(filteredOptions[0]);
    }
  }

  return (
    <div className={styles.comboField}>
      <label className={styles.comboLabel} htmlFor={inputId}>{label}</label>
      <div className={styles.comboBox}>
        <input
          id={inputId}
          className={styles.input}
          placeholder={placeholder}
          value={query}
          disabled={disabled}
          required={required}
          role="combobox"
          aria-controls={listboxId}
          aria-expanded={open}
          aria-autocomplete="list"
          onFocus={() => setOpen(true)}
          onBlur={() => window.setTimeout(() => setOpen(false), 180)}
          onChange={(event) => handleQueryChange(event.target.value)}
          onKeyDown={handleKeyDown}
        />
        <span className={styles.comboChevron} aria-hidden="true">⌄</span>
        {open && !disabled ? (
          <div id={listboxId} className={styles.comboMenu} role="listbox">
            {filteredOptions.length > 0 ? filteredOptions.map((option) => (
              <button
                key={option.code}
                type="button"
                className={styles.comboOption}
                onPointerDown={(event) => {
                  event.preventDefault();
                  handleSelect(option);
                }}
                role="option"
                aria-selected={option.name === value}
              >
                {option.name}
              </button>
            )) : <div className={styles.comboEmpty}>Không tìm thấy kết quả.</div>}
          </div>
        ) : null}
      </div>
    </div>
  );
}

function toForm(address: UserAddress): CreateAddressPayload {
  return {
    recipientName: address.recipientName,
    recipientPhone: address.recipientPhone,
    province: address.province,
    district: '',
    ward: address.ward ?? '',
    addressLine: address.addressLine,
    isDefault: address.isDefault,
  };
}

export default function AddressList() {
  const addressesQuery = useAddressesQuery();
  const createAddressMutation = useCreateAddressMutation();
  const updateAddressMutation = useUpdateAddressMutation();
  const deleteAddressMutation = useDeleteAddressMutation();
  const addresses = addressesQuery.data ?? [];
  const [isAdding, setIsAdding] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<CreateAddressPayload>(EMPTY_FORM);
  const [error, setError] = useState<string | null>(null);
  const [locationError, setLocationError] = useState<string | null>(null);
  const [provinces, setProvinces] = useState<ProvinceOption[]>([]);
  const [wards, setWards] = useState<WardOption[]>([]);
  const [selectedProvinceCode, setSelectedProvinceCode] = useState<number | null>(null);
  const loadError = addressesQuery.error instanceof Error ? addressesQuery.error.message : null;
  const loading = addressesQuery.isPending;
  const isEditing = editingId !== null;
  const showForm = isAdding || isEditing;
  const submitting = createAddressMutation.isPending || updateAddressMutation.isPending;

  const provinceByName = useMemo(() => new Map(provinces.map((item) => [item.name, item.code])), [provinces]);

  useEffect(() => {
    let ignore = false;
    getProvinces()
      .then((items) => {
        if (!ignore) setProvinces(items);
      })
      .catch(() => {
        if (!ignore) setLocationError('Không thể tải danh sách tỉnh/thành. Vui lòng thử lại sau.');
      });
    return () => {
      ignore = true;
    };
  }, []);

  useEffect(() => {
    if (!selectedProvinceCode) return;
    let ignore = false;
    getWardsByProvince(selectedProvinceCode)
      .then((items) => {
        if (!ignore) setWards(items);
      })
      .catch(() => {
        if (!ignore) setLocationError('Không thể tải danh sách phường/xã.');
      });
    return () => {
      ignore = true;
    };
  }, [selectedProvinceCode]);

  function resetForm() {
    setForm(EMPTY_FORM);
    setIsAdding(false);
    setEditingId(null);
    setSelectedProvinceCode(null);
    setWards([]);
  }

  function handleChange(field: keyof CreateAddressPayload, value: string | boolean) {
    setForm((prev) => ({ ...prev, [field]: value }));
  }

  function handleProvinceSelect(option: ProvinceOption) {
    setSelectedProvinceCode(option.code);
    setWards([]);
    setForm((prev) => ({ ...prev, province: option.name, district: '', ward: '' }));
  }

  function handleWardSelect(option: WardOption) {
    setForm((prev) => ({ ...prev, ward: option.name, district: '' }));
  }

  function handleStartAdd() {
    resetForm();
    setIsAdding(true);
    setError(null);
  }

  async function handleStartEdit(address: UserAddress) {
    setIsAdding(false);
    setEditingId(address.id);
    setForm(toForm(address));
    setError(null);

    const provinceCode = provinceByName.get(address.province) ?? null;
    setSelectedProvinceCode(provinceCode);
    if (provinceCode) {
      const loadedWards = await getWardsByProvince(provinceCode).catch(() => []);
      setWards(loadedWards);
    }
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!form.province.trim() || !form.ward?.trim()) {
      setError('Vui lòng chọn tỉnh/thành phố và phường/xã từ danh sách.');
      return;
    }

    const payload = { ...form, district: '' };
    try {
      if (editingId) {
        await updateAddressMutation.mutateAsync({ id: editingId, payload });
      } else {
        await createAddressMutation.mutateAsync(payload);
      }
      resetForm();
      setError(null);
    } catch (value) {
      setError(value instanceof Error ? value.message : 'Không thể lưu địa chỉ.');
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
    return [addr.addressLine, addr.ward, addr.province]
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
          onClick={() => (showForm ? resetForm() : handleStartAdd())}
        >
          <span aria-hidden="true">+</span>
          {showForm ? 'Hủy' : 'Thêm địa chỉ mới'}
        </button>
      </header>

      {loading ? <p className={styles.stateNote}>Đang tải địa chỉ...</p> : null}
      {loadError ? <p className={styles.stateNote}>{loadError}</p> : null}
      {error ? <p className={styles.stateNote}>{error}</p> : null}
      {locationError ? <p className={styles.stateNote}>{locationError}</p> : null}

      {showForm && (
        <form className={styles.form} onSubmit={handleSubmit}>
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
            <SearchableCombobox
              label="Tỉnh/Thành phố"
              placeholder="Tìm tỉnh/thành phố"
              value={form.province}
              options={provinces}
              onSelect={handleProvinceSelect}
              required
            />
            <SearchableCombobox
              label="Phường/Xã"
              placeholder={selectedProvinceCode ? 'Tìm phường/xã' : 'Chọn tỉnh/thành trước'}
              value={form.ward ?? ''}
              options={wards}
              onSelect={handleWardSelect}
              disabled={!selectedProvinceCode}
              required
            />
            <input
              className={styles.input}
              placeholder="Số nhà, tên đường"
              value={form.addressLine}
              onChange={(e) => handleChange('addressLine', e.target.value)}
              required
            />
            <label className={styles.checkboxRow}>
              <input type="checkbox" checked={!!form.isDefault} onChange={(e) => handleChange('isDefault', e.target.checked)} />
              Đặt làm địa chỉ mặc định
            </label>
          </div>
          <button type="submit" className={styles.saveButton} disabled={submitting}>
            {editingId ? 'Cập nhật địa chỉ' : 'Lưu địa chỉ'}
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
                  {addr.isDefault ? <span className={styles.defaultBadge}>Mặc định</span> : null}
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
              <button type="button" className={styles.editButton} onClick={() => void handleStartEdit(addr)}>
                <span aria-hidden="true">✎</span> Chỉnh sửa
              </button>
              <button type="button" className={styles.deleteButton} onClick={() => handleDelete(addr.id)}>
                <span aria-hidden="true">✕</span> Xóa
              </button>
            </div>
          </article>
        ))}

        {!loading && !showForm && (
          <button type="button" className={styles.addCard} onClick={handleStartAdd}>
            <span className={styles.addCardIcon} aria-hidden="true">+</span>
            <span>Thêm địa chỉ giao hàng mới</span>
          </button>
        )}
      </div>

      {!loading && addresses.length === 0 && !showForm ? <p className={styles.stateNote}>Chưa có địa chỉ nào.</p> : null}
    </div>
  );
}
