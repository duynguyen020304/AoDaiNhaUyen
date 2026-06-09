import { useEffect, useState } from 'react'
import type { BlogBlock, BlogPostPayload } from '@/types/blog'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5043'

interface BlogPreviewProps {
  post: BlogPostPayload
}

export function BlogPreview({ post }: BlogPreviewProps) {
  const author = post.authorNameOverride || 'Áo Dài Nhà Uyên'
  const dateStr = post.publishedAt
    ? new Date(post.publishedAt).toLocaleDateString('vi-VN')
    : new Date().toLocaleDateString('vi-VN')

  return (
    <div className="mx-auto max-w-4xl rounded-xl border bg-white p-6 shadow-sm md:p-8">
      {/* Meta tags preview badge */}
      <div className="mb-6 rounded-lg bg-slate-50 p-3 text-xs border border-slate-100 text-slate-500">
        <span className="font-semibold text-slate-700">SEO Preview:</span>{' '}
        {post.metaTitle || post.title || 'No title'} | {post.metaDescription || post.excerpt || 'No description'}
      </div>

      <article className="space-y-6">
        <header className="space-y-4 border-b border-slate-100 pb-6">
          {/* Breadcrumb mock */}
          <nav className="text-xs text-slate-400 flex items-center gap-1.5 font-medium">
            <span>Trang chủ</span>
            <span>/</span>
            <span>Bài viết</span>
            <span>/</span>
            <span className="text-slate-600 truncate max-w-[200px]">{post.title || 'Tiêu đề bài viết'}</span>
          </nav>

          {/* Tags */}
          {post.tags && post.tags.length > 0 && (
            <div className="flex flex-wrap gap-1.5">
              {post.tags.map((tag) => (
                <span
                  key={tag}
                  className="rounded-full bg-primary/5 px-2.5 py-0.5 text-xs font-semibold text-primary"
                >
                  #{tag}
                </span>
              ))}
            </div>
          )}

          {/* Title */}
          <h1 className="text-3xl font-bold tracking-tight text-slate-900 md:text-4xl">
            {post.title || 'Chưa nhập tiêu đề'}
          </h1>

          {/* Excerpt */}
          {post.excerpt && (
            <p className="text-lg text-slate-500 leading-relaxed italic border-l-2 border-slate-200 pl-4">
              {post.excerpt}
            </p>
          )}

          {/* Byline */}
          <div className="flex flex-wrap items-center gap-x-3 gap-y-1 text-xs text-slate-400">
            <span>Viết bởi <strong className="text-slate-600 font-semibold">{author}</strong></span>
            <span>•</span>
            <span>Ngày {dateStr}</span>
            {post.reviewedBy && (
              <>
                <span>•</span>
                <span className="bg-green-50 text-green-700 px-2 py-0.5 rounded border border-green-100">
                  Đã kiểm duyệt bởi {post.reviewedBy}
                </span>
              </>
            )}
            {post.template && (
              <>
                <span>•</span>
                <span className="bg-blue-50 text-blue-700 px-2 py-0.5 rounded border border-blue-100">
                  Giao diện: {post.template}
                </span>
              </>
            )}
          </div>
        </header>

        {/* Featured Image */}
        {post.featuredImage && (
          <div className="overflow-hidden rounded-xl bg-slate-50 border border-slate-100">
            <img
              src={post.featuredImage}
              alt={post.title || 'Featured'}
              className="mx-auto object-cover"
              style={{
                maxWidth: '100%',
                maxHeight: '450px',
                width: post.featuredImageWidth ? `${post.featuredImageWidth}px` : '100%',
                height: 'auto',
              }}
            />
          </div>
        )}

        {/* Author Bio if present */}
        {post.authorBio && (
          <div className="rounded-lg bg-amber-50/50 border border-amber-100/60 p-4 text-sm text-slate-600">
            <h4 className="font-semibold text-amber-900 mb-1">Giới thiệu tác giả</h4>
            <p className="italic">{post.authorBio}</p>
          </div>
        )}

        {/* Blocks Content */}
        <div className="prose prose-slate max-w-none mt-6 space-y-6">
          {post.content && post.content.length > 0 ? (
            post.content.map((block, idx) => (
              <PreviewBlock key={`${block.type}-${idx}`} block={block} />
            ))
          ) : (
            <p className="text-slate-400 text-center py-8 border-2 border-dashed rounded-lg">
              Bài viết chưa có khối nội dung nào.
            </p>
          )}
        </div>
      </article>
    </div>
  )
}

