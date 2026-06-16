import { useRef, useState, useCallback } from 'react';
import { Link } from 'react-router-dom';
import { PictureImg } from '../PictureImg/PictureImg';
import { useProductSpotlight } from '../../hooks/blog/useProductSpotlight';
import type { BlogBlock, BlogTemplate } from '../../types/blog';
import styles from './BlogBlockRenderer.module.css';

export function BlogBlockRenderer({ blocks, template }: { blocks: BlogBlock[]; template: BlogTemplate }) {
  const [lightboxImg, setLightboxImg] = useState<{ src: string; alt: string; caption?: string } | null>(null);

  return (
    <div className={`${styles.content} ${styles[template.toLowerCase()]}`}>
      {blocks.map((block, index) => (
        <Block
          key={`${block.type}-${index}`}
          block={block}
          template={template}
          onOpenLightbox={(img) => setLightboxImg(img)}
        />
      ))}

      {lightboxImg && (
        <div className={styles.lightbox} onClick={() => setLightboxImg(null)} role="dialog" aria-modal="true">
          <div className={styles.lightboxBackdrop} />
          <div className={styles.lightboxContent} onClick={(e) => e.stopPropagation()}>
            <img src={lightboxImg.src} alt={lightboxImg.alt} className={styles.lightboxImage} />
            {lightboxImg.caption && <p className={styles.lightboxCaption}>{lightboxImg.caption}</p>}
            <button className={styles.lightboxClose} onClick={() => setLightboxImg(null)} aria-label="Đóng ảnh">
              &times;
            </button>
          </div>
        </div>
      )}
    </div>
  );
}

function Block({
  block,
  template,
  onOpenLightbox,
}: {
  block: BlogBlock;
  template: BlogTemplate;
  onOpenLightbox: (img: { src: string; alt: string; caption?: string }) => void;
}) {
  switch (block.type) {
    case 'heading':
      return block.level === 3 ? <h3>{block.content}</h3> : <h2>{block.content}</h2>;
    case 'paragraph':
      // StandardArticle can have drop cap on the very first paragraph or styled text
      return <p className={styles.paragraph}>{block.content}</p>;
    case 'image':
      return (
        <figure
          className={`${block.width === 'full' ? styles.full : undefined} ${styles.imageFigure}`}
          onClick={() => onOpenLightbox({ src: block.src, alt: block.alt, caption: block.caption })}
        >
          <PictureImg
            src={block.src}
            alt={block.alt}
            width={block.widthPx ?? 1200}
            height={block.heightPx ?? 800}
            className={styles.image}
          />
          {block.caption ? <figcaption>{block.caption}</figcaption> : null}
        </figure>
      );
    case 'gallery':
      return (
        <GalleryCarousel
          images={block.images}
          onOpenLightbox={onOpenLightbox}
        />
      );
    case 'video':
      return (
        <figure className={styles.videoContainer}>
          <video
            className={styles.video}
            src={block.src}
            poster={block.poster}
            controls
            preload="metadata"
            crossOrigin="anonymous"
          />
          {block.caption ? <figcaption>{block.caption}</figcaption> : null}
        </figure>
      );
    case 'product_spotlight':
      return <ProductSpotlight slugs={block.productSlugs} template={template} />;
    case 'step':
      return (
        <div className={styles.stepCard}>
          <div className={styles.stepBadge}>
            <span>{block.stepNumber}</span>
          </div>
          <div className={styles.stepBody}>
            <h4>{block.title}</h4>
            <p>{block.content}</p>
            {block.tip && (
              <div className={styles.stepTip}>
                <span className={styles.tipBadge}>Mẹo hữu ích</span>
                <p>{block.tip}</p>
              </div>
            )}
          </div>
        </div>
      );
    case 'quote':
      return (
        <blockquote className={styles.blockquote}>
          <span className={styles.quoteIcon}>“</span>
          <p>{block.content}</p>
          {block.attribution ? <cite>— {block.attribution}</cite> : null}
        </blockquote>
      );
    case 'divider':
      return <hr className={styles.divider} />;
    case 'callout':
      return (
        <aside className={`${styles.callout} ${styles[block.variant]}`}>
          <div className={styles.calloutIcon}>
            {block.variant === 'tip' && '💡'}
            {block.variant === 'warning' && '⚠️'}
            {block.variant === 'info' && '✨'}
          </div>
          <div className={styles.calloutText}>{block.content}</div>
        </aside>
      );
    case 'code':
      return (
        <pre className={styles.codeBlock}>
          <code>{block.content}</code>
        </pre>
      );
    case 'embed':
      return (
        <figure className={styles.embedContainer}>
          <iframe
            className={styles.embed}
            src={block.url}
            title={block.caption || 'Nội dung nhúng'}
            loading="lazy"
            sandbox="allow-scripts allow-same-origin allow-presentation"
            allow="fullscreen"
            referrerPolicy="no-referrer"
          />
          {block.caption ? <figcaption>{block.caption}</figcaption> : null}
        </figure>
      );
  }
}

