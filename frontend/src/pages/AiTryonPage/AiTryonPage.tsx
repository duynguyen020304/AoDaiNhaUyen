import { useState, useEffect, useCallback } from 'react';
import { motion } from 'framer-motion';
import { sectionReveal, fadeUp, viewportOnce } from '../../utils/motion';
import AccessoryPanel from './AccessoryPanel';
import ClothingPanel from './ClothingPanel';
import ResultPanel from './ResultPanel';
import ImageDropZone from './ImageDropZone';
import styles from './AiTryonPage.module.css';

export default function AiTryonPage() {
  const [userPhoto, setUserPhoto] = useState<string | null>(null);
  const [userFileName, setUserFileName] = useState<string | null>(null);
  const [selectedAccessories, setSelectedAccessories] = useState<string[]>([]);
  const [selectedGarment, setSelectedGarment] = useState<string | null>(null);
  const [selectedCategory, setSelectedCategory] = useState('all');
  const [tryonResult, setTryonResult] = useState<string | null>(null);
  const [isProcessing, setIsProcessing] = useState(false);

  useEffect(() => {
    return () => {
      if (userPhoto) URL.revokeObjectURL(userPhoto);
    };
  }, [userPhoto]);

  const handleUploadPhoto = useCallback((file: File) => {
    setUserPhoto((prev) => {
      if (prev) URL.revokeObjectURL(prev);
      return URL.createObjectURL(file);
    });
    setUserFileName(file.name);
  }, []);

  const handleToggleAccessory = useCallback((id: string) => {
    setSelectedAccessories((prev) =>
      prev.includes(id) ? prev.filter((a) => a !== id) : [...prev, id],
    );
  }, []);

  const handleTryonClick = useCallback(() => {
    if (!userPhoto || !selectedGarment) return;
    setIsProcessing(true);
    // TODO: API integration
    setTimeout(() => {
      setIsProcessing(false);
      setTryonResult(null);
    }, 2000);
  }, [userPhoto, selectedGarment]);

  return (
    <main className={styles.page}>
      <motion.section
        className={styles.hero}
        variants={sectionReveal}
        initial="hidden"
        whileInView="show"
        viewport={viewportOnce}
      >
        <motion.span className={styles.badge} variants={fadeUp}>
          BETA FEATURE
        </motion.span>
        <motion.h1 variants={fadeUp}>Phòng Thử Đồ Ảo AI</motion.h1>
        <motion.p className={styles.description} variants={fadeUp}>
          Tải lên ảnh khuôn mặt của bạn và để trí tuệ nhân tạo của MaryMy giúp bạn
          thử những thiết kế Áo Dài lộng lẫy nhất trước khi quyết định.
        </motion.p>
      </motion.section>

      <motion.section
        className={styles.mainSection}
        variants={sectionReveal}
        initial="hidden"
        whileInView="show"
        viewport={viewportOnce}
      >
        {/* Left column: User photo + Clothing + Accessories */}
        <motion.div variants={fadeUp} className={styles.leftCol}>
          {/* Upload / User photo */}
          <div className={styles.card}>
            <div className={styles.stepHeader}>
              <span className={styles.stepBadge}>1</span>
              <h2>TẢI LÊN ẢNH CỦA BẠN</h2>
            </div>
            <ImageDropZone
              compact={!!userPhoto}
              photoUrl={userPhoto}
              fileName={userFileName}
              onFileSelect={handleUploadPhoto}
            />
          </div>

          {/* Clothing selection */}
          <ClothingPanel
            selectedCategory={selectedCategory}
            selectedGarment={selectedGarment}
            onCategoryChange={setSelectedCategory}
            onSelectGarment={setSelectedGarment}
          />

          {/* Accessories selection */}
          <AccessoryPanel
            selectedAccessories={selectedAccessories}
            onToggleAccessory={handleToggleAccessory}
          />
        </motion.div>

        {/* Right column: Results */}
        <motion.div variants={fadeUp}>
          <ResultPanel
            tryonResult={tryonResult}
            selectedGarment={selectedGarment}
            isProcessing={isProcessing}
            onTryonClick={handleTryonClick}
          />
        </motion.div>
      </motion.section>
    </main>
  );
}
