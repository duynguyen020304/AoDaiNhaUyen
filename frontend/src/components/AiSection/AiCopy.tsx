import styles from './AiSection.module.css';

const benefits = [
  {
    title: 'Thử Đồ Ảo Thông Minh',
    desc: 'Upload ảnh của bạn và xem ngay kết quả với bất kỳ mẫu áo dài nào',
  },
  {
    title: 'Kết Quả Tức Thì',
    desc: 'Nhận được hình ảnh chỉ trong vài giây với độ chính xác cao',
  },
  {
    title: '100% Miễn Phí',
    desc: 'Không giới hạn số lần thử, hoàn toàn miễn phí cho khách hàng',
  },
];

export default function AiCopy() {
  return (
    <div className={styles.aiCopy}>
      <p className={styles.eyebrow}>Áo Dài Nhã Uyên</p>
      <h1 id="ai-title">
        Trải Nghiệm Áo Dài
        <span>Với Công Nghệ AI</span>
      </h1>
      <p className={styles.aiIntro}>
        Khám phá vẻ đẹp của bạn trong tà áo dài MaryMy mà không cần thử trực tiếp. Công nghệ AI
        giúp bạn xem trước cách mỗi thiết kế tôn lên nét đẹp riêng chỉ trong vài giây.
      </p>

      <ul className={styles.aiBenefits} aria-label="Lợi ích thử đồ AI">
        {benefits.map((b) => (
          <li key={b.title}>
            <span className={styles.benefitDot} />
            <div>
              <strong>{b.title}</strong>
              <p>{b.desc}</p>
            </div>
          </li>
        ))}
      </ul>

      <a className={`${styles.primaryCta} hover-lift`} href="#collection">
        Thử đồ AI Ngay <span aria-hidden="true">&rarr;</span>
      </a>
      <p className={styles.ctaNote}>Dùng thử hoàn toàn miễn phí</p>
    </div>
  );
}