function ProductSpotlight({ slugs, template }: { slugs: string[]; template: BlogTemplate }) {
  const query = useProductSpotlight(slugs);
  if (query.isLoading) return <aside className={styles.productSpotlightLoading}>Đang kết nối sản phẩm từ hệ thống...</aside>;
  const products = query.data ?? [];
  if (products.length === 0) return null;

  return (
    <aside className={`${styles.productSpotlight} ${template === 'ProductSpotlight' ? styles.spotlightHero : ''}`}>
      <div className={styles.spotlightHeader}>
        <span className={styles.spotlightTag}>SẢN PHẨM TRONG BÀI VIẾT</span>
        <h3>Ý kiến chuyên gia đề xuất</h3>
      </div>
      <div className={styles.productGrid}>
        {products.map((product) => {
          const originalPrice = product.price;
          const currentPrice = product.salePrice ?? product.price;
          const discountPct = originalPrice > currentPrice
            ? Math.round(((originalPrice - currentPrice) / originalPrice) * 100)
            : 0;

          return (
            <Link key={product.id} to={`/product/${product.slug}`} className={styles.productCard}>
              <div className={styles.productImageWrapper}>
                <PictureImg
                  src={product.primaryImageUrl}
                  alt={product.name}
                  width={320}
                  height={420}
                  className={styles.productImage}
                />
                {discountPct > 0 && <span className={styles.discountBadge}>-{discountPct}%</span>}
                <div className={styles.productHoverOverlay}>
                  <span>Xem Chi Tiết</span>
                </div>
              </div>
              <div className={styles.productDetails}>
                <span className={styles.productCategory}>{product.productType === 'ao-dai-cach-tan' ? 'Áo Dài Cách Tân' : 'Áo Dài Truyền Thống'}</span>
                <strong className={styles.productName}>{product.name}</strong>
                <div className={styles.priceRow}>
                  <span className={styles.price}>{currentPrice.toLocaleString('vi-VN')}₫</span>
                  {discountPct > 0 && <span className={styles.originalPrice}>{originalPrice.toLocaleString('vi-VN')}₫</span>}
                </div>
              </div>
            </Link>
          );
        })}
      </div>
    </aside>
  );
}

function GalleryCarousel({
  images,
  onOpenLightbox,
}: {
  images: { src: string; alt: string; caption?: string; widthPx?: number; heightPx?: number }[];
  onOpenLightbox: (img: { src: string; alt: string; caption?: string }) => void;
}) {
  const scrollRef = useRef<HTMLDivElement>(null);
  const [activeIdx, setActiveIdx] = useState(0);
  const total = images.length;

  const handleScroll = useCallback(() => {
    const el = scrollRef.current;
    if (!el) return;
    const idx = Math.round(el.scrollLeft / el.offsetWidth);
    setActiveIdx(Math.min(idx, total - 1));
  }, [total]);

  const scrollTo = useCallback((idx: number) => {
    const el = scrollRef.current;
    if (!el) return;
    el.scrollTo({ left: el.offsetWidth * idx, behavior: 'smooth' });
  }, []);

  return (
    <div className={styles.carouselWrapper}>
      <div
        ref={scrollRef}
        className={styles.carousel}
        onScroll={handleScroll}
      >
        {images.map((img) => (
          <figure
            key={img.src}
            className={styles.carouselSlide}
            onClick={() => onOpenLightbox({ src: img.src, alt: img.alt, caption: img.caption })}
          >
            <PictureImg
              src={img.src}
              alt={img.alt}
              width={img.widthPx ?? 800}
              height={img.heightPx ?? 600}
              className={styles.carouselImage}
            />
            {img.caption ? <figcaption className={styles.carouselCaption}>{img.caption}</figcaption> : null}
          </figure>
        ))}
      </div>

      {/* Navigation arrows */}
      {total > 1 && (
        <>
          <button
            className={`${styles.carouselArrow} ${styles.carouselArrowPrev}`}
            onClick={() => scrollTo(Math.max(0, activeIdx - 1))}
            disabled={activeIdx === 0}
            aria-label="Ảnh trước"
          >‹</button>
          <button
            className={`${styles.carouselArrow} ${styles.carouselArrowNext}`}
            onClick={() => scrollTo(Math.min(total - 1, activeIdx + 1))}
            disabled={activeIdx === total - 1}
            aria-label="Ảnh sau"
          >›</button>
        </>
      )}

      {/* Dot indicators */}
      {total > 1 && (
        <div className={styles.carouselDots}>
          {images.map((_img, idx) => (
            <button
              key={idx}
              className={`${styles.carouselDot} ${idx === activeIdx ? styles.carouselDotActive : ''}`}
              onClick={() => scrollTo(idx)}
              aria-label={`Xem ảnh ${idx + 1}`}
            />
          ))}
        </div>
      )}
    </div>
  );
}
