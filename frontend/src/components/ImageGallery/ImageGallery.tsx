import { useState, useCallback } from 'react';
import { AnimatePresence, motion } from 'framer-motion';
import { fadeScale } from '../../utils/motion';
import styles from './ImageGallery.module.css';

interface GalleryImage {
  imageUrl: string;
  altText: string | null;
}

interface ImageGalleryProps {
  images: GalleryImage[];
}

export default function ImageGallery({ images }: ImageGalleryProps) {
  const [activeIndex, setActiveIndex] = useState(0);
  const [zoomed, setZoomed] = useState(false);

  const handleThumbnailClick = useCallback((index: number) => {
    if (index === activeIndex) return;
    setActiveIndex(index);
    setZoomed(false);
  }, [activeIndex]);

  const isEmpty = images.length === 0;

  if (isEmpty) {
    return (
      <div className={styles.gallery}>
        <div className={styles.mainWrapper}>
          <div className={styles.emptyState}>
            <svg
              width="64"
              height="64"
              viewBox="0 0 24 24"
              fill="none"
              stroke="var(--muted)"
              strokeWidth="1.5"
              strokeLinecap="round"
              strokeLinejoin="round"
              aria-hidden="true"
            >
              <rect x="3" y="3" width="18" height="18" rx="2" ry="2" />
              <circle cx="8.5" cy="8.5" r="1.5" />
              <polyline points="21,15 16,10 5,21" />
            </svg>
            <span className={styles.emptyText}>Chưa có hình ảnh</span>
          </div>
        </div>
      </div>
    );
  }

  const activeImage = images[activeIndex];

  return (
    <div className={styles.gallery}>
      {/* Thumbnails — vertical strip on desktop, horizontal scroll on mobile */}
      <div className={styles.thumbnails}>
        {images.map((img, i) => (
          <button
            key={img.imageUrl}
            type="button"
            className={`${styles.thumbBtn} ${i === activeIndex ? styles.thumbActive : ''}`}
            onClick={() => handleThumbnailClick(i)}
            aria-label={
              img.altText
                ? `Xem ảnh: ${img.altText}`
                : `Xem ảnh ${i + 1}`
            }
            aria-current={i === activeIndex ? 'true' : undefined}
          >
            <img
              src={img.imageUrl}
              alt={img.altText ?? `Ảnh sản phẩm ${i + 1}`}
              className={styles.thumbImage}
              loading="lazy"
            />
          </button>
        ))}
      </div>

      {/* Main image */}
      <div className={styles.mainWrapper}>
        <button
          type="button"
          className={styles.mainBtn}
          onClick={() => setZoomed((z) => !z)}
          aria-label="Phóng to ảnh"
        >
          <AnimatePresence mode="wait">
            <motion.img
              key={activeImage.imageUrl}
              src={activeImage.imageUrl}
              alt={activeImage.altText ?? 'Ảnh sản phẩm'}
              className={`${styles.mainImage} ${zoomed ? styles.zoomed : ''}`}
              variants={fadeScale}
              initial="hidden"
              animate="show"
              exit="hidden"
              transition={{ duration: 0.35, ease: 'easeOut' }}
            />
          </AnimatePresence>
        </button>

        {/* Image counter indicator */}
        <span className={styles.counter}>
          {activeIndex + 1} / {images.length}
        </span>
      </div>
    </div>
  );
}
