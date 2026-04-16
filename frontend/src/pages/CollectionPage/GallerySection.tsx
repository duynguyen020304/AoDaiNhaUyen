import { motion } from 'framer-motion';
import styles from './GallerySection.module.css';
import { cardReveal, fadeUp, listStagger, sectionReveal, viewportOnce } from '../../utils/motion';
import { COLLECTIONS, GOLD_GRADIENT, IMG } from './data';

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

      {/* Rotated background pattern */}
      <div className={styles.bgPattern}>
        <img src={IMG.galleryPattern} alt="" />
      </div>

      {/* Collection cards */}
      <motion.div
        className={styles.collections}
        variants={listStagger}
      >
        {COLLECTIONS.map((col) => (
          <motion.div
            key={col.number}
            className={styles.collectionCard}
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
              <p className={styles.collectionLabel}>{col.label}</p>
              <h3
                className={styles.collectionTitle}
                style={{ backgroundImage: GOLD_GRADIENT }}
              >
                {col.titleLines.map((line, j) => (
                  <span key={j}>{line}</span>
                ))}
              </h3>
              <p className={styles.collectionQuote}>{col.quote}</p>
            </div>
          </motion.div>
        ))}
      </motion.div>
    </motion.section>
  );
}
