import { useState, useEffect, useRef } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { ArrowLeft, Loader2, Globe, FileX, Upload, Trash2, Star, EyeOff, PackageCheck } from 'lucide-react'
import { useProductStore } from '@/stores/productStore'
import type { AdminImageResponse, AdminVariantResponse } from '@/types/admin'
import { useCategoryStore } from '@/stores/categoryStore'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select } from '@/components/ui/select'
import { Textarea } from '@/components/ui/textarea'
import { Checkbox } from '@/components/ui/checkbox'
import { Label } from '@/components/ui/label'
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card'

function slugify(text: string) {
  return text
    .toLowerCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/đ/g, 'd')
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-|-$/g, '')
}

export function ProductFormPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { getProduct, createProduct, updateProduct } = useProductStore()
  const { categories, fetchCategories, loading: catLoading } = useCategoryStore()

  const [isLoading, setIsLoading] = useState(!!id)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const [name, setName] = useState('')
  const [slug, setSlug] = useState('')
  const [productType, setProductType] = useState('ao_dai')
  const [categoryId, setCategoryId] = useState('')
  const [shortDesc, setShortDesc] = useState('')
  const [desc, setDesc] = useState('')
  const [material, setMaterial] = useState('')
  const [brand, setBrand] = useState('Nhã Uyên')
  const [origin, setOrigin] = useState('Việt Nam')
  const [care, setCare] = useState('')
  const [status, setStatus] = useState('draft')
  const [featured, setFeatured] = useState(false)
  const [variants, setVariants] = useState<AdminVariantResponse[]>([])
  const [stockInputs, setStockInputs] = useState<Record<string, string>>({})
  const [savingStockId, setSavingStockId] = useState<string | null>(null)
  const [editingVariantId, setEditingVariantId] = useState<string | null>(null)
  const [editSku, setEditSku] = useState('')
  const [editVariantName, setEditVariantName] = useState('')
  const [editSize, setEditSize] = useState('')
  const [editColor, setEditColor] = useState('')
  const [editPrice, setEditPrice] = useState('')
  const [editSalePrice, setEditSalePrice] = useState('')
  const [editStockQty, setEditStockQty] = useState('')
  const [editStatus, setEditStatus] = useState('active')
  const [editIsDefault, setEditIsDefault] = useState(false)
  const [savingVariantId, setSavingVariantId] = useState<string | null>(null)
  const [images, setImages] = useState<AdminImageResponse[]>([])
  const [uploading, setUploading] = useState(false)
  const fileInputRef = useRef<HTMLInputElement>(null)

  useEffect(() => {
    fetchCategories()
  }, [fetchCategories])

  useEffect(() => {
    if (!id) return
    let cancelled = false
    getProduct(id)
      .then((product) => {
        if (cancelled) return
        setName(product.name)
        setSlug(product.slug)
        setProductType(product.productType)
        setCategoryId(product.categoryId)
        setShortDesc(product.shortDescription ?? '')
        setDesc(product.description ?? '')
        setMaterial(product.material ?? '')
        setBrand(product.brand ?? 'Nhã Uyên')
        setOrigin(product.origin ?? 'Việt Nam')
        setCare(product.careInstruction ?? '')
        setStatus(product.status)
        setFeatured(product.isFeatured)
        setVariants(product.variants)
        setStockInputs(Object.fromEntries(product.variants.map((variant) => [variant.id, String(variant.stockQty)])))
        setImages(product.images)
        setIsLoading(false)
      })
      .catch((err) => {
        if (cancelled) return
        setError(err instanceof Error ? err.message : 'Không thể tải sản phẩm.')
        setIsLoading(false)
      })
    return () => { cancelled = true }
  }, [id, getProduct])

  const handlePublishToggle = async () => {
    if (!id) return
    const newStatus = status === 'active' ? 'draft' : 'active'
    try {
      await useProductStore.getState().toggleProductStatus(id, newStatus)
      setStatus(newStatus)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Không thể cập nhật trạng thái.')
    }
  }

  const handleStockInputChange = (variantId: string, value: string) => {
    if (!/^\d*$/.test(value)) return
    setStockInputs((prev) => ({ ...prev, [variantId]: value }))
  }

  const handleSaveStock = async (variant: AdminVariantResponse) => {
    if (!id) return
    const raw = stockInputs[variant.id] ?? ''
    const nextStock = Number(raw)
    if (!raw || !Number.isInteger(nextStock) || nextStock < 0) {
      setError('Tồn kho phải là số nguyên không âm.')
      return
    }

    setSavingStockId(variant.id)
    setError(null)
    try {
      const product = await useProductStore.getState().updateVariantStock(id, variant.id, nextStock)
      setVariants(product.variants)
      setStockInputs(Object.fromEntries(product.variants.map((item) => [item.id, String(item.stockQty)])))
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Không thể cập nhật tồn kho.')
    } finally {
      setSavingStockId(null)
    }
  }

  const handleEditVariantClick = (variant: AdminVariantResponse) => {
    setEditingVariantId(variant.id)
    setEditSku(variant.sku)
    setEditVariantName(variant.variantName ?? '')
    setEditSize(variant.size ?? '')
    setEditColor(variant.color ?? '')
    setEditPrice(String(variant.price))
    setEditSalePrice(variant.salePrice !== null ? String(variant.salePrice) : '')
    setEditStockQty(String(variant.stockQty))
    setEditStatus(variant.status)
    setEditIsDefault(variant.isDefault)
  }

  const handleSaveVariant = async (variantId: string) => {
    if (!id) return
    if (!editSku.trim()) {
      setError('SKU không được để trống.')
      return
    }
    const priceVal = Number(editPrice)
    if (isNaN(priceVal) || priceVal < 0) {
      setError('Giá tiền không hợp lệ.')
      return
    }
    const salePriceVal = editSalePrice.trim() ? Number(editSalePrice) : null
    if (salePriceVal !== null && (isNaN(salePriceVal) || salePriceVal < 0)) {
      setError('Giá khuyến mãi không hợp lệ.')
      return
    }
    const stockQtyVal = Number(editStockQty)
    if (isNaN(stockQtyVal) || !Number.isInteger(stockQtyVal) || stockQtyVal < 0) {
      setError('Số lượng tồn kho không hợp lệ.')
      return
    }

    setSavingVariantId(variantId)
    setError(null)
    try {
      const payload = {
        sku: editSku.trim(),
        variantName: editVariantName.trim() || null,
        size: editSize.trim() || null,
        color: editColor.trim() || null,
        price: priceVal,
        salePrice: salePriceVal,
        stockQty: stockQtyVal,
        isDefault: editIsDefault,
        status: editStatus
      }
      const product = await useProductStore.getState().updateVariant(id, variantId, payload)
      setVariants(product.variants)
      setStockInputs(Object.fromEntries(product.variants.map((item) => [item.id, String(item.stockQty)])))
      setEditingVariantId(null)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Không thể cập nhật biến thể.')
    } finally {
      setSavingVariantId(null)
    }
  }

  const handleFileUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    if (!id) return
    const file = e.target.files?.[0]
    if (!file) return
    
    setUploading(true)
    setError(null)
    try {
      await useProductStore.getState().uploadImage(id, file)
      const refreshed = await getProduct(id)
      setImages(refreshed.images)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Lỗi tải ảnh.')
    } finally {
      setUploading(false)
      if (fileInputRef.current) fileInputRef.current.value = ''
    }
  }

  const handleDeleteImage = async (imageId: string) => {
    if (!id) return
    if (!confirm('Bạn có chắc muốn xóa ảnh này?')) return
    try {
      await useProductStore.getState().deleteImage(id, imageId)
      setImages(prev => prev.filter(img => img.id !== imageId))
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Không thể xóa ảnh.')
    }
  }

  const handleSetPrimaryImage = async (imageId: string) => {
    if (!id) return
    try {
      await useProductStore.getState().setPrimaryImage(id, imageId)
      setImages(prev => prev.map(img => ({ ...img, isPrimary: img.id === imageId })))
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Lỗi cập nhật ảnh chính.')
    }
  }

  const handleToggleImageVisibility = async (imageId: string, currentPublic: boolean) => {
    if (!id) return
    try {
      await useProductStore.getState().toggleImageVisibility(id, imageId, !currentPublic)
      setImages(prev => prev.map(img => img.id === imageId ? { ...img, isPublic: !currentPublic } : img))
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Lỗi chuyển trạng thái hiển thị ảnh.')
    }
  }

  const handleNameChange = (val: string) => {
    setName(val)
    if (!id) setSlug(slugify(val))
  }

  const handleSave = async () => {
    if (!name.trim()) return
    setSaving(true)
    setError(null)
    const data = {
      name: name.trim(),
      slug: slug.trim() || slugify(name.trim()),
      productType,
      categoryId,
      shortDescription: shortDesc.trim() || undefined,
      description: desc.trim() || undefined,
      material: material.trim() || undefined,
      brand: brand.trim() || undefined,
      origin: origin.trim() || undefined,
      careInstruction: care.trim() || undefined,
      status,
      isFeatured: featured,
    }
    try {
      if (id) {
        await updateProduct(id, data)
      } else {
        await createProduct(data)
      }
      navigate('/admin/products')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Không thể lưu sản phẩm.')
      setSaving(false)
    }
  }

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-20">
        <Loader2 className="size-8 animate-spin text-primary" />
      </div>
    )
  }

  const categoryOptions = categories
    .filter((c) => !c.parent) // top-level categories
    .flatMap((parent) => {
      const children = categories.filter((c) => c.parent === parent.id)
      if (children.length === 0) {
        return <option key={parent.id} value={parent.id}>{parent.name}</option>
      }
      return [
        <option key={parent.id} value={parent.id} disabled className="font-semibold text-ink">— {parent.name} —</option>,
        ...children.map((ch) => (
          <option key={ch.id} value={ch.id}>&nbsp;&nbsp;{ch.name}</option>
        )),
      ]
    })

  return (
    <div>
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div className="flex items-center gap-3">
          <Button variant="ghost" size="icon" onClick={() => navigate('/admin/products')}>
            <ArrowLeft className="size-5" />
          </Button>
          <h1 className="text-2xl font-bold tracking-tight text-ink">
            {id ? 'Chỉnh sửa sản phẩm' : 'Thêm sản phẩm mới'}
          </h1>
        </div>
        <div className="flex gap-3">
          {id && (
            <Button 
              variant="outline" 
              onClick={handlePublishToggle}
              disabled={saving}
              className={status === 'active' ? 'text-destructive border-destructive/30 hover:bg-destructive/10' : 'text-green-600 border-green-600/30 hover:bg-green-600/10'}
            >
              {status === 'active' ? <><FileX className="size-4 mr-2" /> Gỡ bán</> : <><Globe className="size-4 mr-2" /> Đăng bán</>}
            </Button>
          )}
          <Button variant="outline" onClick={() => navigate('/admin/products')} disabled={saving}>Hủy</Button>
          <Button className="bg-gold text-ink font-semibold hover:bg-gold/90" onClick={handleSave} disabled={saving}>
            {saving ? 'Đang lưu...' : 'Lưu sản phẩm'}
          </Button>
        </div>
      </div>

      {error && (
        <div className="rounded-lg border border-destructive/30 bg-destructive/5 px-4 py-3 text-sm text-destructive mb-4">
          {error}
        </div>
      )}

      {/* Main grid */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Left: Main info */}
        <div className="lg:col-span-2 space-y-6">
          <Card>
            <CardHeader><CardTitle className="text-lg">Thông tin sản phẩm</CardTitle></CardHeader>
            <CardContent className="space-y-4">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div className="space-y-2">
                  <Label htmlFor="name">Tên sản phẩm <span className="text-destructive">*</span></Label>
                  <Input id="name" placeholder="Nhập tên sản phẩm..." value={name} onChange={(e) => handleNameChange(e.target.value)} />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="slug">Slug</Label>
                  <Input id="slug" placeholder="tu-dong-tao" value={slug} onChange={(e) => setSlug(e.target.value)} />
                </div>
              </div>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div className="space-y-2">
                  <Label htmlFor="type">Loại <span className="text-destructive">*</span></Label>
                  <Select id="type" value={productType} onChange={(e) => setProductType(e.target.value)}>
                    <option value="ao_dai">Áo dài</option>
                    <option value="phu_kien">Phụ kiện</option>
                  </Select>
                </div>
                <div className="space-y-2">
                  <Label htmlFor="cat">Danh mục <span className="text-destructive">*</span></Label>
                  <Select id="cat" value={categoryId} onChange={(e) => setCategoryId(e.target.value)} disabled={catLoading}>
                    <option value="">{catLoading ? 'Đang tải...' : 'Chọn danh mục'}</option>
                    {categoryOptions}
                  </Select>
                </div>
              </div>
              <div className="space-y-2">
                <Label htmlFor="short">Mô tả ngắn</Label>
                <Input id="short" placeholder="Mô tả ngắn gọn..." value={shortDesc} onChange={(e) => setShortDesc(e.target.value)} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="desc">Mô tả chi tiết</Label>
                <Textarea id="desc" placeholder="Mô tả đầy đủ..." rows={5} value={desc} onChange={(e) => setDesc(e.target.value)} />
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader><CardTitle className="text-lg">Chi tiết bổ sung</CardTitle></CardHeader>
            <CardContent className="space-y-4">
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                <div className="space-y-2">
                  <Label htmlFor="material">Chất liệu</Label>
                  <Input id="material" placeholder="Ví dụ: Lụa, cotton..." value={material} onChange={(e) => setMaterial(e.target.value)} />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="brand">Thương hiệu</Label>
                  <Input id="brand" placeholder="Nhã Uyên" value={brand} onChange={(e) => setBrand(e.target.value)} />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="origin">Xuất xứ</Label>
                  <Input id="origin" placeholder="Việt Nam" value={origin} onChange={(e) => setOrigin(e.target.value)} />
                </div>
              </div>
              <div className="space-y-2">
                <Label htmlFor="care">Hướng dẫn bảo quản</Label>
                <Textarea id="care" rows={2} value={care} onChange={(e) => setCare(e.target.value)} />
              </div>
              <div className="flex items-center gap-6">
                <div className="space-y-2">
                  <Label htmlFor="status">Trạng thái</Label>
                  <Select id="status" value={status} onChange={(e) => setStatus(e.target.value)}>
                    <option value="draft">Bản nháp</option>
                    <option value="active">Đang bán</option>
                    <option value="inactive">Ngừng bán</option>
                    <option value="out_of_stock">Hết hàng</option>
                  </Select>
                </div>
                <div className="flex items-center gap-2 pt-6">
                  <Checkbox id="featured" checked={featured} onChange={(e) => setFeatured(e.target.checked)} />
                  <Label htmlFor="featured">Sản phẩm nổi bật</Label>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Right: Variants + Images placeholder */}
        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle className="text-lg flex items-center gap-2">
                <PackageCheck className="size-5 text-burgundy" />
                Biến thể và tồn kho
              </CardTitle>
            </CardHeader>
            <CardContent>
              {!id ? (
                <p className="text-sm text-muted-foreground">Lưu sản phẩm trước rồi quay lại để cập nhật tồn kho.</p>
              ) : variants.length === 0 ? (
                <p className="text-sm text-muted-foreground">Sản phẩm chưa có biến thể.</p>
              ) : (
                <div className="space-y-3">
                  {variants.map((variant) => {
                    const isEditing = editingVariantId === variant.id
                    if (isEditing) {
                      return (
                        <div key={variant.id} className="space-y-3 border-b border-border/70 pb-4 last:border-b-0 last:pb-0">
                          <div className="space-y-2">
                            <Label htmlFor={`edit-name-${variant.id}`}>Tên biến thể</Label>
                            <Input
                              id={`edit-name-${variant.id}`}
                              value={editVariantName}
                              onChange={(e) => setEditVariantName(e.target.value)}
                              placeholder="Mặc định"
                            />
                          </div>
                          <div className="grid grid-cols-2 gap-2">
                            <div className="space-y-2">
                              <Label htmlFor={`edit-sku-${variant.id}`}>SKU *</Label>
                              <Input
                                id={`edit-sku-${variant.id}`}
                                value={editSku}
                                onChange={(e) => setEditSku(e.target.value)}
                              />
                            </div>
                            <div className="space-y-2">
                              <Label htmlFor={`edit-status-${variant.id}`}>Trạng thái</Label>
                              <Select
                                id={`edit-status-${variant.id}`}
                                value={editStatus}
                                onChange={(e) => setEditStatus(e.target.value)}
                              >
                                <option value="active">Đang bán</option>
                                <option value="inactive">Ngừng bán</option>
                              </Select>
                            </div>
                          </div>
                          <div className="grid grid-cols-2 gap-2">
                            <div className="space-y-2">
                              <Label htmlFor={`edit-size-${variant.id}`}>Kích thước</Label>
                              <Input
                                id={`edit-size-${variant.id}`}
                                value={editSize}
                                onChange={(e) => setEditSize(e.target.value)}
                                placeholder="S, M, L..."
                              />
                            </div>
                            <div className="space-y-2">
                              <Label htmlFor={`edit-color-${variant.id}`}>Màu sắc</Label>
                              <Input
                                id={`edit-color-${variant.id}`}
                                value={editColor}
                                onChange={(e) => setEditColor(e.target.value)}
                                placeholder="Đỏ, Xanh..."
                              />
                            </div>
                          </div>
                          <div className="grid grid-cols-3 gap-2">
                            <div className="space-y-2">
                              <Label htmlFor={`edit-price-${variant.id}`}>Giá tiền *</Label>
                              <Input
                                id={`edit-price-${variant.id}`}
                                type="number"
                                value={editPrice}
                                onChange={(e) => setEditPrice(e.target.value)}
                              />
                            </div>
                            <div className="space-y-2">
                              <Label htmlFor={`edit-saleprice-${variant.id}`}>Khuyến mãi</Label>
                              <Input
                                id={`edit-saleprice-${variant.id}`}
                                type="number"
                                value={editSalePrice}
                                onChange={(e) => setEditSalePrice(e.target.value)}
                                placeholder="Trống"
                              />
                            </div>
                            <div className="space-y-2">
                              <Label htmlFor={`edit-stock-${variant.id}`}>Tồn kho *</Label>
                              <Input
                                id={`edit-stock-${variant.id}`}
                                type="number"
                                min={0}
                                step={1}
                                value={editStockQty}
                                onChange={(e) => setEditStockQty(e.target.value)}
                              />
                            </div>
                          </div>
                          <div className="flex justify-end gap-2 pt-2">
                            <Button
                              type="button"
                              variant="outline"
                              size="sm"
                              onClick={() => setEditingVariantId(null)}
                            >
                              Hủy
                            </Button>
                            <Button
                              type="button"
                              size="sm"
                              onClick={() => handleSaveVariant(variant.id)}
                              disabled={savingVariantId === variant.id}
                            >
                              {savingVariantId === variant.id ? <Loader2 className="size-4 animate-spin" /> : 'Lưu'}
                            </Button>
                          </div>
                        </div>
                      )
                    }

                    const stockValue = stockInputs[variant.id] ?? String(variant.stockQty)
                    const currentStock = Number(stockValue)
                    const isDirty = stockValue !== String(variant.stockQty)
                    const isInvalid = !stockValue || !Number.isInteger(currentStock) || currentStock < 0
                    return (
                      <div key={variant.id} className="space-y-2 border-b border-border/70 pb-4 last:border-b-0 last:pb-0">
                        <div className="flex items-start justify-between gap-3">
                          <div className="min-w-0">
                            <p className="font-medium text-sm text-ink truncate">{variant.variantName || variant.sku}</p>
                            <p className="text-xs text-muted-foreground font-mono truncate">{variant.sku}</p>
                            <p className="text-xs text-muted-foreground">
                              {[variant.size, variant.color].filter(Boolean).join(' / ') || 'Mặc định'}
                            </p>
                            <p className="text-xs font-semibold text-burgundy">
                              {variant.price.toLocaleString('vi-VN')} ₫
                              {variant.salePrice !== null && (
                                <span className="line-through text-muted-foreground ml-2 font-normal">
                                  {variant.salePrice.toLocaleString('vi-VN')} ₫
                                </span>
                              )}
                            </p>
                          </div>
                          <div className="flex flex-col items-end gap-2 shrink-0">
                            <span className={`rounded-full px-2 py-1 text-xs font-medium ${variant.stockQty <= 0 ? 'bg-destructive/10 text-destructive' : variant.stockQty <= 5 ? 'bg-orange-100 text-orange-700' : 'bg-green-100 text-green-700'}`}>
                              {variant.stockQty <= 0 ? 'Hết hàng' : variant.stockQty <= 5 ? 'Sắp hết' : 'Còn hàng'}
                            </span>
                            <Button
                              type="button"
                              variant="ghost"
                              size="sm"
                              className="text-xs h-7 px-2"
                              onClick={() => handleEditVariantClick(variant)}
                            >
                              Chỉnh sửa
                            </Button>
                          </div>
                        </div>
                        <div className="grid grid-cols-[1fr_auto] items-end gap-2">
                          <div className="space-y-2">
                            <Label htmlFor={`stock-${variant.id}`}>Tồn kho nhanh</Label>
                            <Input
                              id={`stock-${variant.id}`}
                              type="number"
                              min={0}
                              step={1}
                              inputMode="numeric"
                              value={stockValue}
                              onChange={(e) => handleStockInputChange(variant.id, e.target.value)}
                              aria-invalid={isInvalid}
                            />
                          </div>
                          <Button
                            type="button"
                            variant="outline"
                            onClick={() => handleSaveStock(variant)}
                            disabled={!isDirty || isInvalid || savingStockId === variant.id}
                          >
                            {savingStockId === variant.id ? <Loader2 className="size-4 animate-spin" /> : 'Lưu'}
                          </Button>
                        </div>
                      </div>
                    )
                  })}
                </div>
              )}
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle className="text-lg flex justify-between items-center">
                Hình ảnh
                {id && (
                  <div>
                    <input type="file" ref={fileInputRef} className="hidden" accept="image/*" onChange={handleFileUpload} />
                    <Button variant="outline" size="sm" onClick={() => fileInputRef.current?.click()} disabled={uploading}>
                      {uploading ? <Loader2 className="size-4 mr-2 animate-spin" /> : <Upload className="size-4 mr-2" />}
                      Tải lên
                    </Button>
                  </div>
                )}
              </CardTitle>
            </CardHeader>
            <CardContent>
              {!id ? (
                <p className="text-sm text-muted-foreground">Lưu sản phẩm trước khi tải ảnh lên.</p>
              ) : images.length === 0 ? (
                <p className="text-sm text-muted-foreground">Chưa có hình ảnh nào.</p>
              ) : (
                <div className="grid grid-cols-2 gap-4">
                  {images.sort((a, b) => a.sortOrder - b.sortOrder).map(img => (
                    <div key={img.id} className="relative group border rounded-lg overflow-hidden">
                      <img src={img.imageUrl} alt={img.altText || ''} className="w-full aspect-square object-cover" />
                      
                      <div className="absolute top-2 left-2 flex flex-col gap-1">
                        {img.isPrimary && <span className="bg-gold text-ink text-xs font-bold px-2 py-1 rounded shadow-sm">Ảnh chính</span>}
                        {img.isPublic ? (
                          <span className="bg-green-500 text-white text-xs font-medium px-2 py-1 rounded shadow-sm flex items-center gap-1"><Globe className="size-3" /> Công khai</span>
                        ) : (
                          <span className="bg-gray-600 text-white text-xs font-medium px-2 py-1 rounded shadow-sm flex items-center gap-1"><EyeOff className="size-3" /> Nội bộ</span>
                        )}
                      </div>

                      <div className="absolute inset-0 bg-black/60 opacity-0 group-hover:opacity-100 transition-opacity flex flex-col items-center justify-center gap-2">
                        {!img.isPrimary && (
                          <Button variant="secondary" size="sm" className="w-28" onClick={() => handleSetPrimaryImage(img.id)}>
                            <Star className="size-4 mr-2" /> Đặt chính
                          </Button>
                        )}
                        <Button 
                          variant="secondary" 
                          size="sm" 
                          className="w-28" 
                          onClick={() => handleToggleImageVisibility(img.id, img.isPublic)}
                          disabled={status === 'active' && img.isPublic}
                          title={status === 'active' && img.isPublic ? "Sản phẩm đang bán phải có ảnh công khai" : undefined}
                        >
                          {img.isPublic ? <><EyeOff className="size-4 mr-2" /> Ẩn</> : <><Globe className="size-4 mr-2" /> Public</>}
                        </Button>
                        <Button variant="destructive" size="sm" className="w-28" onClick={() => handleDeleteImage(img.id)}>
                          <Trash2 className="size-4 mr-2" /> Xóa
                        </Button>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  )
}
