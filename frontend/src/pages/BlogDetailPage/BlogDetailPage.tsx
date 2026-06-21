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
  const title = (post.metaTitle || `${post.title} | Áo Dài Nhã Uyên`).slice(0, 60);
  const description = desc(post);
  const image = resolveAssetUrl(post.featuredImage) ?? `${SITE}/assets/footer-logo.png`;
  const author = post.authorName || 'Áo Dài Nhã Uyên';

  // Template-specific processing: extract first video block to render at the top in VideoFeature
  const videoBlock = post.template === 'VideoFeature'
    ? post.content.find(b => b.type === 'video' || b.type === 'embed')
    : null;
  const contentBlocks = videoBlock
    ? post.content.filter(b => b !== videoBlock)
    : post.content;

  // Template schema markup differentiation for richer SEO compliance
  const getStructuredData = () => {
    const base = {
      '@context': 'https://schema.org',
      headline: post.title,
      description,
      image,
      datePublished: post.publishedAt,
      dateModified: post.updatedAt,
      author: { '@type': 'Person', name: author },
      publisher: {
        '@type': 'Organization',
        name: 'Áo Dài Nhã Uyên',
        logo: { '@type': 'ImageObject', url: `${SITE}/logo.png` }
      },
      mainEntityOfPage: { '@type': 'WebPage', '@id': canonical }
    };

    if (post.template === 'HowTo') {
      const steps = post.content
        .filter(b => b.type === 'step')
        .map((b) => ({
          '@type': 'HowToStep',
          position: b.type === 'step' ? b.stepNumber : 1,
          name: b.type === 'step' ? b.title : '',
          text: b.type === 'step' ? b.content : ''
        }));

      return {
        ...base,
        '@type': 'HowTo',
        step: steps
      };
    }

    if (post.template === 'VideoFeature' && videoBlock) {
      return {
        ...base,
        '@type': 'VideoObject',
        name: post.title,
        description: post.excerpt,
        thumbnailUrl: [image],
        uploadDate: post.publishedAt,
        embedUrl: videoBlock.type === 'embed' ? videoBlock.url : undefined,
        contentUrl: videoBlock.type === 'video' ? videoBlock.src : undefined
      };
    }

    return {
      ...base,
      '@type': 'BlogPosting'
    };
  };

  return (
    <main className={`${styles.page} ${styles[post.template.toLowerCase() + 'Container']}`}>
      <BlogSeo title={title} description={description} canonical={canonical} image={image} type="article" />
      <JsonLd data={getStructuredData()} />
      <JsonLd data={{ '@context': 'https://schema.org', '@type': 'BreadcrumbList', itemListElement: [{ '@type': 'ListItem', position: 1, name: 'Trang Chủ', item: `${SITE}/` }, { '@type': 'ListItem', position: 2, name: 'Bài Viết', item: `${SITE}/blog/` }, { '@type': 'ListItem', position: 3, name: post.title }] }} />
      
      <article className={`${styles.article} ${styles[post.template.toLowerCase()]}`}>
        <header className={styles.hero}>
          <nav aria-label="Breadcrumb" className={styles.breadcrumb}>
            <Link to="/">Trang chủ</Link>
            <span>/</span>
            <Link to="/blog/">Bài viết</Link>
            <span>/</span>
            <span className={styles.breadcrumbCurrent}>{post.title}</span>
          </nav>
          
          <div className={styles.categoryLabel}>
            {post.tags && post.tags.length > 0 ? post.tags[0].toUpperCase() : 'BÀI VIẾT'}
          </div>
          
          <h1 className={styles.title}>{post.title}</h1>
          <p className={styles.excerpt}>{post.excerpt}</p>
          
          <div className={styles.metaRow}>
            <div className={styles.byline}>
              Viết bởi <strong className={styles.author}>{author}</strong>
              <span className={styles.bullet}>·</span>
              Xuất bản {date(post.publishedAt)}
            </div>
            {post.reviewedBy && (
              <div className={styles.reviewed}>
                <span>✓</span> Đã kiểm duyệt bởi {post.reviewedBy}
              </div>
            )}
          </div>
        </header>

        {/* Dynamic Video Showcase for VideoFeature template */}
        {post.template === 'VideoFeature' && videoBlock ? (
          <section className={styles.theaterMode} aria-label="Khu vực phát video">
            <div className={styles.theaterInner}>
              {videoBlock.type === 'video' ? (
                <video
                  className={styles.theaterVideo}
                  src={videoBlock.src}
                  poster={videoBlock.poster}
                  controls
                  preload="metadata"
                  crossOrigin="anonymous"
                />
              ) : (
                <iframe
                  className={styles.theaterEmbed}
                  src={videoBlock.url}
                  title={videoBlock.caption || 'Nội dung nhúng'}
                  loading="lazy"
                  sandbox="allow-scripts allow-same-origin allow-presentation"
                  allow="fullscreen"
                  referrerPolicy="no-referrer"
                />
              )}
              {videoBlock.caption && <p className={styles.theaterCaption}>{videoBlock.caption}</p>}
            </div>
          </section>
        ) : (
          /* Default Featured Image for standard templates */
          <div className={styles.heroImage}>
            <PictureImg
              src={post.featuredImage}
              alt={post.title}
              width={post.featuredImageWidth ?? 1200}
              height={post.featuredImageHeight ?? 630}
              lazy={false}
              fetchPriority="high"
              className={styles.image}
              sizes="100vw"
            />
          </div>
        )}

        <div className={styles.bodyContent}>
          <BlogBlockRenderer blocks={contentBlocks} template={post.template} />
        </div>

        {post.tags && post.tags.length > 0 && (
          <div className={styles.bottomTags}>
            {post.tags.map((tag) => (
              <Link key={tag} to={`/blog/?tag=${encodeURIComponent(tag)}`} className={styles.tagLink}>
                #{tag}
              </Link>
            ))}
          </div>
        )}

        {post.authorBio && (
          <aside className={styles.authorBio} aria-label="Thông tin tác giả">
            <div className={styles.authorAvatar}>
              {author.slice(0, 1).toUpperCase()}
            </div>
            <div className={styles.authorBioText}>
              <strong>Giới thiệu tác giả</strong>
              <p>{post.authorBio}</p>
            </div>
          </aside>
        )}

        {(related.data?.length ?? 0) > 0 ? (
          <section className={styles.related} aria-labelledby="related-title">
            <div className={styles.relatedHeader}>
              <h2 id="related-title">Bài viết liên quan</h2>
              <span className={styles.relatedSub}>Có thể bạn quan tâm</span>
            </div>
            <div className={styles.relatedGrid}>
              {related.data?.map((item) => (
                <BlogCard key={item.id} post={item} />
              ))}
            </div>
          </section>
        ) : null}
      </article>
    </main>
  );
}
