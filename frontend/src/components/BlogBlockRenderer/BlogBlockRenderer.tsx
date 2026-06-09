import { Link } from 'react-router-dom';
import { PictureImg } from '../PictureImg/PictureImg';
import { useProductSpotlight } from '../../hooks/blog/useProductSpotlight';
import type { BlogBlock } from '../../types/blog';
import styles from './BlogBlockRenderer.module.css';

export function BlogBlockRenderer({ blocks }: { blocks: BlogBlock[] }) {
  return <div className={styles.content}>{blocks.map((block, index) => <Block key={`${block.type}-${index}`} block={block} />)}</div>;
}
function Block({ block }: { block: BlogBlock }) {
  switch (block.type) {
    case 'heading': return block.level === 3 ? <h3>{block.content}</h3> : <h2>{block.content}</h2>;
    case 'paragraph': return <p>{block.content}</p>;
    case 'image': return <figure className={block.width === 'full' ? styles.full : undefined}><PictureImg src={block.src} alt={block.alt} width={block.widthPx ?? 1200} height={block.heightPx ?? 800} className={styles.image} />{block.caption ? <figcaption>{block.caption}</figcaption> : null}</figure>;
    case 'gallery': return <div className={styles.gallery}>{block.images.map((img) => <figure key={img.src}><PictureImg src={img.src} alt={img.alt} width={img.widthPx ?? 800} height={img.heightPx ?? 600} className={styles.image} />{img.caption ? <figcaption>{img.caption}</figcaption> : null}</figure>)}</div>;
    case 'video': return <figure><video className={styles.video} src={block.src} poster={block.poster} controls preload="metadata" crossOrigin="anonymous" />{block.caption ? <figcaption>{block.caption}</figcaption> : null}</figure>;
    case 'product_spotlight': return <ProductSpotlight slugs={block.productSlugs} />;
    case 'step': return <section className={styles.step}><strong>Bước {block.stepNumber}: {block.title}</strong><p>{block.content}</p>{block.tip ? <em>{block.tip}</em> : null}</section>;
    case 'quote': return <blockquote>{block.content}{block.attribution ? <cite>{block.attribution}</cite> : null}</blockquote>;
    case 'divider': return <hr />;
    case 'callout': return <aside className={`${styles.callout} ${styles[block.variant]}`}>{block.content}</aside>;
    case 'code': return <pre><code>{block.content}</code></pre>;
    case 'embed': return <figure><iframe className={styles.embed} src={block.url} title={block.caption || 'Nội dung nhúng'} loading="lazy" sandbox="allow-scripts allow-same-origin" referrerPolicy="no-referrer" />{block.caption ? <figcaption>{block.caption}</figcaption> : null}</figure>;
  }
}

function ProductSpotlight({ slugs }: { slugs: string[] }) {
  const query = useProductSpotlight(slugs);
  if (query.isLoading) return <aside className={styles.callout}>Đang tải sản phẩm liên quan...</aside>;
  const products = query.data ?? [];
  if (products.length === 0) return null;

  return (
    <aside className={styles.productSpotlight}>
      <h3>Sản phẩm liên quan</h3>
      <div className={styles.productGrid}>
        {products.map((product) => (
          <Link key={product.id} to={`/product/${product.slug}`} className={styles.productCard}>
            <PictureImg src={product.primaryImageUrl} alt={product.name} width={320} height={420} className={styles.productImage} />
            <strong>{product.name}</strong>
            <span>{(product.salePrice ?? product.price).toLocaleString('vi-VN')}₫</span>
          </Link>
        ))}
      </div>
    </aside>
  );
}
