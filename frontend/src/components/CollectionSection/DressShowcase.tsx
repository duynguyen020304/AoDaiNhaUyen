import { AnimatePresence, motion } from 'framer-motion';
import styles from './CollectionSection.module.css';
import { cardHover, cardReveal, imageParallaxHover, staggerContainer } from '../../utils/motion';

type Dress = {
  src: string;
  alt: string;
  name: string;
};

type DressShowcaseProps = {
  dresses: Dress[];
  tabLabel: string;
};

export default function DressShowcase({ dresses, tabLabel }: DressShowcaseProps) {
  return (
    <motion.div className={styles.dressShowcase} variants={staggerContainer} aria-live="polite">
      <AnimatePresence mode="wait">
        <motion.div
          key={tabLabel}
          className={styles.dressGrid}
          initial="hidden"
          animate="show"
          exit="exit"
          variants={{
            hidden: {},
            show: {
              transition: {
                staggerChildren: 0.08,
                delayChildren: 0.04,
              },
            },
            exit: {
              transition: {
                staggerChildren: 0.04,
                staggerDirection: -1,
              },
            },
          }}
        >
          {dresses.map((dress, index) => (
            <motion.article
              key={`${tabLabel}-${dress.name}`}
              className={`${styles.dressArticle} ${index === 1 ? styles.featured : ''}`}
              variants={{
                ...cardReveal,
                exit: {
                  opacity: 0,
                  y: 22,
                  scale: 0.98,
                  filter: 'blur(5px)',
                  transition: { duration: 0.2, ease: 'easeIn' },
                },
              }}
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
      </AnimatePresence>
    </motion.div>
  );
}
