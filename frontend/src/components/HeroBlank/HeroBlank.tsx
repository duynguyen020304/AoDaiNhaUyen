import { useEffect, useState } from 'react';
import { AnimatePresence, motion, useReducedMotion } from 'framer-motion';
import styles from './HeroBlank.module.css';
import { easeOutQuart } from '../../utils/motion';

type HeroSlide = {
  image: string;
  video?: string;
};

const slides: HeroSlide[] = [
  {
    image: '/assets/BANNER ÁO DÀI 1.png', //thumbnail
    video: '/assets/áo dài 1.mp4',
  },
  {
    image: '/assets/BANNER ÁO DÀI 2.png', //thumbnail
    video: '/assets/áo dài 2_1.mp4',
  },
  {
    image: '/assets/BANNER ÁO DÀI 3.png', //thumbnail
    video: '/assets/áo dài 3_1.mp4',
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
        {slide.video ? (
          <motion.video
            key={slide.video}
            className={styles.heroMedia}
            poster={slide.image}
            src={slide.video}
            autoPlay
            loop
            muted
            playsInline
            preload="metadata"
            aria-hidden="true"
            variants={slideMotion}
            initial="enter"
            animate="center"
            exit="exit"
            transition={{ duration: 0.78, ease: easeOutQuart }}
          />
        ) : (
          <motion.img
            key={slide.image}
            className={styles.heroMedia}
            src={slide.image}
            alt=""
            aria-hidden="true"
            variants={slideMotion}
            initial="enter"
            animate="center"
            exit="exit"
            transition={{ duration: 0.78, ease: easeOutQuart }}
          />
        )}
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
              key={item.image || item.video}
              type="button"
              className={index === active ? styles.isActive : ''}
              onClick={() => goToSlide(index)}
              aria-label={`Chuyển đến`}
              aria-pressed={index === active}
              whileHover={prefersReducedMotion ? undefined : { scaleY: 1.25 }}
              whileTap={{ scale: 0.9 }}
            >
              {index === active && !prefersReducedMotion ? (
                <motion.span
                  key={`${isPaused ? 'paused' : 'playing'}`}
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
