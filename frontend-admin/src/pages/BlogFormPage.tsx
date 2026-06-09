import { useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { ArrowLeft, Save } from 'lucide-react'
import { uploadBlogImage } from '@/api/blog'
import { useBlogStore } from '@/stores/blogStore'
import { blogTemplates, type BlogBlock, type BlogPostPayload, type BlogStatus, type BlogTemplate } from '@/types/blog'
import { BlockEditor } from '@/components/blog/BlockEditor'
import { BlogPreview } from '@/components/blog/BlogPreview'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Select } from '@/components/ui/select'
import { Label } from '@/components/ui/label'

const initialBlocks: BlogBlock[] = [
  { type: 'heading', level: 2, content: 'Áo dài là gì?' },
  { type: 'paragraph', content: 'Áo dài là trang phục truyền thống Việt Nam, tôn dáng thanh lịch và giữ nét mềm mại trong từng đường cắt.' },
]

const templateLabels: Record<BlogTemplate, string> = {
  StandardArticle: 'Bài viết chuẩn',
  PhotoGallery: 'Thư viện ảnh',
  VideoFeature: 'Video nổi bật',
  ProductSpotlight: 'Giới thiệu sản phẩm',
  HowTo: 'Hướng dẫn',
}

function slugify(value: string) {
  return value
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/đ/g, 'd')
    .replace(/Đ/g, 'd')
    .toLowerCase()
    .replace(/[^a-z0-9\s-]/g, '')
    .trim()
    .replace(/[\s-]+/g, '-')
}

function splitTags(value: string) {
  return value.split(',').map((tag) => tag.trim()).filter(Boolean)
}

