import styles from './AiSection.module.css';

export default function AiVisual() {
  return (
    <div className={styles.aiVisual} aria-hidden="true">
      <div className={styles.aiPhotoStage}>
        <img className={styles.aiBackdrop} src="/assets/ai-visual-backdrop.svg" alt="" />
        <img className={styles.aiModel} src="/assets/ai-scene.png" alt="" />
        <img className={`${styles.aiCard} ${styles.cardYellow}`} src="/assets/ai-card-yellow.jpg" alt="" />
        <img className={`${styles.aiCard} ${styles.cardBlue}`} src="/assets/ai-card-blue.jpg" alt="" />
      </div>
    </div>
  );
}
