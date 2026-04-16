import { motion } from 'framer-motion';
import styles from './EraSection.module.css';
import { fadeUp, sectionReveal, viewportOnce } from '../../utils/motion';
import { GOLD_GRADIENT, type EraData } from './data';

interface Props {
  data: EraData;
}

export default function EraSection({ data }: Props) {
  const isRight = data.layout === 'right';

  return (
    <motion.section
      className={styles.era}
      variants={sectionReveal}
      initial="hidden"
      whileInView="show"
      viewport={viewportOnce}
    >
      {/* Background texture */}
      <div className={styles.textureBg}>
        <img src={data.images.bg ?? data.images.street} alt="" className={styles.textureImg} />
      </div>

      {/* Decorative vector ornament */}
      <img src="/assets/collection/vector-decor.svg" alt="" className={styles.vectorDecor} aria-hidden="true" />

      {/* Content layout */}
      <div className={`${styles.content} ${isRight ? styles.contentRight : styles.contentLeft}`}>
        {/* Text side */}
        <motion.div className={styles.textCol} variants={fadeUp}>
          <p className={styles.eraBadge}>{data.era}</p>
          <h3
            className={styles.eraTitle}
            style={{ backgroundImage: GOLD_GRADIENT }}
          >
            {data.title}
          </h3>
          <p className={styles.eraSubtitle}>{data.subtitle}</p>
          <p className={styles.eraDesc}>{data.description}</p>
        </motion.div>

        {/* Image side */}
        <motion.div className={styles.imageCol} variants={fadeUp}>
          <div className={styles.productWrap}>
            <img src={data.images.product} alt={data.title} className={styles.productImg} />
          </div>
          {data.images.cachTan && (
            <div className={styles.cachTanWrap}>
              <img src={data.images.cachTan} alt="" className={styles.cachTanImg} />
            </div>
          )}
        </motion.div>
      </div>

      {/* Bottom blur */}
      <div className={styles.blurBottom} />
    </motion.section>
  );
}