export function BlogFormPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const isEdit = Boolean(id)
  const { selectedPost, loading, error, fetchPost, createPost, updatePost, clearError } = useBlogStore()
  const [saving, setSaving] = useState(false)
  const [uploadingImage, setUploadingImage] = useState(false)
  const [tagsInput, setTagsInput] = useState('')
  const [activeTab, setActiveTab] = useState<'edit' | 'preview'>('edit')
  const [form, setForm] = useState<BlogPostPayload>({
    title: '',
    slug: '',
    excerpt: '',
    featuredImage: '',
    featuredImageWidth: 1200,
    featuredImageHeight: 630,
    template: 'StandardArticle',
    content: initialBlocks,
    tags: [],
    status: 'Draft',
    metaTitle: '',
    metaDescription: '',
    canonicalUrl: '',
    authorNameOverride: '',
    authorBio: '',
    reviewedBy: '',
    informationGain: '',
  })

  useEffect(() => {
    if (id) void fetchPost(id)
  }, [id, fetchPost])

  useEffect(() => {
    if (!isEdit || !selectedPost) return
    setTagsInput(selectedPost.tags.join(', '))
    setForm({
      title: selectedPost.title,
      slug: selectedPost.slug,
      excerpt: selectedPost.excerpt,
      featuredImage: selectedPost.featuredImage ?? '',
      featuredImageWidth: selectedPost.featuredImageWidth ?? 1200,
      featuredImageHeight: selectedPost.featuredImageHeight ?? 630,
      template: selectedPost.template,
      content: selectedPost.content,
      tags: selectedPost.tags,
      authorId: selectedPost.authorId,
      authorNameOverride: selectedPost.authorName ?? '',
      authorBio: selectedPost.authorBio ?? '',
      reviewedBy: selectedPost.reviewedBy ?? '',
      informationGain: selectedPost.informationGain ?? '',
      status: selectedPost.status,
      publishedAt: selectedPost.publishedAt,
      metaTitle: selectedPost.metaTitle ?? '',
      metaDescription: selectedPost.metaDescription ?? '',
      canonicalUrl: selectedPost.canonicalUrl ?? '',
    })
  }, [isEdit, selectedPost])

  const seoTitle = useMemo(() => form.metaTitle?.trim() || `${form.title} | Áo Dài Nhà Uyên`, [form.metaTitle, form.title])
  const seoDescription = useMemo(() => form.metaDescription?.trim() || form.excerpt, [form.metaDescription, form.excerpt])

  function patch(patch: Partial<BlogPostPayload>) {
    setForm((current) => ({ ...current, ...patch }))
  }

  async function handleImageUpload(file: File | undefined) {
    if (!file) return
    setUploadingImage(true)
    try {
      const result = await uploadBlogImage(file)
      patch({
        featuredImage: result.url,
        featuredImageWidth: result.width ?? form.featuredImageWidth,
        featuredImageHeight: result.height ?? form.featuredImageHeight,
      })
    } finally {
      setUploadingImage(false)
    }
  }

  async function submit(nextStatus?: BlogStatus) {
    clearError()
    setSaving(true)
    const payload: BlogPostPayload = {
      ...form,
      status: nextStatus ?? form.status,
      slug: form.slug?.trim() || slugify(form.title),
      tags: splitTags(tagsInput),
      featuredImage: form.featuredImage?.trim() || null,
      authorNameOverride: form.authorNameOverride?.trim() || null,
      authorBio: form.authorBio?.trim() || null,
      reviewedBy: form.reviewedBy?.trim() || null,
      informationGain: form.informationGain?.trim() || null,
      metaTitle: form.metaTitle?.trim() || null,
      metaDescription: form.metaDescription?.trim() || null,
      canonicalUrl: form.canonicalUrl?.trim() || null,
    }

    try {
      if (id) await updatePost(id, payload)
      else await createPost(payload)
      navigate('/admin/blog')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="space-y-5">
      <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
        <div className="space-y-1">
          <Button type="button" variant="ghost" size="sm" onClick={() => navigate('/admin/blog')}>
            <ArrowLeft className="size-4" /> Quay lại
          </Button>
          <h1 className="text-xl font-semibold">{isEdit ? 'Sửa bài đăng' : 'Tạo bài đăng'}</h1>
          <p className="text-sm text-muted-foreground">Nội dung block JSONB, SEO metadata, E-E-A-T.</p>
        </div>
        <div className="flex gap-2">
          <Button type="button" variant="outline" disabled={saving || loading} onClick={() => submit('Draft')}>Lưu nháp</Button>
          <Button type="button" disabled={saving || loading} onClick={() => submit('Published')}>
            <Save className="size-4" /> Xuất bản
          </Button>
        </div>
      </div>

      <div className="flex border-b border-slate-200 gap-4">
        <button
          type="button"
          className={`pb-2 text-sm font-medium border-b-2 transition-colors ${
            activeTab === 'edit'
              ? 'border-primary text-primary'
              : 'border-transparent text-slate-500 hover:text-slate-700 hover:border-slate-300'
          }`}
          onClick={() => setActiveTab('edit')}
        >
          Chỉnh sửa bài đăng
        </button>
        <button
          type="button"
          className={`pb-2 text-sm font-medium border-b-2 transition-colors ${
            activeTab === 'preview'
              ? 'border-primary text-primary'
              : 'border-transparent text-slate-500 hover:text-slate-700 hover:border-slate-300'
          }`}
          onClick={() => {
            // Sync tags array into the form before previewing
            patch({ tags: splitTags(tagsInput) })
            setActiveTab('preview')
          }}
        >
          Xem trước bài viết
        </button>
      </div>

      {error && <div className="rounded-lg border border-destructive/30 bg-destructive/5 px-4 py-3 text-sm text-destructive">{error}</div>}

      {activeTab === 'edit' ? (
        <div className="grid gap-5 xl:grid-cols-[minmax(0,1fr)_360px]">
        <div className="space-y-5">
          <section className="rounded-xl border bg-white p-4 space-y-4">
            <h2 className="font-semibold">Thông tin chính</h2>
            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2 md:col-span-2">
                <Label htmlFor="title">Tiêu đề</Label>
                <Input id="title" value={form.title} onChange={(e) => patch({ title: e.target.value, slug: form.slug || slugify(e.target.value) })} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="slug">Slug</Label>
                <Input id="slug" value={form.slug ?? ''} onChange={(e) => patch({ slug: slugify(e.target.value) })} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="template">Template</Label>
                <Select id="template" value={form.template} onChange={(e) => patch({ template: e.target.value as BlogTemplate })}>
                  {blogTemplates.map((template) => <option key={template} value={template}>{templateLabels[template]}</option>)}
                </Select>
              </div>
              <div className="space-y-2 md:col-span-2">
                <Label htmlFor="excerpt">Tóm tắt</Label>
                <Textarea id="excerpt" rows={3} value={form.excerpt} onChange={(e) => patch({ excerpt: e.target.value })} />
              </div>
              <div className="space-y-2 md:col-span-2">
                <Label htmlFor="featuredImage">Ảnh nổi bật</Label>
                <div className="grid gap-2 md:grid-cols-[1fr_auto]">
                  <Input id="featuredImage" value={form.featuredImage ?? ''} onChange={(e) => patch({ featuredImage: e.target.value })} placeholder="/uploads/blog/cach-chon-ao-dai.webp" />
                  <label className="inline-flex cursor-pointer items-center justify-center rounded-md border px-3 text-sm font-medium hover:bg-muted">
                    {uploadingImage ? 'Đang tải...' : 'Tải ảnh'}
                    <input type="file" accept="image/*" className="sr-only" disabled={uploadingImage} onChange={(e) => void handleImageUpload(e.target.files?.[0])} />
                  </label>
                </div>
              </div>
              <div className="space-y-2">
                <Label htmlFor="featuredImageWidth">Rộng ảnh</Label>
                <Input id="featuredImageWidth" type="number" value={form.featuredImageWidth ?? 1200} onChange={(e) => patch({ featuredImageWidth: Number(e.target.value) || null })} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="featuredImageHeight">Cao ảnh</Label>
                <Input id="featuredImageHeight" type="number" value={form.featuredImageHeight ?? 630} onChange={(e) => patch({ featuredImageHeight: Number(e.target.value) || null })} />
              </div>
              <div className="space-y-2 md:col-span-2">
                <Label htmlFor="tags">Tags</Label>
                <Input id="tags" value={tagsInput} onChange={(e) => setTagsInput(e.target.value)} placeholder="áo dài cưới, bảo quản áo dài" />
              </div>
            </div>
          </section>

          <section className="rounded-xl border bg-white p-4 space-y-4">
            <h2 className="font-semibold">Nội dung</h2>
            <BlockEditor blocks={form.content} onChange={(content) => patch({ content })} />
          </section>
        </div>

        <aside className="space-y-5">
          <section className="rounded-xl border bg-white p-4 space-y-4">
            <h2 className="font-semibold">Xuất bản</h2>
            <div className="space-y-2">
              <Label htmlFor="status">Trạng thái</Label>
              <Select id="status" value={form.status} onChange={(e) => patch({ status: e.target.value as BlogStatus })}>
                <option value="Draft">Bản nháp</option>
                <option value="Published">Đã xuất bản</option>
                <option value="Archived">Lưu trữ</option>
              </Select>
            </div>
          </section>

          <section className="rounded-xl border bg-white p-4 space-y-4">
            <h2 className="font-semibold">SEO</h2>
            <div className="space-y-2">
              <Label htmlFor="metaTitle">Meta title ({seoTitle.length}/60)</Label>
              <Input id="metaTitle" value={form.metaTitle ?? ''} onChange={(e) => patch({ metaTitle: e.target.value })} />
            </div>
            <div className="space-y-2">
              <Label htmlFor="metaDescription">Meta description ({seoDescription.length}/150)</Label>
              <Textarea id="metaDescription" rows={3} value={form.metaDescription ?? ''} onChange={(e) => patch({ metaDescription: e.target.value })} />
            </div>
            <div className="space-y-2">
              <Label htmlFor="canonicalUrl">Canonical URL</Label>
              <Input id="canonicalUrl" value={form.canonicalUrl ?? ''} onChange={(e) => patch({ canonicalUrl: e.target.value })} />
            </div>
            <div className="rounded-lg border bg-cream p-3 text-sm">
              <div className="font-medium text-blue-700 line-clamp-1">{seoTitle || 'Tiêu đề SEO'}</div>
              <div className="text-green-700 text-xs">https://aodainhauyen.io.vn/blog/{form.slug || slugify(form.title)}/</div>
              <p className="mt-1 text-muted-foreground line-clamp-2">{seoDescription || 'Mô tả SEO hiển thị trên Google.'}</p>
            </div>
          </section>

          <section className="rounded-xl border bg-white p-4 space-y-4">
            <h2 className="font-semibold">E-E-A-T</h2>
            <div className="space-y-2">
              <Label htmlFor="authorNameOverride">Tên tác giả hiển thị</Label>
              <Input id="authorNameOverride" value={form.authorNameOverride ?? ''} onChange={(e) => patch({ authorNameOverride: e.target.value })} />
            </div>
            <div className="space-y-2">
              <Label htmlFor="authorBio">Tiểu sử tác giả</Label>
              <Textarea id="authorBio" rows={3} value={form.authorBio ?? ''} onChange={(e) => patch({ authorBio: e.target.value })} />
            </div>
            <div className="space-y-2">
              <Label htmlFor="reviewedBy">Người kiểm duyệt</Label>
              <Input id="reviewedBy" value={form.reviewedBy ?? ''} onChange={(e) => patch({ reviewedBy: e.target.value })} />
            </div>
            <div className="space-y-2">
              <Label htmlFor="informationGain">Giá trị độc đáo</Label>
              <Textarea id="informationGain" rows={3} value={form.informationGain ?? ''} onChange={(e) => patch({ informationGain: e.target.value })} />
            </div>
          </section>
        </aside>
      </div>
      ) : (
        <BlogPreview post={form} />
      )}
    </div>
  )
}
