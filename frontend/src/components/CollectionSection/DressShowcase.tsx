import { motion } from 'framer-motion';
import styles from './CollectionSection.module.css';
import { cardHover, cardReveal, imageParallaxHover, staggerContainer } from '../../utils/motion';

const dresses = [
  { src: '/assets/dress-white.png', alt: 'Áo dài màu trắng kem', name: 'Trắng kem', featured: false },
  { src: '/assets/dress-pink.png', alt: 'Áo dài màu hồng phấn', name: 'Hồng Phấn', featured: true },
  { src: '/assets/dress-green.png', alt: 'Áo dài màu xanh lục bảo', name: 'Xanh Lục Bảo', featured: false },
];

export default function DressShowcase() {
  return (
    <motion.div className={styles.dressShowcase} variants={staggerContainer}>
      {dresses.map((dress) => (
        <motion.article
          key={dress.name}
          className={`${styles.dressArticle} ${dress.featured ? styles.featured : ''}`}
          variants={cardReveal}
          whileHover="hover"
          transition={{ duration: 0.32, ease: 'easeOut' }}
        >
          <motion.img
            src={dress.src}
            alt={dress.alt}
            variants={imageParallaxHover}
            transition={{ duration: 0.36, ease: 'easeOut' }}
          />
          <motion.div className={styles.dressGlow} variants={cardHover} aria-hidden="true" />
          <h3>{dress.name}</h3>
        </motion.article>
      ))}
    </motion.div>
  );
}
