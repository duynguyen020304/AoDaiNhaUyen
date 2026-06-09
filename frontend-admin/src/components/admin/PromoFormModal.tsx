import { useState } from 'react'
import { Loader2, Save } from 'lucide-react'
import { ModalOverlay } from './ModalOverlay'
import { usePromoStore } from '@/stores/promoStore'
import type { AdminPromoItem } from '@/types/admin'
import { Button } from '@/components/ui/button'
import { Checkbox } from '@/components/ui/checkbox'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'

type DiscountType = 'percentage' | 'fixed'

interface Props {
  open: boolean
  onClose: () => void
  promo: AdminPromoItem | null
}

function toDateInput(value: string) {
  if (!value) return ''
  return new Date(value).toISOString().slice(0, 10)
}

function getErrorMessage(error: unknown, fallback: string) {
  return error instanceof Error ? error.message : fallback
}

export function PromoFormModal({ open, onClose, promo }: Props) {
  const { createPromo, updatePromo } = usePromoStore()
  const isEdit = Boolean(promo)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [code, setCode] = useState(() => promo?.code ?? '')
  const [discountType, setDiscountType] = useState<DiscountType>(() => promo?.discountType ?? 'percentage')
  const [discountValue, setDiscountValue] = useState(() => (promo ? String(promo.discountValue) : ''))
  const [minOrderAmount, setMinOrderAmount] = useState(() => (promo ? String(promo.minOrderAmount) : '0'))
  const [maxUses, setMaxUses] = useState(() => (promo ? String(promo.maxUses) : '0'))
  const [startDate, setStartDate] = useState(() => (promo ? toDateInput(promo.startDate) : ''))
  const [endDate, setEndDate] = useState(() => (promo ? toDateInput(promo.endDate) : ''))
  const [freeShipping, setFreeShipping] = useState(() => promo?.freeShipping ?? false)
  const [isActive, setIsActive] = useState(() => promo?.isActive ?? true)

  function validateForm() {
    const value = Number(discountValue)
    const minOrder = Number(minOrderAmount)
    const uses = Number(maxUses)
    if (!code.trim()) return 'Mã giảm giá là bắt buộc.'
    if (!Number.isFinite(value) || value <= 0) return 'Giá trị giảm giá phải lớn hơn 0.'
    if (discountType === 'percentage' && value > 100) return 'Phần trăm giảm giá không được vượt quá 100.'
    if (!Number.isFinite(minOrder) || minOrder < 0) return 'Đơn tối thiểu phải là số không âm.'
    if (!Number.isInteger(uses) || uses < 0) return 'Số lượt tối đa phải là số nguyên không âm.'
    if (startDate && endDate && new Date(endDate) <= new Date(startDate)) return 'Ngày kết thúc phải sau ngày bắt đầu.'
    return null
  }

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const validationError = validateForm()
    if (validationError) {
      setError(validationError)
      return
    }

    setSaving(true)
    setError(null)

    const payload = {
      code: code.trim().toUpperCase(),
      discountType,
      discountValue: Number(discountValue),
      minOrderAmount: Number(minOrderAmount),
      maxUses: Number(maxUses),
      startDate: startDate ? new Date(startDate).toISOString() : undefined,
      endDate: endDate ? new Date(endDate).toISOString() : undefined,
      freeShipping,
      isActive,
    }

    try {
      if (promo) await updatePromo(promo.id, payload)
      else await createPromo(payload)
      onClose()
    } catch (err) {
      setError(getErrorMessage(err, 'Không thể lưu mã giảm giá.'))
    } finally {
      setSaving(false)
    }
  }

  return (
    <ModalOverlay open={open} onClose={onClose} className="max-w-2xl overflow-hidden">
      <form onSubmit={handleSubmit}>
        <div className="border-b px-6 py-4 pr-12">
          <h2 className="text-lg font-semibold text-ink">{isEdit ? 'Sửa mã giảm giá' : 'Tạo mã giảm giá'}</h2>
          <p className="text-sm text-muted-foreground">Thiết lập điều kiện áp dụng mã khuyến mãi.</p>
        </div>

        <div className="max-h-[70dvh] overflow-y-auto px-6 py-5">
          {error && <div className="rounded-lg border border-destructive/30 bg-destructive/5 px-4 py-3 text-sm text-destructive mb-4">{error}</div>}

          <div className="grid gap-5">
            <div className="grid gap-2">
              <Label htmlFor="promo-code">Mã giảm giá</Label>
              <Input id="promo-code" value={code} onChange={(event) => setCode(event.target.value.toUpperCase())} maxLength={50} required />
              <p className="text-xs text-muted-foreground">Dùng chữ in hoa, không trùng mã đã tồn tại.</p>
            </div>

            <div className="grid gap-5 md:grid-cols-2">
              <div className="grid gap-2">
                <Label htmlFor="promo-discount-type">Loại giảm giá</Label>
                <Select id="promo-discount-type" value={discountType} onChange={(event) => setDiscountType(event.target.value as DiscountType)}>
                  <option value="percentage">Phần trăm</option>
                  <option value="fixed">Số tiền</option>
                </Select>
              </div>
              <div className="grid gap-2">
                <Label htmlFor="promo-discount-value">Giá trị</Label>
                <Input id="promo-discount-value" type="number" min="0" step="0.01" value={discountValue} onChange={(event) => setDiscountValue(event.target.value)} required />
              </div>
            </div>

            <div className="grid gap-5 md:grid-cols-2">
              <div className="grid gap-2">
                <Label htmlFor="promo-min-order">Đơn tối thiểu</Label>
                <Input id="promo-min-order" type="number" min="0" step="1000" value={minOrderAmount} onChange={(event) => setMinOrderAmount(event.target.value)} />
              </div>
              <div className="grid gap-2">
                <Label htmlFor="promo-max-uses">Số lượt tối đa</Label>
                <Input id="promo-max-uses" type="number" min="0" step="1" value={maxUses} onChange={(event) => setMaxUses(event.target.value)} />
                <p className="text-xs text-muted-foreground">Nhập 0 để không giới hạn.</p>
              </div>
            </div>

            <div className="grid gap-5 md:grid-cols-2">
              <div className="grid gap-2">
                <Label htmlFor="promo-start-date">Ngày bắt đầu</Label>
                <Input id="promo-start-date" type="date" value={startDate} onChange={(event) => setStartDate(event.target.value)} />
              </div>
              <div className="grid gap-2">
                <Label htmlFor="promo-end-date">Ngày kết thúc</Label>
                <Input id="promo-end-date" type="date" value={endDate} onChange={(event) => setEndDate(event.target.value)} />
              </div>
            </div>

            <div className="grid gap-3 rounded-lg border bg-cream/50 p-4">
              <label className="flex items-center gap-3 text-sm font-medium">
                <Checkbox checked={freeShipping} onChange={(event) => setFreeShipping(event.target.checked)} />
                Miễn phí vận chuyển
              </label>
              <label className="flex items-center gap-3 text-sm font-medium">
                <Checkbox checked={isActive} onChange={(event) => setIsActive(event.target.checked)} />
                Kích hoạt mã ngay
              </label>
            </div>
          </div>
        </div>

        <div className="flex justify-end gap-3 border-t bg-cream/30 px-6 py-4">
          <Button type="button" variant="outline" onClick={onClose}>Hủy</Button>
          <Button type="submit" disabled={saving}>
            {saving ? <Loader2 className="size-4 animate-spin" /> : <Save className="size-4" />}
            {saving ? 'Đang lưu...' : 'Lưu mã'}
          </Button>
        </div>
      </form>
    </ModalOverlay>
  )
}
