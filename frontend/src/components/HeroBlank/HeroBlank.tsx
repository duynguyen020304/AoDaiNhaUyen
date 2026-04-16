import { useEffect, useState } from 'react';
import { AnimatePresence, motion, useReducedMotion } from 'framer-motion';
import styles from './HeroBlank.module.css';
import { carouselCopy, carouselCopyItem, easeOutQuart } from '../../utils/motion';

const slides = [
  {
    eyebrow: 'Áo Dài Nhã Uyên',
    title: 'Tà áo Việt cho ngày đáng nhớ',
    desc: 'Lụa mềm, dáng thanh và sắc đỏ son cho những khoảnh khắc rạng rỡ.',
    image: '/assets/dress-panel.png',
  },
  {
    eyebrow: 'Bộ Sưu Tập',
    title: 'Hoa văn truyền thống, phom dáng hiện đại',
    desc: 'Những thiết kế tôn dáng với nhịp chuyển nhẹ như một dải lụa.',
    image: '/assets/red-floral.png',
  },
  {
    eyebrow: 'Thử Đồ AI',
    title: 'Xem trước tà áo hợp với bạn',
    desc: 'Chọn mẫu yêu thích và bắt đầu trải nghiệm chỉ trong vài giây.',
    image: '/assets/ai-scene.png',
  },
];

const slideMotion = {
  enter: { opacity: 0, scale: 1.04 },
  center: { opacity: 1, scale: 1 },
  exit: { opacity: 0, scale: 0.98 },
};

export default function HeroBlank() {
  const [active, setActive] = useState(0);
  const [isPaused, setIsPaused] = useState(false);
  const prefersReducedMotion = useReducedMotion();
  const slide = slides[active];

  useEffect(() => {
    if (prefersReducedMotion || isPaused) {
      return;
    }

    const timer = window.setInterval(() => {
      setActive((current) => (current + 1) % slides.length);
    }, 4300);

    return () => window.clearInterval(timer);
  }, [isPaused, prefersReducedMotion]);

  const goToSlide = (index: number) => setActive(index);
  const goPrevious = () => setActive((current) => (current - 1 + slides.length) % slides.length);
  const goNext = () => setActive((current) => (current + 1) % slides.length);

  return (
    <section
      className={styles.heroBlank}
      aria-label="Áo dài Nhã Uyên"
      onMouseEnter={() => setIsPaused(true)}
      onMouseLeave={() => setIsPaused(false)}
      onFocus={() => setIsPaused(true)}
      onBlur={(event) => {
        if (!event.currentTarget.contains(event.relatedTarget)) {
          setIsPaused(false);
        }
      }}
    >
      <AnimatePresence mode="wait" initial={false}>
        <motion.img
          key={slide.image}
          className={styles.heroImage}
          src={slide.image}
          alt=""
          aria-hidden="true"
          variants={slideMotion}
          initial="enter"
          animate="center"
          exit="exit"
          transition={{ duration: 0.78, ease: easeOutQuart }}
        />
      </AnimatePresence>

      <div className={styles.heroShade} aria-hidden="true" />

      <AnimatePresence mode="wait" initial={false}>
        <motion.div
          key={slide.title}
          className={styles.heroCopy}
          variants={carouselCopy}
          initial="enter"
          animate="center"
          exit="exit"
        >
          <motion.p variants={carouselCopyItem}>{slide.eyebrow}</motion.p>
          <motion.h1 variants={carouselCopyItem}>{slide.title}</motion.h1>
          <motion.span variants={carouselCopyItem}>{slide.desc}</motion.span>
          <motion.a
            className={styles.heroCta}
            href="#collection"
            variants={carouselCopyItem}
            whileHover={prefersReducedMotion ? undefined : { y: -2, scale: 1.02 }}
            whileTap={{ scale: 0.97 }}
          >
            Khám phá bộ sưu tập
          </motion.a>
        </motion.div>
      </AnimatePresence>

      <div className={styles.sliderControls} aria-label="Điều khiển banner">
        <motion.button
          type="button"
          onClick={goPrevious}
          aria-label="Banner trước"
          whileHover={prefersReducedMotion ? undefined : { x: -2, backgroundColor: 'rgba(255, 255, 255, 0.18)' }}
          whileTap={{ scale: 0.94 }}
        >
          ‹
        </motion.button>
        <div className={styles.slideDots}>
          {slides.map((item, index) => (
            <motion.button
              key={item.title}
              type="button"
              className={index === active ? styles.isActive : ''}
              onClick={() => goToSlide(index)}
              aria-label={`Chuyển đến ${item.title}`}
              aria-pressed={index === active}
              whileHover={prefersReducedMotion ? undefined : { scaleY: 1.25 }}
              whileTap={{ scale: 0.9 }}
            >
              {index === active && !prefersReducedMotion ? (
                <motion.span
                  key={`${item.title}-${isPaused ? 'paused' : 'playing'}`}
                  className={styles.dotProgress}
                  initial={{ scaleX: 0 }}
                  animate={{ scaleX: isPaused ? 0.18 : 1 }}
                  transition={{ duration: isPaused ? 0.18 : 4.3, ease: 'linear' }}
                />
              ) : null}
            </motion.button>
          ))}
        </div>
        <motion.button
          type="button"
          onClick={goNext}
          aria-label="Banner tiếp theo"
          whileHover={prefersReducedMotion ? undefined : { x: 2, backgroundColor: 'rgba(255, 255, 255, 0.18)' }}
          whileTap={{ scale: 0.94 }}
        >
          ›
        </motion.button>
      </div>
    </section>
  );
}
