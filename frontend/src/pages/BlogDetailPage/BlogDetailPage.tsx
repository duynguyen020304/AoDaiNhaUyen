import { Link, useParams } from 'react-router-dom';
import { BlogBlockRenderer } from '../../components/BlogBlockRenderer/BlogBlockRenderer';
import { PictureImg } from '../../components/PictureImg/PictureImg';
import { BlogSeo } from '../../components/Seo/BlogSeo';
import { JsonLd } from '../../components/Seo/JsonLd';
import { resolveAssetUrl } from '../../api/client';
import { BlogCard } from '../../components/BlogCard/BlogCard';
import { useBlogDetail, useRelatedPosts } from '../../hooks/blog/useBlogQueries';
import styles from './BlogDetailPage.module.css';

const SITE = 'https://aodainhauyen.io.vn';
function date(value: string | null | undefined) { return value ? new Date(value).toLocaleDateString('vi-VN') : ''; }
function desc(post: { metaDescription: string | null; excerpt: string }) { return (post.metaDescription || post.excerpt).slice(0, 150); }
export default function BlogDetailPage() {
  const { slug } = useParams();
  const query = useBlogDetail(slug);
  const related = useRelatedPosts(slug);
  const post = query.data;
  if (query.isLoading) return <main className={styles.page}><div className={styles.state}>Đang tải bài viết...</div></main>;
  if (!post) return <main className={styles.page}><div className={styles.state}>Không tìm thấy bài viết.</div></main>;
  const canonical = post.canonicalUrl || `${SITE}/blog/${post.slug}/`;
  const title = (post.metaTitle || `${post.title} | Áo Dài Nhà Uyên`).slice(0, 60);
  const description = desc(post);
  const image = resolveAssetUrl(post.featuredImage) ?? `${SITE}/assets/footer-logo.png`;
  const author = post.authorName || 'Áo Dài Nhà Uyên';

  return (
    <main className={styles.page}>
      <BlogSeo title={title} description={description} canonical={canonical} image={image} type="article" />
      <JsonLd data={{ '@context': 'https://schema.org', '@type': 'BlogPosting', headline: post.title, description, image, datePublished: post.publishedAt, dateModified: post.updatedAt, author: { '@type': 'Person', name: author }, publisher: { '@type': 'Organization', name: 'Áo Dài Nhà Uyên', logo: { '@type': 'ImageObject', url: `${SITE}/logo.png` } }, mainEntityOfPage: { '@type': 'WebPage', '@id': canonical } }} />
      <JsonLd data={{ '@context': 'https://schema.org', '@type': 'BreadcrumbList', itemListElement: [{ '@type': 'ListItem', position: 1, name: 'Trang Chủ', item: `${SITE}/` }, { '@type': 'ListItem', position: 2, name: 'Bài Viết', item: `${SITE}/blog/` }, { '@type': 'ListItem', position: 3, name: post.title }] }} />
      <article>
        <header className={styles.hero}>
          <nav aria-label="Breadcrumb"><Link to="/">Trang chủ</Link><span>/</span><Link to="/blog/">Bài viết</Link><span>/</span><span>{post.title}</span></nav>
          <div className={styles.tags}>{post.tags.map((tag) => <Link key={tag} to={`/blog/?tag=${encodeURIComponent(tag)}`}>{tag}</Link>)}</div>
          <h1>{post.title}</h1>
          <p>{post.excerpt}</p>
          <div className={styles.byline}>Viết bởi {author} · Xuất bản {date(post.publishedAt)} · Cập nhật {date(post.updatedAt)}</div>
          {post.reviewedBy ? <div className={styles.reviewed}>Đã kiểm duyệt bởi {post.reviewedBy}</div> : null}
        </header>
        <div className={styles.heroImage}>
          <PictureImg src={post.featuredImage} alt={post.title} width={post.featuredImageWidth ?? 1200} height={post.featuredImageHeight ?? 630} lazy={false} fetchPriority="high" className={styles.image} sizes="100vw" />
        </div>
        {post.authorBio ? <aside className={styles.authorBio}>{post.authorBio}</aside> : null}
        <BlogBlockRenderer blocks={post.content} />
        {(related.data?.length ?? 0) > 0 ? (
          <section className={styles.related} aria-labelledby="related-title">
            <h2 id="related-title">Bài viết liên quan</h2>
            <div className={styles.relatedGrid}>{related.data?.map((item) => <BlogCard key={item.id} post={item} />)}</div>
          </section>
        ) : null}
      </article>
    </main>
  );
}
