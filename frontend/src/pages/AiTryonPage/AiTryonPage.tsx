import { useState, useEffect, useCallback } from 'react';
import { motion } from 'framer-motion';
import { useNavigate } from 'react-router-dom';
import { sectionReveal, fadeUp, viewportOnce } from '../../utils/motion';
import { convertToSupportedFormat } from '../../utils/imageConversion';
import { useAuthModal } from '../../auth/AuthModalContext';
import { useAuth } from '../../auth/useAuth';
import AccessoryPanel from './AccessoryPanel';
import ClothingPanel from './ClothingPanel';
import ResultPanel from './ResultPanel';
import ImageDropZone from './ImageDropZone';
import {
  getAiTryOnCatalog,
  submitAiTryOn,
  type AiTryOnCatalogCategory,
  type AiTryOnCatalogItem,
  type AiTryOnCatalogPage,
} from '../../api/aiTryon';
import { createAiTryOnFeedback } from '../../api/aiTryonFeedback';
import { trackEvent } from '../../api/events';
import { addCartItem } from '../../api/cart';
import styles from './AiTryonPage.module.css';

type UserPhotoSource = 'file' | 'paste';
const CATALOG_PAGE_SIZE = 6;
const EMPTY_CATALOG_PAGE: AiTryOnCatalogPage = {
  items: [],
  page: 1,
  pageSize: CATALOG_PAGE_SIZE,
  totalItems: 0,
  totalPages: 1,
};
const DEFAULT_GARMENT_CATEGORIES: AiTryOnCatalogCategory[] = [
  { key: 'all', label: 'Tất cả' },
  { key: 'bestseller', label: 'Bestseller' },
];
const DEFAULT_ACCESSORY_CATEGORIES: AiTryOnCatalogCategory[] = [
  { key: 'all', label: 'Tất cả' },
];

