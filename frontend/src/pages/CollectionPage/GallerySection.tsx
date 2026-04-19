import { motion } from 'framer-motion';
import type { CSSProperties } from 'react';
import styles from './GallerySection.module.css';
import { cardReveal, fadeUp, listStagger, sectionReveal, viewportOnce } from '../../utils/motion';
import { COLLECTIONS, IMG } from './data';

export default function GallerySection() {
  return (
    <motion.section
      className={styles.gallery}
      variants={sectionReveal}
      initial="hidden"
      whileInView="show"
      viewport={viewportOnce}
    >
      {/* Top blur */}
      <div className={styles.topBlur}>
        <img src={IMG.galleryTexture} alt="" />
      </div>

      {/* Collection cards */}
      <motion.div
        className={styles.collections}
        variants={listStagger}
      >
        {COLLECTIONS.map((col, index) => (
          <motion.div
            key={col.number}
            className={`${styles.collectionCard} ${styles[`collectionCard${index + 1}`]}`}
            style={{ '--frame-height': col.frameHeight } as CSSProperties}
            variants={cardReveal}
          >
            {/* Product images */}
            <div className={styles.images}>
              {col.images.map((src, j) => (
                <motion.div
                  key={j}
                  className={`${styles.imageFrame} ${j === 0 ? styles.imageMain : styles.imageSide}`}
                  variants={fadeUp}
                >
                  <img src={src} alt={col.titleLines.join(' ')} />
                </motion.div>
              ))}
            </div>

            {/* Label overlay */}
            <div className={styles.labelOverlay}>
              <p className={styles.collectionNumber}>{col.number}</p>
              {col.textLayout === 'title-first' ? (
                <>
                  <h3 className={styles.collectionTitle}>
                    {col.titleLines.map((line, j) => (
                      <span key={j}>{line}</span>
                    ))}
                  </h3>
                  <p className={styles.collectionLabel}>{col.label}</p>
                </>
              ) : (
                <>
                  <p className={styles.collectionLabel}>{col.label}</p>
                  <h3 className={styles.collectionTitle}>
                    {col.titleLines.map((line, j) => (
                      <span key={j}>{line}</span>
                    ))}
                  </h3>
                </>
              )}
              <p className={styles.collectionQuote}>{col.quote}</p>
            </div>
          </motion.div>
        ))}
      </motion.div>
    </motion.section>
  );
}
