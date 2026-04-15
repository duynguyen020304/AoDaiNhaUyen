import styles from './MaterialSection.module.css';

export default function MaterialSection() {
  return (
    <section className={`red-texture ${styles.materialSection}`} aria-labelledby="material-title">
      <div className={`${styles.scriptTitleWrap} script-title`} id="material-title">
        Chất liệu
      </div>
      <div className={styles.materialCopy}>
        <h3>Vải lụa</h3>
        <p>Có độ mềm mại, rũ, tạo nên vẻ đẹp thướt tha, duyên dáng cho người mặc.</p>
        <div className={styles.swatches} aria-label="Mẫu chất liệu">
          <span className={styles.swatch} />
          <span className={styles.swatch} />
          <span className={styles.swatch} />
        </div>
      </div>
      <img className={styles.drumPattern} src="/assets/drum-pattern.png" alt="" aria-hidden="true" />
      <img className={styles.dragonMark} src="/assets/dragon.png" alt="" aria-hidden="true" />
    </section>
  );
}
