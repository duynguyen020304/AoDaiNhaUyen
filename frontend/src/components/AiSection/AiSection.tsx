import { useState } from 'react';
import { motion } from 'framer-motion';
import styles from './AiSection.module.css';
import { sectionReveal, viewportOnce } from '../../utils/motion';

type AiVariant = {
  id: string;
  role: string;
  name: string;
  sourceImage: string;
  dressImage: string;
  modelImage: string;
  modelClass: string;
  modelImageClass: string;
};

const variants: AiVariant[] = [
  {
    id: 'taylor',
    role: 'Ca sĩ',
    name: 'Taylor Swift',
    sourceImage: '/assets/ai-figma-taylor-source.png',
    dressImage: '/assets/ai-figma-taylor-dress.png',
    modelImage: '/assets/ai-figma-taylor-model.png',
    modelClass: styles.modelTaylor,
    modelImageClass: styles.modelImageTaylor,
  },
  {
    id: 'khanh-van',
    role: 'Hoa hậu/Người mẫu',
    name: 'Khánh Vân',
    sourceImage: '/assets/ai-figma-khanhvan-input.png',
    dressImage: '/assets/ai-figma-khanhvan-dress.png',
    modelImage: '/assets/ai-figma-khanhvan-model.png',
    modelClass: styles.modelKhanhVan,
    modelImageClass: styles.modelImageKhanhVan,
  },
  {
    id: 'misthy',
    role: 'Streamer',
    name: 'Misthy',
    sourceImage: '/assets/ai-figma-white-input.png',
    dressImage: '/assets/ai-figma-pink-dress.png',
    modelImage: '/assets/ai-figma-misthy-model.png',
    modelClass: styles.modelMisthy,
    modelImageClass: styles.modelImageMisthy,
  },
];

export default function AiSection() {
  const [activeIndex, setActiveIndex] = useState(0);
  const activeVariant = variants[activeIndex];

  const selectPrevious = () => {
    setActiveIndex((current) => (current === 0 ? variants.length - 1 : current - 1));
  };

  const selectNext = () => {
    setActiveIndex((current) => (current + 1) % variants.length);
  };

  return (
    <motion.section
      className={styles.aiSection}
      id="ai"
      aria-labelledby="ai-title"
      variants={sectionReveal}
      initial="hidden"
      whileInView="show"
      viewport={viewportOnce}
    >
      <div className={styles.scene}>
        <div className={styles.texture} aria-hidden="true" />
        <div className={styles.fanLayer} aria-hidden="true">
          <img className={`${styles.fan} ${styles.fanDefault}`} src="/assets/ai-figma-fan-default.svg" alt="" />
          <img className={`${styles.fan} ${styles.fanHover}`} src="/assets/ai-figma-fan-hover.svg" alt="" />
        </div>
        <div className={styles.preload} aria-hidden="true">
          {variants.flatMap((variant) => [variant.sourceImage, variant.dressImage, variant.modelImage]).map((src) => (
            <img key={src} src={src} alt="" />
          ))}
        </div>

        <div className={styles.cardCluster} aria-label="Ảnh nguồn và mẫu áo dài được AI phối thử">
          <img
            key={`${activeVariant.id}-source`}
            className={`${styles.photoCard} ${styles.sourceCard}`}
            src={activeVariant.sourceImage}
            alt={`Ảnh chân dung của ${activeVariant.name}`}
          />
          <span className={styles.plus} aria-hidden="true">+</span>
          <img
            key={`${activeVariant.id}-dress`}
            className={`${styles.photoCard} ${styles.dressCard}`}
            src={activeVariant.dressImage}
            alt={`Mẫu áo dài dành cho ${activeVariant.name}`}
          />
          <span className={`${styles.toolBadge} ${styles.toolBadgeTop}`} aria-hidden="true">×</span>
          <span className={`${styles.toolBadge} ${styles.toolBadgeBottom}`} aria-hidden="true">↔</span>
        </div>

        <div className={styles.controls} aria-label="Chọn mẫu AI">
          <button type="button" className={styles.navButton} onClick={selectPrevious} aria-label="Mẫu trước">
            <span aria-hidden="true" />
          </button>
          <button type="button" className={`${styles.navButton} ${styles.nextButton}`} onClick={selectNext} aria-label="Mẫu tiếp theo">
            <span aria-hidden="true" />
          </button>
        </div>

        <div className={styles.copy}>
          <h2 id="ai-title">Trải Nghiệm Áo Dài</h2>
          <p>
            Khám phá vẻ đẹp của bạn trong tà áo dài Nhã Uyên mà không cần thử trực tiếp. Công nghệ AI
            của chúng tôi giúp bạn xem trước cách mỗi thiết kế sẽ tôn lên nét đẹp riêng của bạn chỉ trong vài giây.
          </p>
          <a className={styles.primaryCta} href="/ai-tryon">
            Thử đồ AI Ngay <span aria-hidden="true">→</span>
          </a>
          <span className={styles.ctaNote}>Dùng thử hoàn toàn miễn phí</span>
        </div>

        <div className={styles.identity} aria-live="polite">
          <span>{activeVariant.role}</span>
          <strong>{activeVariant.name}</strong>
        </div>

        <img className={styles.sparkOne} src="/assets/ai-figma-spark-icon.png" alt="" aria-hidden="true" />
        <img className={styles.sparkTwo} src="/assets/ai-figma-spark-icon.png" alt="" aria-hidden="true" />
        <img className={styles.sparkThree} src="/assets/ai-figma-spark-icon.png" alt="" aria-hidden="true" />

        <div
          key={activeVariant.id}
          className={`${styles.model} ${activeVariant.modelClass}`}
          aria-hidden="true"
        >
          <img className={`${styles.modelImage} ${activeVariant.modelImageClass}`} src={activeVariant.modelImage} alt="" />
        </div>
      </div>
    </motion.section>
  );
}