function PreviewBlock({ block }: { block: BlogBlock }) {
  switch (block.type) {
    case 'heading':
      if (block.level === 3) {
        return <h3 className="text-lg font-bold text-slate-800 mt-4 mb-2">{block.content}</h3>
      }
      return <h2 className="text-xl font-bold text-slate-900 mt-6 mb-3">{block.content}</h2>

    case 'paragraph':
      return <p className="text-slate-600 leading-relaxed text-base">{block.content}</p>

    case 'image':
      return (
        <figure className={`my-4 flex flex-col items-center ${block.width === 'full' ? 'w-full' : 'max-w-xl mx-auto'}`}>
          <img
            src={block.src || '/placeholder-image.webp'}
            alt={block.alt}
            className="rounded-lg border border-slate-100 shadow-xs object-cover"
            style={{
              width: block.widthPx ? `${block.widthPx}px` : '100%',
              height: block.heightPx ? `${block.heightPx}px` : 'auto',
              maxHeight: '400px',
            }}
          />
          {block.caption && (
            <figcaption className="mt-2 text-xs text-slate-400 text-center italic">{block.caption}</figcaption>
          )}
        </figure>
      )

    case 'gallery':
      return (
        <div className="my-6 space-y-4">
          <div className="grid grid-cols-2 gap-4">
            {block.images && block.images.map((img, i) => (
              <figure key={i} className="flex flex-col items-center">
                <img
                  src={img.src || '/placeholder-image.webp'}
                  alt={img.alt}
                  className="rounded-lg border border-slate-100 object-cover w-full"
                  style={{
                    height: img.heightPx ? `${img.heightPx}px` : '180px',
                  }}
                />
                {img.caption && (
                  <figcaption className="mt-1 text-xs text-slate-400 text-center italic">{img.caption}</figcaption>
                )}
              </figure>
            ))}
          </div>
        </div>
      )

    case 'video':
      return (
        <figure className="my-4 max-w-xl mx-auto flex flex-col items-center">
          <div className="aspect-video w-full rounded-lg bg-slate-900 flex items-center justify-center relative overflow-hidden border border-slate-800">
            {block.src ? (
              <video src={block.src} poster={block.poster} controls className="w-full h-full object-contain" />
            ) : (
              <span className="text-xs text-slate-500">Mô phỏng trình phát video ({block.src || 'Chưa cấu hình URL'})</span>
            )}
          </div>
          {block.caption && (
            <figcaption className="mt-2 text-xs text-slate-400 text-center italic">{block.caption}</figcaption>
          )}
        </figure>
      )

    case 'product_spotlight':
      return <ProductSpotlightPreview slugs={block.productSlugs} />

    case 'step':
      return (
        <section className="my-4 rounded-xl border border-slate-100 bg-slate-50/50 p-4 space-y-2">
          <div className="flex items-center gap-2">
            <span className="flex h-6 w-6 items-center justify-center rounded-full bg-primary text-xs font-bold text-white">
              {block.stepNumber}
            </span>
            <strong className="text-slate-800 font-bold">{block.title || 'Chưa nhập tiêu đề bước'}</strong>
          </div>
          <p className="text-sm text-slate-600 leading-relaxed pl-8">{block.content}</p>
          {block.tip && (
            <div className="pl-8 text-xs text-amber-600 italic">
              <strong>Mẹo:</strong> {block.tip}
            </div>
          )}
        </section>
      )

    case 'quote':
      return (
        <blockquote className="my-4 border-l-4 border-primary pl-4 py-1 bg-slate-50/60 rounded-r-lg">
          <p className="text-slate-700 italic text-base">"{block.content || 'Nội dung trích dẫn...'}"</p>
          {block.attribution && (
            <cite className="block text-xs font-semibold text-slate-400 mt-1 not-italic">— {block.attribution}</cite>
          )}
        </blockquote>
      )

    case 'divider':
      return <hr className="my-6 border-slate-200" />

    case 'callout': {
      const themes = {
        info: 'bg-blue-50 border-blue-200 text-blue-800',
        warning: 'bg-amber-50 border-amber-200 text-amber-800',
        tip: 'bg-green-50 border-green-200 text-green-800',
      }
      const label = {
        info: 'Thông tin',
        warning: 'Lưu ý',
        tip: 'Mẹo bổ ích',
      }
      return (
        <aside className={`my-4 rounded-lg border p-4 text-sm ${themes[block.variant || 'tip']}`}>
          <strong className="block font-semibold mb-1">{label[block.variant || 'tip']}</strong>
          <p>{block.content}</p>
        </aside>
      )
    }

    case 'code':
      return (
        <div className="my-4 rounded-lg overflow-hidden border border-slate-200 bg-slate-900 text-slate-100 p-4 font-mono text-xs relative">
          <div className="absolute top-2 right-2 text-[10px] text-slate-500 uppercase tracking-wider font-sans font-medium">
            {block.language || 'text'}
          </div>
          <pre className="overflow-x-auto">
            <code>{block.content || '// Không có mã nguồn'}</code>
          </pre>
        </div>
      )

    case 'embed':
      return (
        <figure className="my-4 max-w-xl mx-auto flex flex-col items-center">
          <div className="aspect-video w-full rounded-lg bg-slate-100 flex items-center justify-center border border-slate-200 overflow-hidden">
            {block.url ? (
              <iframe
                src={block.url}
                title={block.caption || 'Nội dung nhúng'}
                className="w-full h-full border-none"
                loading="lazy"
              />
            ) : (
              <span className="text-xs text-slate-400">Mô phỏng iframe nhúng ({block.url || 'Chưa cấu hình URL'})</span>
            )}
          </div>
          {block.caption && (
            <figcaption className="mt-2 text-xs text-slate-400 text-center italic">{block.caption}</figcaption>
          )}
        </figure>
      )

    default:
      return null
  }
}

