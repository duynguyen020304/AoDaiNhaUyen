import { useSearchParams } from 'react-router-dom';
import { motion } from 'framer-motion';
import { BlogCard } from '../../components/BlogCard/BlogCard';
import { BlogSeo } from '../../components/Seo/BlogSeo';
import { JsonLd } from '../../components/Seo/JsonLd';
import { useBlogList } from '../../hooks/blog/useBlogQueries';
import styles from './BlogPage.module.css';

const SITE = 'https://aodainhauyen.io.vn';
export default function BlogPage() {
  const [sp, setSp] = useSearchParams();
  const tag = sp.get('tag') ?? undefined;
  const category = sp.get('category') ?? undefined;
  const page = Number(sp.get('page') ?? '1');
  const posts = useBlogList({ tag, category, page, pageSize: 9 });

  const title = 'Bài Viết Áo Dài, Cưới, Văn Hóa | Áo Dài Nhà Uyên';
  const description = 'Khám phá bí quyết chọn áo dài cưới, bảo quản áo dài và cảm hứng thời trang Việt từ Áo Dài Nhà Uyên.';
  const items = posts.data?.data ?? [];

  return (
    <main className={styles.page}>
      <BlogSeo title={title} description={description} canonical={`${SITE}/blog/`} image={`${SITE}/assets/footer-logo.png`} />
      <JsonLd data={{ '@context': 'https://schema.org', '@type': 'Organization', name: 'Áo Dài Nhà Uyên', url: SITE, logo: `${SITE}/logo.png` }} />
      <JsonLd data={{ '@context': 'https://schema.org', '@type': 'ItemList', itemListElement: items.slice(0, 10).map((post, index) => ({ '@type': 'ListItem', position: index + 1, url: `${SITE}/blog/${post.slug}/`, name: post.title, image: post.featuredImage ?? undefined })) }} />
      
      {/* Decorative background grid and gradient */}
      <div className={styles.gridOverlay} aria-hidden="true" />
      <div className={styles.glowBlob} aria-hidden="true" />

      <div className={styles.container}>
        {posts.isLoading && (
          <div className={styles.loadingContainer}>
            <div className={styles.spinner} />
            <p>Đang tải bài viết...</p>
          </div>
        )}

        {posts.isError && (
          <div className={styles.stateContainer}>
            <div className={styles.errorIcon}>⚠️</div>
            <p>Không thể tải bài viết. Vui lòng thử lại.</p>
          </div>
        )}

        {!posts.isLoading && items.length === 0 && (
          <div className={styles.stateContainer}>
            <div className={styles.emptyIcon}>✍️</div>
            <p>Chưa có bài viết phù hợp với chủ đề này.</p>
          </div>
        )}



        {!posts.isLoading && items.length > 0 && (
          <motion.section 
            className={styles.grid}
            initial="hidden"
            animate="show"
            variants={{
              hidden: { opacity: 0 },
              show: {
                opacity: 1,
                transition: {
                  staggerChildren: 0.08
                }
              }
            }}
          >
            {items.map((post) => (
              <motion.div
                key={post.id}
                variants={{
                  hidden: { opacity: 0, y: 20 },
                  show: { opacity: 1, y: 0, transition: { duration: 0.6, ease: [0.16, 1, 0.3, 1] } }
                }}
              >
                <BlogCard post={post} />
              </motion.div>
            ))}
          </motion.section>
        )}

        <footer className={styles.paginationSection}>
          <div className={styles.pagination}>
            <button 
              className={styles.pageBtn}
              disabled={page <= 1} 
              onClick={() => setSp({ ...(tag ? { tag } : {}), page: String(page - 1) })}
              aria-label="Trang trước"
            >
              ← Trước
            </button>
            <span className={styles.pageIndicator}>Trang <strong>{page}</strong></span>
            <button 
              className={styles.pageBtn}
              disabled={!posts.data?.hasNextPage} 
              onClick={() => setSp({ ...(tag ? { tag } : {}), page: String(page + 1) })}
              aria-label="Trang sau"
            >
              Sau →
            </button>
          </div>
        </footer>
      </div>
    </main>
  );
}
