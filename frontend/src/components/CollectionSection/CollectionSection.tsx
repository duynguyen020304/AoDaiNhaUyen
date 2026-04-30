import { useState } from 'react';
import { motion } from 'framer-motion';
import styles from './CollectionSection.module.css';
import CategoryPills from './CategoryPills';
import DressShowcase from './DressShowcase';
import { fadeScale, fadeUp, sectionReveal, viewportOnce } from '../../utils/motion';

const collectionTabs = [
  {
    label: 'Áo dài thêu hoa',
    dresses: [
      { src: '/assets/collection/embroidered-cream.png', alt: 'Áo dài thêu hoa màu trắng kem', name: 'Trắng kem' },
      { src: '/assets/collection/embroidered-pink.png', alt: 'Áo dài thêu hoa màu hồng phấn', name: 'Hồng Phấn' },
      { src: '/assets/collection/embroidered-green.png', alt: 'Áo dài thêu hoa màu xanh lục bảo', name: 'Xanh Lục Bảo' },
    ],
  },
  {
    label: 'Áo dài cách tân',
    dresses: [
      { src: '/assets/collection/modern-ruby.png', alt: 'Áo dài cách tân màu đỏ ruby', name: 'Đỏ Ruby' },
      { src: '/assets/collection/modern-white.png', alt: 'Áo dài cách tân màu trắng tuyết', name: 'Trắng tuyết' },
      { src: '/assets/collection/modern-peach.png', alt: 'Áo dài cách tân màu hồng đào', name: 'Hồng đào' },
    ],
  },
  {
    label: 'Áo dài lụa trơn',
    dresses: [
      { src: '/assets/collection/silk-pale-pink.png', alt: 'Áo dài lụa trơn màu hồng cánh sen nhạt', name: 'Hồng cánh sen nhạt' },
      { src: '/assets/collection/silk-red.png', alt: 'Áo dài lụa trơn màu đỏ tươi', name: 'Đỏ tươi' },
      { src: '/assets/collection/silk-blue.png', alt: 'Áo dài lụa trơn màu xanh baby', name: 'Xanh baby' },
    ],
  },
  {
    label: 'Áo dài truyền thống',
    dresses: [
      { src: '/assets/collection/traditional-velvet-red.png', alt: 'Áo dài truyền thống màu đỏ nhung', name: 'Đỏ nhung' },
      { src: '/assets/collection/traditional-pink.png', alt: 'Áo dài truyền thống màu hồng phấn', name: 'Hồng Phấn' },
      { src: '/assets/collection/traditional-cream.png', alt: 'Áo dài truyền thống màu trắng kem', name: 'Trắng kem' },
    ],
  },
];

export default function CollectionSection() {
  const [selectedTab, setSelectedTab] = useState(0);
  const activeCollection = collectionTabs[selectedTab];

  return (
    <motion.section
      className={`${styles.collectionTexture} ${styles.collectionSection}`}
      id="collection"
      aria-labelledby="collection-title"
      variants={sectionReveal}
      initial="hidden"
      whileInView="show"
      viewport={viewportOnce}
    >
      <div className={styles.floralTexture} aria-hidden="true" />
      <motion.div className={styles.goldStar} aria-hidden="true" variants={fadeScale} />
      <motion.h2 className="script-title" id="collection-title" variants={fadeUp}>
        Bộ sưu tập
      </motion.h2>
      <CategoryPills
        categories={collectionTabs.map((tab) => tab.label)}
        selected={selectedTab}
        onSelect={setSelectedTab}
      />
      <DressShowcase dresses={activeCollection.dresses} tabLabel={activeCollection.label} />
    </motion.section>
  );
}
