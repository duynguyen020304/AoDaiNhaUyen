import styles from './CollectionSection.module.css';

const dresses = [
  { src: '/assets/dress-white.png', alt: 'Áo dài màu trắng kem', name: 'Trắng kem', featured: false },
  { src: '/assets/dress-pink.png', alt: 'Áo dài màu hồng phấn', name: 'Hồng Phấn', featured: true },
  { src: '/assets/dress-green.png', alt: 'Áo dài màu xanh lục bảo', name: 'Xanh Lục Bảo', featured: false },
];

export default function DressShowcase() {
  return (
    <div className={styles.dressShowcase}>
      {dresses.map((dress) => (
        <article key={dress.name} className={`${styles.dressArticle} ${dress.featured ? styles.featured : ''}`}>
          <img src={dress.src} alt={dress.alt} />
          <h3>{dress.name}</h3>
        </article>
      ))}
    </div>
  );
}
