import { useState } from 'react';
import { motion } from 'framer-motion';
import styles from './ProductsPage.module.css';
import { sectionReveal, staggerContainer, viewportOnce } from '../../utils/motion';
import { CATEGORIES, SIZES } from './data';
import CategoryBanner from '../../components/CategoryBanner/CategoryBanner';
import ProductCard from '../../components/ProductCard/ProductCard';

export default function ProductsPage() {
  return (
    <main className={styles.page}>
      {CATEGORIES.map((category) => (
        <div key={category.id}>
          <CategoryBanner title={category.name} />
          <motion.section
            className={styles.productSection}
            variants={sectionReveal}
            initial="hidden"
            whileInView="show"
            viewport={viewportOnce}
          >
            <div className={styles.filterBar}>
              <SizeDropdown />
            </div>
            <motion.div
              className={styles.productGrid}
              variants={staggerContainer}
            >
              {category.products.map((product) => (
                <ProductCard key={product.id} data={product} />
              ))}
            </motion.div>
          </motion.section>
        </div>
      ))}
    </main>
  );
}

function SizeDropdown() {
  const [open, setOpen] = useState(false);
  const [selected, setSelected] = useState<string | null>(null);

  return (
    <div className={styles.dropdown}>
      <button
        className={styles.dropdownToggle}
        onClick={() => setOpen(!open)}
        type="button"
      >
        {selected ?? 'Chọn size'}
        <svg width="10" height="6" viewBox="0 0 10 6" fill="none" aria-hidden="true" role="img">
          <path d="M1 1l4 4 4-4" stroke="currentColor" strokeWidth="1.2" strokeLinecap="round" strokeLinejoin="round"/>
        </svg>
      </button>
      {open && (
        <ul className={styles.dropdownMenu}>
          {SIZES.map((size) => (
            <li key={size}>
              <button
                type="button"
                className={`${styles.dropdownOption} ${selected === size ? styles.active : ''}`}
                onClick={() => { setSelected(size); setOpen(false); }}
              >
                {size}
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