interface ProductListItem {
  id: string
  name: string
  slug: string
  price: number
  salePrice: number | null
  primaryImageUrl: string
}

function ProductSpotlightPreview({ slugs }: { slugs: string[] }) {
  const [products, setProducts] = useState<ProductListItem[]>([])
  const [loading, setLoading] = useState(false)

  const slugsKey = slugs.filter(Boolean).join(',')

  useEffect(() => {
    if (!slugsKey) {
      return
    }

    let active = true

    const loadData = async () => {
      await Promise.resolve() // Defer state update to avoid linter warnings
      if (!active) return
      setLoading(true)
      try {
        const url = `${API_BASE_URL}/api/v1/products/batch?slugs=${slugsKey}`
        const res = await fetch(url)
        const json = await res.json()
        if (active && json.success && Array.isArray(json.data)) {
          setProducts(json.data)
        }
      } catch (err) {
        console.error('Failed to fetch spotlight products:', err)
      } finally {
        if (active) {
          setLoading(false)
        }
      }
    }

    void loadData()

    return () => {
      active = false
    }
  }, [slugsKey])

  const displayedProducts = !slugsKey ? [] : products

  if (!slugsKey) {
    return (
      <aside className="my-4 rounded-lg border border-dashed border-slate-200 p-4 text-center text-xs text-slate-400">
        Chưa có sản phẩm nào được chọn (Nhập danh sách slug sản phẩm).
      </aside>
    )
  }

  if (loading) {
    return (
      <aside className="my-4 rounded-lg border border-slate-100 bg-slate-50/50 p-4 text-center text-xs text-slate-500">
        Đang tải thông tin sản phẩm liên quan...
      </aside>
    )
  }

  return (
    <aside className="my-6 rounded-xl border border-slate-100 bg-slate-50/40 p-4 space-y-3">
      <h4 className="font-bold text-slate-900 text-sm">Sản phẩm liên quan</h4>
      <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 md:grid-cols-4">
        {displayedProducts.map((product) => (
          <div key={product.id} className="rounded-lg border border-slate-100 bg-white p-2 flex flex-col justify-between shadow-2xs">
            <div className="space-y-1">
              <img
                src={product.primaryImageUrl}
                alt={product.name}
                className="aspect-3/4 w-full rounded-md object-cover bg-slate-50 border border-slate-100/50"
              />
              <strong className="block text-xs font-semibold text-slate-800 line-clamp-2 min-h-[2rem]">
                {product.name}
              </strong>
            </div>
            <div className="mt-1 text-xs text-primary font-bold">
              {(product.salePrice ?? product.price).toLocaleString('vi-VN')}₫
            </div>
          </div>
        ))}
        {displayedProducts.length === 0 && (
          <p className="text-xs text-slate-400 col-span-full py-2">
            Không tìm thấy sản phẩm trùng khớp với các slug: {slugsKey.split(',').join(', ')}
          </p>
        )}
      </div>
    </aside>
  )
}
