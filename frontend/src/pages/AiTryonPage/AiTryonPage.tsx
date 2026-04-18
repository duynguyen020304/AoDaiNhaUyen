import { useState, useEffect, useCallback } from 'react';
import { motion } from 'framer-motion';
import { sectionReveal, fadeUp, viewportOnce } from '../../utils/motion';
import AccessoryPanel from './AccessoryPanel';
import ClothingPanel from './ClothingPanel';
import ResultPanel from './ResultPanel';
import ImageDropZone from './ImageDropZone';
import { submitAiTryOn } from '../../api/aiTryon';
import { ACCESSORIES, GARMENTS } from './data';
import styles from './AiTryonPage.module.css';

type UserPhotoSource = 'file' | 'paste';

export default function AiTryonPage() {
  const [userPhoto, setUserPhoto] = useState<string | null>(null);
  const [userPhotoFile, setUserPhotoFile] = useState<File | null>(null);
  const [userFileName, setUserFileName] = useState<string | null>(null);
  const [userPhotoSource, setUserPhotoSource] = useState<UserPhotoSource>('file');
  const [selectedAccessories, setSelectedAccessories] = useState<string[]>([]);
  const [selectedGarment, setSelectedGarment] = useState<string | null>(null);
  const [selectedCategory, setSelectedCategory] = useState('all');
  const [tryonResult, setTryonResult] = useState<string | null>(null);
  const [isProcessing, setIsProcessing] = useState(false);
  const [tryonError, setTryonError] = useState<string | null>(null);

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
    setUserPhotoFile(file);
    setUserFileName(file.name);
    setUserPhotoSource('file');
    setTryonResult(null);
    setTryonError(null);
  }, []);

  const handlePastePhoto = useCallback((file: File) => {
    const pastedName = file.name && file.name.trim().length > 0
      ? file.name
      : `pasted-image-${new Date().toISOString().replace(/[:.]/g, '-')}.png`;

    const pastedFile = file.name === pastedName
      ? file
      : new File([file], pastedName, { type: file.type || 'image/png' });

    setUserPhoto((prev) => {
      if (prev) URL.revokeObjectURL(prev);
      return URL.createObjectURL(pastedFile);
    });
    setUserPhotoFile(pastedFile);
    setUserFileName(pastedFile.name);
    setUserPhotoSource('paste');
    setTryonResult(null);
    setTryonError(null);
  }, []);

  const handleToggleAccessory = useCallback((id: string) => {
    setSelectedAccessories((prev) =>
      prev.includes(id) ? prev.filter((a) => a !== id) : [...prev, id],
    );
  }, []);

  const handleTryonClick = useCallback(async () => {
    if (!userPhotoFile || !selectedGarment) return;

    const garment = GARMENTS.find((item) => item.id === selectedGarment);
    if (!garment) {
      setTryonError('Không tìm thấy trang phục đã chọn.');
      return;
    }

    setIsProcessing(true);
    setTryonError(null);

    try {
      const garmentImage = await fetchGarmentImage(garment.thumbnail, garment.id);
      const accessoryImages = await Promise.all(
        selectedAccessories.map(async (accessoryId) => {
          const accessory = ACCESSORIES.find((item) => item.id === accessoryId);
          if (!accessory) {
            throw new Error('Không tìm thấy phụ kiện đã chọn.');
          }

          return {
            id: accessory.id,
            file: await fetchTryOnAsset(accessory.thumbnail, accessory.id),
          };
        }),
      );
      const result = await submitAiTryOn({
        personImage: userPhotoFile,
        garmentImage,
        garmentId: garment.id,
        accessoryImages,
      });

      setTryonResult(result.resultImageUrl);
    } catch (error) {
      setTryonResult(null);
      setTryonError(error instanceof Error ? error.message : 'Không thể tạo ảnh thử đồ.');
    } finally {
      setIsProcessing(false);
    }
  }, [userPhotoFile, selectedGarment, selectedAccessories]);

  return (
    <main
      className={styles.page}
      onPaste={(event) => {
        const clipboardItems = event.clipboardData?.items;
        const clipboardFiles = event.clipboardData?.files;

        const pastedFile = getPastedImageFile(clipboardItems, clipboardFiles);
        if (!pastedFile) return;

        event.preventDefault();
        handlePastePhoto(pastedFile);
      }}
    >
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
              fileName={userFileName ?? undefined}
              source={userPhotoSource}
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
            canTryOn={!!userPhotoFile && !!selectedGarment}
            isProcessing={isProcessing}
            errorMessage={tryonError}
            onTryonClick={handleTryonClick}
          />
        </motion.div>
      </motion.section>
    </main>
  );
}

async function fetchGarmentImage(thumbnail: string, garmentId: string): Promise<File> {
  return fetchTryOnAsset(thumbnail, garmentId);
}

async function fetchTryOnAsset(thumbnail: string, fileName: string): Promise<File> {
  const response = await fetch(thumbnail);
  if (!response.ok) {
    throw new Error('Không thể tải ảnh đã chọn.');
  }

  const blob = await response.blob();
  const extension = blob.type === 'image/png' ? 'png' : 'jpg';
  return new File([blob], `${fileName}.${extension}`, {
    type: blob.type || 'image/png',
  });
}

function getPastedImageFile(
  items: DataTransferItemList | undefined,
  files: FileList | undefined,
): File | null {
  const supportedTypes = new Set(['image/jpeg', 'image/png', 'image/webp', 'image/gif']);

  if (items) {
    for (const item of Array.from(items)) {
      if (item.kind !== 'file') continue;
      const file = item.getAsFile();
      if (file && supportedTypes.has(file.type)) {
        return file;
      }
    }
  }

  if (files) {
    for (const file of Array.from(files)) {
      if (supportedTypes.has(file.type)) {
        return file;
      }
    }
  }

  return null;
}
