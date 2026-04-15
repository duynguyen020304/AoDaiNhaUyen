import styles from './AiSection.module.css';
import AiCopy from './AiCopy';
import AiVisual from './AiVisual';

export default function AiSection() {
  return (
    <section className={styles.aiSection} id="ai" aria-labelledby="ai-title">
      <AiCopy />
      <AiVisual />
      <img className={styles.aiWave} src="/assets/ai-wave.svg" alt="" aria-hidden="true" />
    </section>
  );
}
