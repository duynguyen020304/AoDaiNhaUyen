import { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { ArrowLeft, Loader2 } from 'lucide-react'
import { useProductStore } from '@/stores/productStore'
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
        setIsLoading(false)
      })
      .catch((err) => {
        if (cancelled) return
        setError(err instanceof Error ? err.message : 'Không thể tải sản phẩm.')
        setIsLoading(false)
      })
    return () => { cancelled = true }
  }, [id, getProduct])

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
            <CardHeader><CardTitle className="text-lg">Biến thể</CardTitle></CardHeader>
            <CardContent>
              {id ? (
                <p className="text-sm text-muted-foreground">Biến thể hiển thị khi xem chi tiết sản phẩm. Quản lý biến thể sẽ có ở bản cập nhật sau.</p>
              ) : (
                <p className="text-sm text-muted-foreground">Lưu sản phẩm trước rồi quay lại để thêm biến thể.</p>
              )}
            </CardContent>
          </Card>

          <Card>
            <CardHeader><CardTitle className="text-lg">Hình ảnh</CardTitle></CardHeader>
            <CardContent>
              <p className="text-sm text-muted-foreground">Tải lên và quản lý hình ảnh sản phẩm sẽ có ở bản cập nhật sau.</p>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  )
}
