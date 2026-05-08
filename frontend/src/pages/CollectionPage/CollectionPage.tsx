import { useEffect, useState } from 'react';
import { faArrowUp } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { AnimatePresence, motion } from 'framer-motion';
import styles from './CollectionPage.module.css';
import { fadeUp, sectionReveal, viewportOnce } from '../../utils/motion';
import { GOLD_GRADIENT, STORY_INTRO, IMG, ERAS } from './data';
import CollectionHero from './CollectionHero';
import EraSection from './EraSection';
import BrandStorySection from './BrandStorySection';
import GallerySection from './GallerySection';

export default function CollectionPage() {
  const [showBackToTop, setShowBackToTop] = useState(false);

  useEffect(() => {
    const updateBackToTopVisibility = () => {
      setShowBackToTop(window.scrollY > 520);
    };

    updateBackToTopVisibility();
    window.addEventListener('scroll', updateBackToTopVisibility, { passive: true });

    return () => {
      window.removeEventListener('scroll', updateBackToTopVisibility);
    };
  }, []);

  const scrollToPageTop = () => {
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  return (
    <div className={styles.page}>
      {/* Hero banner */}
      <CollectionHero />

      {/* bst1 – Story intro */}
      <motion.section
        className={styles.storyIntro}
        variants={sectionReveal}
        initial="hidden"
        whileInView="show"
        viewport={viewportOnce}
      >
        {/* Background textures */}
        <div className={styles.textureTop}>
          <img src={IMG.figmaBst1Bg} alt="" />
        </div>
        <div className={styles.textureBottom}>
          <img src={IMG.figmaBst1Bg} alt="" />
        </div>
        <div className={styles.patternDecor}>
          <img src={IMG.figmaCloudPattern} alt="" />
        </div>

        <motion.h2
          className={styles.storyTitle}
          style={{ backgroundImage: GOLD_GRADIENT }}
          variants={fadeUp}
        >
          {STORY_INTRO.title}
        </motion.h2>
        <motion.p className={styles.storyDesc} variants={fadeUp}>
          {STORY_INTRO.description}
        </motion.p>
      </motion.section>

      {/* bst2–bst5 – Era sections */}
      {ERAS.map((era) => (
        <EraSection key={era.title} data={era} />
      ))}

      {/* bst6 – Brand story */}
      <BrandStorySection />

      {/* bst7–bst10 – Gallery */}
      <GallerySection />

      <AnimatePresence>
        {showBackToTop ? (
          <motion.button
            className={styles.backToTopButton}
            type="button"
            aria-label="Di chuyển lên đầu trang"
            title="Di chuyển lên đầu trang"
            onClick={scrollToPageTop}
            initial={{ opacity: 0, y: 18, scale: 0.96 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: 18, scale: 0.96 }}
            whileHover={{ y: -3 }}
            whileTap={{ scale: 0.94 }}
            transition={{ type: 'spring', stiffness: 320, damping: 26 }}
          >
            <FontAwesomeIcon icon={faArrowUp} />
          </motion.button>
        ) : null}
      </AnimatePresence>
    </div>
  );
}
