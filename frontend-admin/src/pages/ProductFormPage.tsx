import { useState, useCallback } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { Plus, Trash2, Upload, ArrowLeft } from 'lucide-react'
import { useProducts } from '@/hooks/useProducts'
import { categories, type MockProduct, type MockVariant, type MockImage } from '@/data/mockProducts'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select } from '@/components/ui/select'
import { Textarea } from '@/components/ui/textarea'
import { Checkbox } from '@/components/ui/checkbox'
import { Label } from '@/components/ui/label'
import { Badge } from '@/components/ui/badge'
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

function emptyVariant(id: string): MockVariant {
  return { id, sku: '', variantName: '', size: '', color: '', price: 0, salePrice: null, stockQty: 0, isDefault: false }
}

export function ProductFormPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { getProduct, addProduct, updateProduct, deleteProduct, allProducts } = useProducts()

  const existing = id ? getProduct(id) : undefined

  const [name, setName] = useState(existing?.name ?? '')
  const [slug, setSlug] = useState(existing?.slug ?? '')
  const [type, setType] = useState<'ao-dai' | 'phu-kien'>(existing?.type ?? 'ao-dai')
  const [categoryId, setCategoryId] = useState(existing?.categoryId ?? 'cat-1-1')
  const [shortDesc, setShortDesc] = useState(existing?.shortDescription ?? '')
  const [desc, setDesc] = useState(existing?.description ?? '')
  const [material, setMaterial] = useState(existing?.material ?? '')
  const [brand, setBrand] = useState(existing?.brand ?? 'Nhã Uyên')
  const [origin, setOrigin] = useState(existing?.origin ?? 'Việt Nam')
  const [care, setCare] = useState(existing?.careInstruction ?? '')
  const [status, setStatus] = useState<'active' | 'draft' | 'inactive'>(existing?.status ?? 'draft')
  const [featured, setFeatured] = useState(existing?.isFeatured ?? false)
  const [variants, setVariants] = useState<MockVariant[]>(existing?.variants ?? [emptyVariant('v-1')])
  const [images, setImages] = useState<MockImage[]>(existing?.images ?? [])
  const [sortOrder] = useState(existing?.sortOrder ?? allProducts.length + 1)

  const handleNameChange = (val: string) => {
    setName(val)
    setSlug(slugify(val))
  }

  const addVariant = () => {
    setVariants([...variants, emptyVariant(`v-${Date.now()}`)])
  }

  const updateVariant = (vid: string, field: keyof MockVariant, value: unknown) => {
    setVariants(variants.map(v => v.id === vid ? { ...v, [field]: value } : v))
  }

  const removeVariant = (vid: string) => {
    if (variants.length <= 1) return
    setVariants(variants.filter(v => v.id !== vid))
  }

  const addImage = () => {
    const seed = `img-${Date.now()}`
    setImages([...images, { id: `img-${Date.now()}`, url: `https://picsum.photos/seed/${seed}/400/400`, altText: name, isPrimary: images.length === 0, sortOrder: images.length }])
  }

  const removeImage = (imgId: string) => {
    setImages(images.filter(img => img.id !== imgId))
  }

  const setPrimary = (imgId: string) => {
    setImages(images.map(img => ({ ...img, isPrimary: img.id === imgId })))
  }

  const categoryName = useCallback(() => {
    for (const cat of categories) {
      const child = cat.children?.find(c => c.id === categoryId)
      if (child) return `${cat.name} > ${child.name}`
    }
    return ''
  }, [categoryId])

  const handleSave = () => {
    if (!name.trim()) return

    const product: MockProduct = {
      id: existing?.id ?? `prod-${Date.now()}`,
      name, slug: slug || slugify(name), type, categoryId,
      categoryName: categoryName(),
      shortDescription: shortDesc, description: desc,
      material, brand, origin, careInstruction: care,
      status, isFeatured: featured, sortOrder,
      createdAt: existing?.createdAt ?? new Date().toISOString(),
      variants: variants.map(v => ({ ...v, price: Number(v.price) || 0, stockQty: Number(v.stockQty) || 0, salePrice: v.salePrice ? Number(v.salePrice) : null })),
      images: images.map((img, i) => ({ ...img, altText: img.altText || name, sortOrder: i })),
    }

    if (existing) {
      updateProduct(existing.id, product)
    } else {
      addProduct(product)
    }
    navigate('/admin/products')
  }

  const handleDelete = () => {
    if (existing) {
      deleteProduct(existing.id)
      navigate('/admin/products')
    }
  }

  const allCatOptions = categories.flatMap(c =>
    c.children?.map(ch => (
      <option key={ch.id} value={ch.id}>{c.name} &gt; {ch.name}</option>
    )) ?? []
  )

  return (
    <div>
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div className="flex items-center gap-3">
          <Button variant="ghost" size="icon" onClick={() => navigate('/admin/products')}>
            <ArrowLeft className="size-5" />
          </Button>
          <h1 className="text-2xl font-bold tracking-tight text-ink">
            {existing ? 'Chỉnh sửa sản phẩm' : 'Thêm sản phẩm mới'}
          </h1>
        </div>
        <div className="flex gap-3">
          <Button variant="outline" onClick={() => navigate('/admin/products')}>Hủy</Button>
          {existing && (
            <Button variant="destructive" onClick={handleDelete}>
              <Trash2 className="size-4" /> Xóa
            </Button>
          )}
          <Button className="bg-gold text-ink font-semibold hover:bg-gold/90" onClick={handleSave}>
            Lưu sản phẩm
          </Button>
        </div>
      </div>

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
                  <Input id="name" placeholder="Nhập tên sản phẩm..." value={name} onChange={e => handleNameChange(e.target.value)} />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="slug">Slug</Label>
                  <Input id="slug" placeholder="tu-dong-tao" value={slug} onChange={e => setSlug(e.target.value)} />
                </div>
              </div>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div className="space-y-2">
                  <Label htmlFor="type">Loại <span className="text-destructive">*</span></Label>
                  <Select id="type" value={type} onChange={e => setType(e.target.value as 'ao-dai' | 'phu-kien')}>
                    <option value="ao-dai">Áo dài</option>
                    <option value="phu-kien">Phụ kiện</option>
                  </Select>
                </div>
                <div className="space-y-2">
                  <Label htmlFor="cat">Danh mục</Label>
                  <Select id="cat" value={categoryId} onChange={e => setCategoryId(e.target.value)}>
                    {allCatOptions}
                  </Select>
                </div>
              </div>
              <div className="space-y-2">
                <Label htmlFor="short">Mô tả ngắn</Label>
                <Input id="short" placeholder="Mô tả ngắn gọn..." value={shortDesc} onChange={e => setShortDesc(e.target.value)} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="desc">Mô tả chi tiết</Label>
                <Textarea id="desc" placeholder="Mô tả đầy đủ..." rows={5} value={desc} onChange={e => setDesc(e.target.value)} />
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader><CardTitle className="text-lg">Chi tiết bổ sung</CardTitle></CardHeader>
            <CardContent className="space-y-4">
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                <div className="space-y-2">
                  <Label htmlFor="material">Chất liệu</Label>
                  <Input id="material" placeholder="Ví dụ: Lụa, cotton..." value={material} onChange={e => setMaterial(e.target.value)} />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="brand">Thương hiệu</Label>
                  <Input id="brand" placeholder="Nhã Uyên" value={brand} onChange={e => setBrand(e.target.value)} />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="origin">Xuất xứ</Label>
                  <Input id="origin" placeholder="Việt Nam" value={origin} onChange={e => setOrigin(e.target.value)} />
                </div>
              </div>
              <div className="space-y-2">
                <Label htmlFor="care">Hướng dẫn bảo quản</Label>
                <Textarea id="care" rows={2} value={care} onChange={e => setCare(e.target.value)} />
              </div>
              <div className="flex items-center gap-6">
                <div className="space-y-2">
                  <Label htmlFor="status">Trạng thái</Label>
                  <Select id="status" value={status} onChange={e => setStatus(e.target.value as 'active' | 'draft' | 'inactive')}>
                    <option value="draft">Bản nháp</option>
                    <option value="active">Đang bán</option>
                    <option value="inactive">Ngừng bán</option>
                  </Select>
                </div>
                <div className="flex items-center gap-2 pt-6">
                  <Checkbox id="featured" checked={featured} onChange={e => setFeatured(e.target.checked)} />
                  <Label htmlFor="featured">Sản phẩm nổi bật</Label>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Right: Variants + Images */}
        <div className="space-y-6">
          <Card>
            <CardHeader className="flex-row items-center justify-between">
              <CardTitle>Biến thể</CardTitle>
              <Button variant="outline" size="sm" onClick={addVariant}><Plus className="size-3" /> Thêm</Button>
            </CardHeader>
            <CardContent>
              {variants.map(v => (
                <div key={v.id} className="border rounded-lg p-3 mb-3 space-y-2">
                  <div className="grid grid-cols-2 gap-2">
                    <Input placeholder="SKU" value={v.sku} onChange={e => updateVariant(v.id, 'sku', e.target.value)} />
                    <Input placeholder="Tên biến thể" value={v.variantName} onChange={e => updateVariant(v.id, 'variantName', e.target.value)} />
                  </div>
                  <div className="grid grid-cols-2 gap-2">
                    <Input placeholder="Size" value={v.size} onChange={e => updateVariant(v.id, 'size', e.target.value)} />
                    <Input placeholder="Màu" value={v.color} onChange={e => updateVariant(v.id, 'color', e.target.value)} />
                  </div>
                  <div className="grid grid-cols-2 gap-2">
                    <Input type="number" placeholder="Giá (VNĐ)" value={v.price || ''} onChange={e => updateVariant(v.id, 'price', Number(e.target.value))} />
                    <Input type="number" placeholder="Giá sale" value={v.salePrice ?? ''} onChange={e => updateVariant(v.id, 'salePrice', e.target.value ? Number(e.target.value) : null)} />
                  </div>
                  <div className="flex items-center gap-2">
                    <Input type="number" placeholder="Tồn kho" value={v.stockQty || ''} onChange={e => updateVariant(v.id, 'stockQty', Number(e.target.value))} className="flex-1" />
                    {variants.length > 1 && (
                      <Button variant="ghost" size="icon" className="shrink-0 text-destructive" onClick={() => removeVariant(v.id)}>
                        <Trash2 className="size-4" />
                      </Button>
                    )}
                  </div>
                  <label className="flex items-center gap-2 text-xs">
                    <input type="radio" name="defaultVariant" checked={v.isDefault} onChange={() => setVariants(variants.map(x => ({ ...x, isDefault: x.id === v.id })))} />
                    Mặc định
                  </label>
                </div>
              ))}
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="flex-row items-center justify-between">
              <CardTitle>Hình ảnh</CardTitle>
              <Button variant="outline" size="sm" onClick={addImage}><Upload className="size-3" /> Thêm</Button>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-2 gap-3">
                {images.map(img => (
                  <div key={img.id} className="relative group rounded-lg overflow-hidden border">
                    <img src={img.url} alt={img.altText || name} className="aspect-square object-cover w-full" />
                    {img.isPrimary && (
                      <Badge className="absolute top-1 left-1 bg-gold text-ink text-[10px]">Chính</Badge>
                    )}
                    <div className="absolute top-1 right-1 flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                      {!img.isPrimary && (
                        <Button variant="secondary" size="icon" className="size-6" onClick={() => setPrimary(img.id)} title="Đặt làm ảnh chính">
                          <span className="text-[10px]">★</span>
                        </Button>
                      )}
                      <Button variant="destructive" size="icon" className="size-6" onClick={() => removeImage(img.id)}>
                        <Trash2 className="size-3" />
                      </Button>
                    </div>
                  </div>
                ))}
              </div>
              {images.length === 0 && (
                <p className="text-sm text-muted-foreground text-center py-4">Chưa có hình ảnh. Nhấn "Thêm" để thêm ảnh.</p>
              )}
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  )
}
