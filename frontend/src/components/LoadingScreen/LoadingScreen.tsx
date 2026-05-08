import { useEffect, useState } from 'react';
import { motion, useReducedMotion } from 'framer-motion';
import styles from './LoadingScreen.module.css';
import { easeOutQuart } from '../../utils/motion';

type LoadingScreenProps = {
  onComplete: () => void;
};

const greetings = [
  'Xin Chào',
  'Hi',
  'Hello',
  'Bonjour',
  'Hej',
  'Merhaba',
  'Halo',
  'Xin chào',
];

const ARTBOARD_SRC = '/assets/figma-loading/loading-artboard.png';
const INITIAL_LOADING_DURATION = 1500;
const GREETING_INTERVAL = 250;

export default function LoadingScreen({ onComplete }: LoadingScreenProps) {
  const [step, setStep] = useState(0);
  const [isReady, setIsReady] = useState(false);
  const [hasImageError, setHasImageError] = useState(false);
  const prefersReducedMotion = useReducedMotion();
  const visibleStep = prefersReducedMotion ? greetings.length - 1 : step;
  const visibleReady = Boolean(prefersReducedMotion) || isReady;

  useEffect(() => {
    if (prefersReducedMotion) {
      return undefined;
    }

    const timers = [
      ...Array.from({ length: greetings.length - 1 }, (_, index) => (
        window.setTimeout(
          () => setStep(index + 1),
          INITIAL_LOADING_DURATION + (index * GREETING_INTERVAL),
        )
      )),
      window.setTimeout(
        () => setIsReady(true),
        INITIAL_LOADING_DURATION + ((greetings.length - 1) * GREETING_INTERVAL),
      ),
    ];

    return () => timers.forEach(window.clearTimeout);
  }, [prefersReducedMotion]);

  useEffect(() => {
    if (!visibleReady) {
      return undefined;
    }

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Enter' || event.key === ' ') {
        event.preventDefault();
        onComplete();
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [visibleReady, onComplete]);

  return (
    <motion.section
      className={styles.loadingScreen}
      aria-label="Đang tải trang chủ Áo dài Nhã Uyên"
      initial={{ opacity: 1 }}
      exit={{ opacity: 0 }}
      transition={{ duration: 0.62, ease: easeOutQuart }}
      onClick={() => {
        if (visibleReady) {
          onComplete();
        }
      }}
    >
      <motion.div
        className={styles.markWrap}
        initial={{ opacity: 0, scale: 0.92 }}
        animate={{ opacity: 1, scale: visibleReady ? 1 : [0.92, 1, 0.96] }}
        transition={{ duration: visibleReady ? 0.5 : 1.2, ease: easeOutQuart }}
      >
        {hasImageError ? (
          <span className={styles.markFallback} aria-hidden="true">Nhã Uyên</span>
        ) : (
          <img
            className={styles.mark}
            src={ARTBOARD_SRC}
            alt=""
            aria-hidden="true"
            onError={() => setHasImageError(true)}
          />
        )}
      </motion.div>

      {!visibleReady && visibleStep === 0 ? (
        <motion.div
            className={styles.progress}
            aria-hidden="true"
            initial={{ opacity: 0, y: 16 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.34, duration: 0.46, ease: easeOutQuart }}
          >
            <motion.span
              initial={{ scaleX: 0 }}
              animate={{ scaleX: 1 }}
              transition={{ duration: 1.32, ease: 'easeInOut' }}
            />
          </motion.div>
      ) : null}

      <div className={styles.copy} aria-live="polite">
        {!visibleReady && visibleStep === 0 ? (
          <p className={styles.loadingText}>
            Loading ...
          </p>
        ) : null}

        {visibleStep > 0 ? (
          <h1 className={styles.title}>
            Nhã Uyên <span>{greetings[visibleStep]}</span>
          </h1>
        ) : null}

        {visibleReady ? (
          <button
            className={styles.enterPrompt}
            type="button"
            onClick={onComplete}
          >
            Nhấn “Enter” để bắt đầu!
          </button>
        ) : null}
      </div>
    </motion.section>
  );
}
