import { useState, useEffect, useCallback } from 'react';
import { motion } from 'framer-motion';
import { useNavigate } from 'react-router-dom';
import { sectionReveal, fadeUp, viewportOnce } from '../../utils/motion';
import { convertToSupportedFormat } from '../../utils/imageConversion';
import { useAuth } from '../../auth/useAuth';
import AccessoryPanel from './AccessoryPanel';
import ClothingPanel from './ClothingPanel';
import ResultPanel from './ResultPanel';
import ImageDropZone from './ImageDropZone';
import { getAiTryOnCatalog, submitAiTryOn, type AiTryOnCatalogItem } from '../../api/aiTryon';
import { addCartItem } from '../../api/cart';
import styles from './AiTryonPage.module.css';

type UserPhotoSource = 'file' | 'paste';

export default function AiTryonPage() {
  const navigate = useNavigate();
  const { status } = useAuth();
  const [garments, setGarments] = useState<AiTryOnCatalogItem[]>([]);
  const [accessories, setAccessories] = useState<AiTryOnCatalogItem[]>([]);
  const [catalogLoading, setCatalogLoading] = useState(true);
  const [catalogError, setCatalogError] = useState<string | null>(null);
  const [userPhoto, setUserPhoto] = useState<string | null>(null);
  const [userPhotoFile, setUserPhotoFile] = useState<File | null>(null);
  const [userFileName, setUserFileName] = useState<string | null>(null);
  const [userPhotoSource, setUserPhotoSource] = useState<UserPhotoSource>('file');
  const [selectedAccessories, setSelectedAccessories] = useState<number[]>([]);
  const [selectedGarment, setSelectedGarment] = useState<number | null>(null);
  const [selectedCategory, setSelectedCategory] = useState('all');
  const [tryonResult, setTryonResult] = useState<string | null>(null);
  const [isProcessing, setIsProcessing] = useState(false);
  const [isPurchasing, setIsPurchasing] = useState(false);
  const [showLoginPrompt, setShowLoginPrompt] = useState(false);
  const [tryonError, setTryonError] = useState<string | null>(null);

  useEffect(() => {
    return () => {
      if (userPhoto) URL.revokeObjectURL(userPhoto);
    };
  }, [userPhoto]);

  useEffect(() => {
    let ignore = false;

    async function loadCatalog() {
      setCatalogLoading(true);
      setCatalogError(null);

      try {
        const result = await getAiTryOnCatalog();
        if (!ignore) {
          setGarments(result.garments);
          setAccessories(result.accessories);
        }
      } catch (error) {
        if (!ignore) {
          setCatalogError(error instanceof Error ? error.message : 'Không thể tải danh mục thử đồ.');
        }
      } finally {
        if (!ignore) {
          setCatalogLoading(false);
        }
      }
    }

    loadCatalog();

    return () => {
      ignore = true;
    };
  }, []);

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
    setSelectedAccessories([]);
    setSelectedGarment(null);
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
    setSelectedAccessories([]);
    setSelectedGarment(null);
  }, []);

  const handleToggleAccessory = useCallback((id: number) => {
    setSelectedAccessories((prev) => {
      if (prev.includes(id)) {
        setTryonError(null);
        return prev.filter((a) => a !== id);
      }

      if (prev.length >= 3) {
        setTryonError('Bạn chỉ có thể chọn tối đa 3 phụ kiện cho mỗi lần thử đồ.');
        return prev;
      }

      setTryonError(null);
      return [...prev, id];
    });
    setTryonResult(null);
  }, []);

  const handleSelectGarment = useCallback((id: number) => {
    setSelectedGarment(id);
    setTryonResult(null);
    setTryonError(null);
  }, []);

  const handleTryonClick = useCallback(async () => {
    if (!userPhotoFile || !selectedGarment) return;

    const garment = garments.find((item) => item.productId === selectedGarment);
    if (!garment) {
      setTryonError('Không tìm thấy trang phục đã chọn.');
      return;
    }

    setIsProcessing(true);
    setTryonError(null);

    try {
      const result = await submitAiTryOn({
        personImage: userPhotoFile,
        garmentProductId: garment.productId,
        garmentVariantId: garment.defaultVariantId,
        accessoryProductIds: selectedAccessories,
      });

      setTryonResult(result.resultImageUrl);
    } catch (error) {
      setTryonResult(null);
      setTryonError(error instanceof Error ? error.message : 'Không thể tạo ảnh thử đồ.');
    } finally {
      setIsProcessing(false);
    }
  }, [garments, selectedAccessories, selectedGarment, userPhotoFile]);

  const handleBuyNowClick = useCallback(async () => {
    if (!tryonResult || !selectedGarment) return;

    if (status === 'loading') {
      setTryonError('Hệ thống đang kiểm tra phiên đăng nhập. Vui lòng thử lại sau giây lát.');
      return;
    }

    if (status === 'anonymous') {
      setShowLoginPrompt(true);
      return;
    }

    const selectedItems = [
      garments.find((item) => item.productId === selectedGarment),
      ...selectedAccessories
        .map((productId) => accessories.find((item) => item.productId === productId)),
    ].filter((item): item is AiTryOnCatalogItem => Boolean(item));

    const purchasableItems = selectedItems.filter(
      (item) => typeof item.defaultVariantId === 'number',
    );

    if (purchasableItems.length === 0) {
      setTryonError('Các sản phẩm đã chọn chưa có phiên bản để thêm vào giỏ hàng.');
      return;
    }

    setIsPurchasing(true);
    setTryonError(null);

    try {
      for (const item of purchasableItems) {
        await addCartItem({ variantId: item.defaultVariantId as number, quantity: 1 });
      }

      navigate('/cart');
    } catch (error) {
      setTryonError(error instanceof Error ? error.message : 'Không thể thêm sản phẩm vào giỏ hàng.');
    } finally {
      setIsPurchasing(false);
    }
  }, [accessories, garments, navigate, selectedAccessories, selectedGarment, status, tryonResult]);

  return (
    <main
      className={styles.page}
      onPaste={async (event) => {
        const clipboardItems = event.clipboardData?.items;
        const clipboardFiles = event.clipboardData?.files;

        const pastedFile = getPastedImageFile(clipboardItems, clipboardFiles);
        if (!pastedFile) return;

        event.preventDefault();
        const convertedFile = await convertToSupportedFormat(pastedFile);
        handlePastePhoto(convertedFile);
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

      {catalogError ? (
        <section className={styles.hero}>
          <p className={styles.description}>{catalogError}</p>
        </section>
      ) : null}

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

          {userPhoto ? (
            <>
              {/* Clothing selection */}
              <ClothingPanel
                selectedCategory={selectedCategory}
                selectedGarment={selectedGarment}
                garments={garments}
                onCategoryChange={setSelectedCategory}
                onSelectGarment={handleSelectGarment}
              />

              {/* Accessories selection */}
              <AccessoryPanel
                accessories={accessories}
                selectedAccessories={selectedAccessories}
                onToggleAccessory={handleToggleAccessory}
              />
            </>
          ) : null}
        </motion.div>

        {/* Right column: Results */}
        <motion.div variants={fadeUp}>
          <ResultPanel
            tryonResult={tryonResult}
            selectedGarment={selectedGarment ? String(selectedGarment) : null}
            canTryOn={!catalogLoading && !!userPhotoFile && !!selectedGarment}
            isProcessing={catalogLoading || isProcessing}
            isPurchasing={isPurchasing}
            errorMessage={tryonError}
            onTryonClick={handleTryonClick}
            onBuyNowClick={handleBuyNowClick}
          />
        </motion.div>
      </motion.section>

      {showLoginPrompt ? (
        <div
          className={styles.loginPromptOverlay}
          role="presentation"
          onClick={() => setShowLoginPrompt(false)}
        >
          <motion.div
            className={styles.loginPrompt}
            role="dialog"
            aria-modal="true"
            aria-labelledby="ai-tryon-login-title"
            initial={{ opacity: 0, y: 18, scale: 0.98 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            transition={{ duration: 0.18 }}
            onClick={(event) => event.stopPropagation()}
          >
            <h2 id="ai-tryon-login-title">Bạn có muốn đăng nhập không?</h2>
            <p>
              Đăng nhập để thêm các sản phẩm đã chọn vào giỏ hàng và tiếp tục thanh toán.
            </p>
            <div className={styles.loginPromptActions}>
              <button
                type="button"
                className={styles.loginPromptSecondary}
                onClick={() => setShowLoginPrompt(false)}
              >
                Để sau
              </button>
              <button
                type="button"
                className={styles.loginPromptPrimary}
                onClick={() => navigate('/login', { state: { from: '/ai-tryon' } })}
              >
                Đăng nhập
              </button>
            </div>
          </motion.div>
        </div>
      ) : null}
    </main>
  );
}

function isAllowedImage(file: File): boolean {
  return file.type.startsWith('image/') && file.type !== 'image/gif';
}

function getPastedImageFile(
  items: DataTransferItemList | undefined,
  files: FileList | undefined,
): File | null {
  if (items) {
    for (const item of Array.from(items)) {
      if (item.kind !== 'file') continue;
      const file = item.getAsFile();
      if (file && isAllowedImage(file)) {
        return file;
      }
    }
  }

  if (files) {
    for (const file of Array.from(files)) {
      if (isAllowedImage(file)) {
        return file;
      }
    }
  }

  return null;
}
