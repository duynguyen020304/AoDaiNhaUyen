import { motion } from 'framer-motion';
import styles from './CollectionPage.module.css';
import { fadeUp, sectionReveal, viewportOnce } from '../../utils/motion';
import { GOLD_GRADIENT, STORY_INTRO, IMG, ERAS } from './data';
import CollectionHero from './CollectionHero';
import EraSection from './EraSection';
import BrandStorySection from './BrandStorySection';
import GallerySection from './GallerySection';

export default function CollectionPage() {
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
    </div>
  );
}
