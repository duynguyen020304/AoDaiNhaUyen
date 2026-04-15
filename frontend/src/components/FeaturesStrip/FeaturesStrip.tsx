import styles from './FeaturesStrip.module.css';

const features = [
  { icon: '\u25B1', title: 'MIỄN PHÍ VẬN CHUYỂN', desc: 'Đơn hàng trên 1 triệu' },
  { icon: '\u25A1', title: 'THANH TOÁN AN TOÀN', desc: 'Bảo mật tuyệt đối' },
  { icon: '\u21BB', title: 'ĐỔI TRẢ 7 NGÀY', desc: 'Nếu có lỗi nhà sản xuất' },
];

export default function FeaturesStrip() {
  return (
    <section className={styles.featuresStrip} aria-label="Dịch vụ">
      {features.map((f) => (
        <article key={f.title} className={styles.article}>
          <span className={`${styles.icon} hover-lift`} aria-hidden="true">{f.icon}</span>
          <div>
            <h2 className={styles.title}>{f.title}</h2>
            <p className={styles.desc}>{f.desc}</p>
          </div>
        </article>
      ))}
    </section>
  );
}