export default function AiTryonPage() {
  const navigate = useNavigate();
  const { status } = useAuth();
  const { openAuthModal } = useAuthModal();
  const [garments, setGarments] = useState<AiTryOnCatalogItem[]>([]);
  const [accessories, setAccessories] = useState<AiTryOnCatalogItem[]>([]);
  const [garmentPage, setGarmentPage] = useState<AiTryOnCatalogPage>(EMPTY_CATALOG_PAGE);
  const [accessoryPage, setAccessoryPage] = useState<AiTryOnCatalogPage>(EMPTY_CATALOG_PAGE);
  const [garmentCategories, setGarmentCategories] = useState<AiTryOnCatalogCategory[]>(DEFAULT_GARMENT_CATEGORIES);
  const [accessoryCategories, setAccessoryCategories] = useState<AiTryOnCatalogCategory[]>(DEFAULT_ACCESSORY_CATEGORIES);
  const [catalogLoading, setCatalogLoading] = useState(true);
  const [catalogError, setCatalogError] = useState<string | null>(null);
  const [userPhoto, setUserPhoto] = useState<string | null>(null);
  const [userPhotoFile, setUserPhotoFile] = useState<File | null>(null);
  const [userFileName, setUserFileName] = useState<string | null>(null);
  const [userPhotoSource, setUserPhotoSource] = useState<UserPhotoSource>('file');
  const [selectedAccessories, setSelectedAccessories] = useState<string[]>([]);
  const [selectedAccessoryItems, setSelectedAccessoryItems] = useState<Record<string, AiTryOnCatalogItem>>({});
  const [selectedGarment, setSelectedGarment] = useState<string | null>(null);
  const [selectedGarmentItem, setSelectedGarmentItem] = useState<AiTryOnCatalogItem | null>(null);
  const [selectedCategory, setSelectedCategory] = useState('all');
  const [selectedAccessoryCategory, setSelectedAccessoryCategory] = useState('all');
  const [garmentPageNumber, setGarmentPageNumber] = useState(1);
  const [accessoryPageNumber, setAccessoryPageNumber] = useState(1);
  const [tryonResult, setTryonResult] = useState<string | null>(null);
  const [isProcessing, setIsProcessing] = useState(false);
  const [isPurchasing, setIsPurchasing] = useState(false);
  const [showLoginPrompt, setShowLoginPrompt] = useState(false);
  const [generatedImageId, setGeneratedImageId] = useState<string | null>(null);
  const [showFeedbackModal, setShowFeedbackModal] = useState(false);
  const [feedbackRating, setFeedbackRating] = useState(5);
  const [feedbackComment, setFeedbackComment] = useState('');
  const [feedbackError, setFeedbackError] = useState<string | null>(null);
  const [feedbackSubmitting, setFeedbackSubmitting] = useState(false);
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
        const result = await getAiTryOnCatalog({
          garmentPage: garmentPageNumber,
          accessoryPage: accessoryPageNumber,
          pageSize: CATALOG_PAGE_SIZE,
          garmentCategory: selectedCategory,
          accessoryCategory: selectedAccessoryCategory,
        });
        if (!ignore) {
          setGarments(result.garments.items);
          setAccessories(result.accessories.items);
          setGarmentPage(result.garments);
          setAccessoryPage(result.accessories);
          setGarmentCategories(result.garmentCategories);
          setAccessoryCategories(result.accessoryCategories);
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
  }, [accessoryPageNumber, garmentPageNumber, selectedAccessoryCategory, selectedCategory]);

  const handleUploadPhoto = useCallback((file: File) => {
    setUserPhoto((prev) => {
      if (prev) URL.revokeObjectURL(prev);
      return URL.createObjectURL(file);
    });
    setUserPhotoFile(file);
    setUserFileName(file.name);
    setUserPhotoSource('file');
    setTryonResult(null);
    setGeneratedImageId(null);
    setShowFeedbackModal(false);
    setTryonError(null);
    setSelectedAccessories([]);
    setSelectedAccessoryItems({});
    setSelectedGarment(null);
    setSelectedGarmentItem(null);
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
    setGeneratedImageId(null);
    setShowFeedbackModal(false);
    setTryonError(null);
    setSelectedAccessories([]);
    setSelectedAccessoryItems({});
    setSelectedGarment(null);
    setSelectedGarmentItem(null);
  }, []);

  const handleToggleAccessory = useCallback((item: AiTryOnCatalogItem) => {
    const id = item.productId;
    setSelectedAccessories((prev) => {
      if (prev.includes(id)) {
        setTryonError(null);
        setSelectedAccessoryItems((current) => {
          const next = { ...current };
          delete next[id];
          return next;
        });
        return prev.filter((a) => a !== id);
      }

      if (prev.length >= 3) {
        setTryonError('Bạn chỉ có thể chọn tối đa 3 phụ kiện cho mỗi lần thử đồ.');
        return prev;
      }

      setTryonError(null);
      setSelectedAccessoryItems((current) => ({
        ...current,
        [id]: item,
      }));
      return [...prev, id];
    });
    setTryonResult(null);
    setGeneratedImageId(null);
    setShowFeedbackModal(false);
  }, []);

  const handleSelectGarment = useCallback((item: AiTryOnCatalogItem) => {
    setSelectedGarment(item.productId);
    setSelectedGarmentItem(item);
    setTryonResult(null);
    setGeneratedImageId(null);
    setShowFeedbackModal(false);
    setTryonError(null);
  }, []);

  const handleGarmentCategoryChange = useCallback((category: string) => {
    setSelectedCategory(category);
    setGarmentPageNumber(1);
  }, []);

  const handleAccessoryCategoryChange = useCallback((category: string) => {
    setSelectedAccessoryCategory(category);
    setAccessoryPageNumber(1);
  }, []);

  const handleTryonClick = useCallback(async () => {
    if (!userPhotoFile || !selectedGarment) return;

    const garment = selectedGarmentItem;
    if (!garment) {
      setTryonError('Không tìm thấy trang phục đã chọn.');
      return;
    }

    void trackEvent({ eventType: 'ai_tryon_started', productId: garment.productId, productVariantId: garment.defaultVariantId, metadata: { accessoryProductIds: selectedAccessories } });
    setIsProcessing(true);
    setTryonError(null);

    try {
      const result = await submitAiTryOn({
        personImage: userPhotoFile,
        garmentProductId: garment.productId,
        garmentVariantId: garment.defaultVariantId,
        accessoryProductIds: selectedAccessories,
      });

      void trackEvent({ eventType: 'ai_tryon_completed', productId: garment.productId, productVariantId: garment.defaultVariantId, metadata: { accessoryProductIds: selectedAccessories } });
      setTryonResult(result.resultImageUrl);
      setGeneratedImageId(result.generatedImageId);
      if (result.generatedImageId) {
        setFeedbackRating(5);
        setFeedbackComment('');
        setFeedbackError(null);
        setShowFeedbackModal(true);
      }
    } catch (error) {
      setTryonResult(null);
      setGeneratedImageId(null);
      setTryonError(error instanceof Error ? error.message : 'Không thể tạo ảnh thử đồ.');
    } finally {
      setIsProcessing(false);
    }
  }, [selectedAccessories, selectedGarment, selectedGarmentItem, userPhotoFile]);

  const handleSubmitFeedback = useCallback(async () => {
    if (!generatedImageId) {
      setShowFeedbackModal(false);
      return;
    }

    setFeedbackSubmitting(true);
    setFeedbackError(null);
    try {
      await createAiTryOnFeedback({
        generatedImageId,
        rating: feedbackRating,
        comment: feedbackComment.trim() || undefined,
      });
      setShowFeedbackModal(false);
      setFeedbackComment('');
    } catch (error) {
      setFeedbackError(error instanceof Error ? error.message : 'Không thể gửi đánh giá.');
    } finally {
      setFeedbackSubmitting(false);
    }
  }, [feedbackComment, feedbackRating, generatedImageId]);

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
      selectedGarmentItem,
      ...selectedAccessories
        .map((productId) => selectedAccessoryItems[productId]),
    ].filter((item): item is AiTryOnCatalogItem => Boolean(item));

    const purchasableItems = selectedItems.filter(
      (item) => typeof item.defaultVariantId === 'string' && item.defaultVariantId.length > 0,
    );

    if (purchasableItems.length === 0) {
      setTryonError('Các sản phẩm đã chọn chưa có phiên bản để thêm vào giỏ hàng.');
      return;
    }

    setIsPurchasing(true);
    setTryonError(null);

    try {
      for (const item of purchasableItems) {
        await addCartItem({ variantId: item.defaultVariantId!, quantity: 1 });
      }

      navigate('/cart');
    } catch (error) {
      setTryonError(error instanceof Error ? error.message : 'Không thể thêm sản phẩm vào giỏ hàng.');
    } finally {
      setIsPurchasing(false);
    }
  }, [
    navigate,
    selectedAccessories,
    selectedAccessoryItems,
    selectedGarment,
    selectedGarmentItem,
    status,
    tryonResult,
  ]);

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
          Tải lên ảnh khuôn mặt của bạn và để trí tuệ nhân tạo của Nhã Uyên giúp bạn
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
                garmentPage={garmentPage}
                categories={garmentCategories}
                onCategoryChange={handleGarmentCategoryChange}
                onPageChange={setGarmentPageNumber}
                onSelectGarment={handleSelectGarment}
              />

              {/* Accessories selection */}
              <AccessoryPanel
                accessories={accessories}
                accessoryPage={accessoryPage}
                categories={accessoryCategories}
                selectedCategory={selectedAccessoryCategory}
                selectedAccessories={selectedAccessories}
                onCategoryChange={handleAccessoryCategoryChange}
                onPageChange={setAccessoryPageNumber}
                onToggleAccessory={handleToggleAccessory}
              />
            </>
          ) : null}
        </motion.div>

        {/* Right column: Results */}
        <motion.div variants={fadeUp} className={styles.resultCol}>
          <div className={!tryonResult ? styles.resultSticky : undefined}>
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
          </div>
        </motion.div>
      </motion.section>

      {showFeedbackModal ? (
        <div
          className={styles.loginPromptOverlay}
          role="presentation"
          onClick={() => setShowFeedbackModal(false)}
        >
          <motion.div
            className={styles.feedbackModal}
            role="dialog"
            aria-modal="true"
            aria-labelledby="ai-tryon-feedback-title"
            initial={{ opacity: 0, y: 18, scale: 0.98 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            transition={{ duration: 0.18 }}
            onClick={(event) => event.stopPropagation()}
          >
            <h2 id="ai-tryon-feedback-title">Bạn thấy ảnh thử đồ thế nào?</h2>
            <p>Đánh giá của bạn giúp Nhã Uyên cải thiện chất lượng AI try-on.</p>
            <div className={styles.ratingRow} aria-label="Chọn số sao">
              {[1, 2, 3, 4, 5].map((value) => (
                <button
                  key={value}
                  type="button"
                  className={value <= feedbackRating ? styles.starActive : styles.starButton}
                  onClick={() => setFeedbackRating(value)}
                  aria-label={`${value} sao`}
                >
                  ★
                </button>
              ))}
            </div>
            <textarea
              className={styles.feedbackTextarea}
              value={feedbackComment}
              onChange={(event) => setFeedbackComment(event.target.value)}
              placeholder="Góp ý thêm về độ giống, dáng áo, màu sắc..."
              rows={4}
            />
            {feedbackError ? <p className={styles.feedbackError}>{feedbackError}</p> : null}
            <div className={styles.loginPromptActions}>
              <button
                type="button"
                className={styles.loginPromptSecondary}
                onClick={() => setShowFeedbackModal(false)}
              >
                Để sau
              </button>
              <button
                type="button"
                className={styles.loginPromptPrimary}
                onClick={handleSubmitFeedback}
                disabled={feedbackSubmitting}
              >
                {feedbackSubmitting ? 'Đang gửi...' : 'Gửi đánh giá'}
              </button>
            </div>
          </motion.div>
        </div>
      ) : null}

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
                onClick={() => {
                  setShowLoginPrompt(false);
                  openAuthModal({ from: '/ai-tryon' });
                }}
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
