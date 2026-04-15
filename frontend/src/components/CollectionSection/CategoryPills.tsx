import { useState } from 'react';
import styles from './CollectionSection.module.css';

const categories = [
  'Áo dài thêu hoa',
  'Áo dài cách tân',
  'Áo dài lụa trơn',
  'Áo dài truyền thống',
];

export default function CategoryPills() {
  const [selected, setSelected] = useState(0);

  return (
    <div className={styles.categoryPills} aria-label="Danh mục áo dài">
      {categories.map((cat, i) => (
        <button
          key={cat}
          className={`${i === selected ? styles.selected : ''} hover-lift`}
          type="button"
          onClick={() => setSelected(i)}
        >
          {cat}
        </button>
      ))}
    </div>
  );
}
