import type { CSSProperties } from 'react';
import { AnimatePresence, motion } from 'framer-motion';
import styles from './ProductSection.module.css';
import { easeOutQuart, fadeScale } from '../../utils/motion';
import type { ProductSectionNote, ProductSectionSlide } from './productSectionData';

type ProductSectionHeroProps = {
  active: number;
  activeNote: ProductSectionNote | null;
  notes: ProductSectionNote[];
  prefersReducedMotion: boolean | null;
  slide: ProductSectionSlide;
  slides: ProductSectionSlide[];
  onNext: () => void;
  onPrevious: () => void;
  onSelect: (index: number) => void;
  onNoteChange: (note: ProductSectionNote | null) => void;
};

export default function ProductSectionHero({
  activeNote,
  notes,
  onNoteChange,
  prefersReducedMotion,
  slide,
}: ProductSectionHeroProps) {
  return (
    <motion.div className={styles.heroColumn} variants={fadeScale}>
      <motion.img
        className={styles.heroDragon}
        src="/assets/dragon.png"
        alt=""
        aria-hidden="true"
        animate={
          prefersReducedMotion
            ? { opacity: 0.12 }
            : { opacity: [0.08, 0.16, 0.08], rotate: [17.52, 20, 17.52] }
        }
        transition={{ duration: 8, repeat: Infinity, ease: 'easeInOut' }}
      />

      <div className={styles.archFrame}>
        <img className={styles.archBackdrop} src="/assets/dress-panel.png" alt="" aria-hidden="true" />

        <AnimatePresence mode="wait">
          <motion.img
            key={slide.heroImage}
            className={styles.heroImage}
            src={slide.heroImage}
            alt={slide.heroAlt}
            initial={{ opacity: 0, y: 18, scale: 1.02 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: -18, scale: 0.98 }}
            transition={{ duration: 0.38, ease: easeOutQuart }}
          />
        </AnimatePresence>

        <div className={styles.hotspotLayer} aria-label="Chi tiết thiết kế">
          {notes.map((note) => (
            <button
              key={note.id}
              type="button"
              className={`${styles.hotspotButton} ${activeNote?.id === note.id ? styles.hotspotActive : ''}`}
              style={
                {
                  '--hotspot-x': note.hotspotX,
                  '--hotspot-y': note.hotspotY,
                } as CSSProperties
              }
              aria-label={note.label}
              aria-pressed={activeNote?.id === note.id}
              onBlur={() => onNoteChange(null)}
              onFocus={() => onNoteChange(note)}
              onMouseEnter={() => onNoteChange(note)}
              onMouseLeave={() => onNoteChange(null)}
            >
              <span aria-hidden="true" />
            </button>
          ))}
        </div>

        <AnimatePresence>
          {activeNote ? (
            <motion.div
              key={activeNote.id}
              className={styles.noteCallout}
              style={
                {
                  '--callout-x': activeNote.calloutX,
                  '--callout-y': activeNote.calloutY,
                  '--callout-object-position': activeNote.calloutObjectPosition,
                  '--callout-scale': activeNote.calloutScale,
                  '--connector-length': activeNote.connectorLength,
                  '--connector-angle': activeNote.connectorAngle,
                } as CSSProperties
              }
              initial={{ opacity: 0, scale: 0.84 }}
              animate={{ opacity: 1, scale: 1 }}
              exit={{ opacity: 0, scale: 0.9 }}
              transition={{ duration: 0.24, ease: easeOutQuart }}
              aria-hidden="true"
            >
              <span className={styles.noteConnector} />
              <span className={styles.noteLens}>
                <img src={slide.heroImage} alt="" />
              </span>
            </motion.div>
          ) : null}
        </AnimatePresence>
      </div>

      <div className={styles.titlePill}>
        <AnimatePresence mode="wait">
          <motion.span
            key={slide.id}
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -10 }}
            transition={{ duration: 0.28, ease: easeOutQuart }}
          >
            {slide.title}
          </motion.span>
        </AnimatePresence>
      </div>
    </motion.div>
  );
}
