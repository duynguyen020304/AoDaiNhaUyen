import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import { PictureImg } from '../PictureImg/PictureImg';
import type { BlogPostListItem } from '../../types/blog';
import styles from './BlogCard.module.css';

function formatDate(value: string | null) {
  if (!value) return '';
  const dateObj = new Date(value);
  return dateObj.toLocaleDateString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric'
  });
}

export function BlogCard({ post }: { post: BlogPostListItem }) {
  return (
    <motion.article 
      className={styles.card} 
      whileHover={{ y: -6, boxShadow: '0 20px 40px rgba(114,19,17,0.08)' }} 
      transition={{ duration: 0.3, ease: [0.16, 1, 0.3, 1] }}
    >
      <Link to={`/blog/${post.slug}/`} className={styles.link} aria-label={`Đọc bài viết ${post.title}`}>
        <div className={styles.imageWrapper}>
          <PictureImg 
            src={post.featuredImage} 
            alt={post.title} 
            width={post.featuredImageWidth ?? 800} 
            height={post.featuredImageHeight ?? 520} 
            className={styles.image} 
          />
          <div className={styles.imageOverlay} />
        </div>
        <div className={styles.body}>
          <div className={styles.meta}>
            <span className={styles.date}>{formatDate(post.publishedAt)}</span>
            {post.authorName && (
              <>
                <span className={styles.metaDivider}>·</span>
                <span className={styles.author}>{post.authorName}</span>
              </>
            )}
          </div>
          <h2 className={styles.cardTitle}>{post.title}</h2>
          <p className={styles.excerpt}>{post.excerpt}</p>
          <div className={styles.footer}>
            <div className={styles.tags}>
              {post.tags.slice(0, 2).map((tag) => (
                <span key={tag} className={styles.tag}>{tag}</span>
              ))}
            </div>
            <span className={styles.readMore}>
              Đọc tiếp 
              <svg className={styles.arrow} width="16" height="16" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path d="M6 12L10 8L6 4" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
              </svg>
            </span>
          </div>
        </div>
      </Link>
    </motion.article>
  );
}
