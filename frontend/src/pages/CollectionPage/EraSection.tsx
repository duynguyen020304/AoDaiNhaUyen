import { motion } from 'framer-motion';
import type { CSSProperties } from 'react';
import styles from './EraSection.module.css';
import { fadeUp, sectionReveal, viewportOnce } from '../../utils/motion';
import { GOLD_GRADIENT, IMG, type EraData } from './data';

interface Props {
  data: EraData;
}

export default function EraSection({ data }: Props) {
  const isRight = data.layout === 'right';

  return (
    <motion.section
      className={`${styles.era} ${styles[data.variant]}`}
      style={{ '--era-height': data.frameHeight } as CSSProperties}
      variants={sectionReveal}
      initial="hidden"
      whileInView="show"
      viewport={viewportOnce}
    >
      <div className={styles.bgScene}>
        <img src={data.images.bg ?? data.images.street} alt="" className={styles.bgImg} />
      </div>

      <img src={IMG.figmaVectorBase} alt="" className={`${styles.vectorDecor} ${styles.vectorBase}`} aria-hidden="true" />
      <img src={IMG.figmaVectorRight} alt="" className={`${styles.vectorDecor} ${styles.vectorRight}`} aria-hidden="true" />
      <img src={IMG.figmaVectorSmall} alt="" className={`${styles.vectorDecor} ${styles.vectorSmall}`} aria-hidden="true" />

      <img src={IMG.figmaCloudPattern} alt="" className={`${styles.cloudDecor} ${styles.cloudTop}`} aria-hidden="true" />
      <img src={IMG.figmaCloudPattern} alt="" className={`${styles.cloudDecor} ${styles.cloudBottom}`} aria-hidden="true" />

      <div className={`${styles.content} ${isRight ? styles.contentRight : styles.contentLeft}`}>
        <motion.div className={styles.textCol} variants={fadeUp}>
          <h3
            className={styles.eraTitle}
            style={{ backgroundImage: GOLD_GRADIENT }}
          >
            {data.title}
          </h3>
          <div className={styles.copy}>
            <p className={styles.eraBadge}>{data.era}</p>
            <p className={styles.eraSubtitle}>{data.subtitle}</p>
            <p className={styles.eraDesc}>{data.description}</p>
          </div>
        </motion.div>

        <motion.div className={styles.imageCol} variants={fadeUp}>
          <div className={styles.productWrap}>
            <img src={data.images.product} alt={data.title} className={styles.productImg} />
          </div>
        </motion.div>
      </div>
    </motion.section>
  );
}
