import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import { PictureImg } from '../PictureImg/PictureImg';
import type { BlogPostListItem } from '../../types/blog';
import styles from './BlogCard.module.css';

function formatDate(value: string | null) { return value ? new Date(value).toLocaleDateString('vi-VN') : ''; }
export function BlogCard({ post }: { post: BlogPostListItem }) {
  return (
    <motion.article className={styles.card} whileHover={{ y: -4 }} transition={{ duration: 0.2 }}>
      <Link to={`/blog/${post.slug}/`} className={styles.link} aria-label={`Đọc bài viết ${post.title}`}>
        <PictureImg src={post.featuredImage} alt={post.title} width={post.featuredImageWidth ?? 800} height={post.featuredImageHeight ?? 520} className={styles.image} />
        <div className={styles.body}>
          <div className={styles.meta}>{formatDate(post.publishedAt)}{post.authorName ? ` · ${post.authorName}` : ''}</div>
          <h2>{post.title}</h2>
          <p>{post.excerpt}</p>
          <div className={styles.tags}>{post.tags.slice(0, 3).map((tag) => <span key={tag}>{tag}</span>)}</div>
        </div>
      </Link>
    </motion.article>
  );
}
