import { Link, useSearchParams } from 'react-router-dom';
import { BlogCard } from '../../components/BlogCard/BlogCard';
import { BlogSeo } from '../../components/Seo/BlogSeo';
import { JsonLd } from '../../components/Seo/JsonLd';
import { useBlogList, useBlogTags } from '../../hooks/blog/useBlogQueries';
import styles from './BlogPage.module.css';

const SITE = 'https://aodainhauyen.io.vn';
export default function BlogPage() {
  const [sp, setSp] = useSearchParams();
  const tag = sp.get('tag') ?? undefined;
  const page = Number(sp.get('page') ?? '1');
  const posts = useBlogList({ tag, page, pageSize: 9 });
  const tags = useBlogTags();
  const title = 'Bài Viết Áo Dài, Cưới, Văn Hóa | Áo Dài Nhà Uyên';
  const description = 'Khám phá bí quyết chọn áo dài cưới, bảo quản áo dài và cảm hứng thời trang Việt từ Áo Dài Nhà Uyên.';
  const items = posts.data?.data ?? [];

  return (
    <main className={styles.page}>
      <BlogSeo title={title} description={description} canonical={`${SITE}/blog/`} image={`${SITE}/assets/footer-logo.png`} />
      <JsonLd data={{ '@context': 'https://schema.org', '@type': 'Organization', name: 'Áo Dài Nhà Uyên', url: SITE, logo: `${SITE}/logo.png` }} />
      <JsonLd data={{ '@context': 'https://schema.org', '@type': 'ItemList', itemListElement: items.slice(0, 10).map((post, index) => ({ '@type': 'ListItem', position: index + 1, url: `${SITE}/blog/${post.slug}/`, name: post.title, image: post.featuredImage ?? undefined })) }} />
      <section className={styles.hero}>
        <nav aria-label="Breadcrumb"><Link to="/">Trang chủ</Link><span>/</span><span>Bài viết</span></nav>
        <h1>Bài Viết</h1>
        <p>Hướng dẫn chọn áo dài, cảm hứng cưới hỏi, chăm sóc chất liệu và câu chuyện văn hóa Việt.</p>
      </section>
      <section className={styles.filters} aria-label="Lọc theo tag">
        <button className={!tag ? styles.active : ''} onClick={() => setSp({})}>Tất cả</button>
        {(tags.data ?? []).map((item) => <button key={item} className={tag === item ? styles.active : ''} onClick={() => setSp({ tag: item })}>{item}</button>)}
      </section>
      {posts.isLoading ? <div className={styles.state}>Đang tải bài viết...</div> : null}
      {posts.isError ? <div className={styles.state}>Không thể tải bài viết. Vui lòng thử lại.</div> : null}
      {!posts.isLoading && items.length === 0 ? <div className={styles.state}>Chưa có bài viết phù hợp.</div> : null}
      <section className={styles.grid}>{items.map((post) => <BlogCard key={post.id} post={post} />)}</section>
      <div className={styles.pagination}>
        <button disabled={page <= 1} onClick={() => setSp({ ...(tag ? { tag } : {}), page: String(page - 1) })}>Trước</button>
        <span>Trang {page}</span>
        <button disabled={!posts.data?.hasNextPage} onClick={() => setSp({ ...(tag ? { tag } : {}), page: String(page + 1) })}>Sau</button>
      </div>
    </main>
  );
}
