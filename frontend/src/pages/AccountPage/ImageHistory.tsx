import { useState } from 'react';
import { getImagePresignedUrl, type UserImage } from '../../api/media';
import { useMyImagesQuery } from '../../hooks/media/useMediaQueries';
import { resolveAssetUrl } from '../../api/client';
import styles from './ImageHistory.module.css';

const SOURCE_FILTERS = [
  { value: '', label: 'Tất cả' },
  { value: 'chat', label: 'Chat' },
  { value: 'ai_tryon', label: 'Thử đồ AI' },
] as const;

export default function ImageHistory() {
  const [sourceFilter, setSourceFilter] = useState('');
  const [page, setPage] = useState(1);
  const [previewImage, setPreviewImage] = useState<string | null>(null);
  const [previewLoading, setPreviewLoading] = useState(false);

  const imagesQuery = useMyImagesQuery(page, 12, sourceFilter || undefined);
  const data = imagesQuery.data ?? null;
  const loading = imagesQuery.isPending;
  const error = imagesQuery.error instanceof Error ? imagesQuery.error.message : null;

  function handleFilterChange(value: string) {
    setSourceFilter(value);
    setPage(1);
  }

  function handlePageChange(newPage: number) {
    setPage(newPage);
  }

  async function handlePreview(image: UserImage) {
    setPreviewLoading(true);
    try {
      const { url } = await getImagePresignedUrl(image.id);
      setPreviewImage(url);
    } catch {
      const fallback = resolveAssetUrl(image.url);
      setPreviewImage(fallback ?? image.url);
    } finally {
      setPreviewLoading(false);
    }
  }

  function closePreview() {
    setPreviewImage(null);
  }

  async function handleDownload(image: UserImage) {
    try {
      const { url } = await getImagePresignedUrl(image.id);
      const response = await fetch(url);
      const blob = await response.blob();
      const objectUrl = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = objectUrl;
      a.download = image.originalFileName ?? `image-${image.id}`;
      document.body.appendChild(a);
      a.click();
      a.remove();
      URL.revokeObjectURL(objectUrl);
    } catch {
      // fallback
    }
  }

  function formatFileSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  function formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('vi-VN', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
    });
  }

  if (loading && !data) {
    return <div className={styles.loading}>Đang tải...</div>;
  }

  if (error) {
    return <div className={styles.error}>{error}</div>;
  }

  return (
    <div className={styles.container}>
      <h2 className={styles.title}>Hình ảnh của tôi</h2>

      <div className={styles.filters}>
        {SOURCE_FILTERS.map(({ value, label }) => (
          <button
            key={value}
            type="button"
            className={`${styles.filterBtn} ${sourceFilter === value ? styles.filterBtnActive : ''}`}
            onClick={() => handleFilterChange(value)}
          >
            {label}
          </button>
        ))}
      </div>

      {data && data.items.length === 0 ? (
        <div className={styles.empty}>
          <p>Chưa có hình ảnh nào.</p>
          <p className={styles.emptyHint}>Gửi ảnh trong chat hoặc thử đồ AI để lưu lại.</p>
        </div>
      ) : (
        <>
          <div className={styles.grid}>
            {data?.items.map((image) => (
              <div key={image.id} className={styles.card}>
                <button
                  type="button"
                  className={styles.thumbnailBtn}
                  onClick={() => handlePreview(image)}
                >
                  <img
                    className={styles.thumbnail}
                    src={resolveAssetUrl(image.url) ?? image.url}
                    alt={image.originalFileName ?? 'Ảnh'}
                    loading="lazy"
                  />
                </button>
                <div className={styles.cardInfo}>
                  <span className={styles.sourceTag}>
                    {image.sourceType === 'ai_tryon' ? '✨ Thử đồ AI' : '💬 Chat'}
                  </span>
                  <span className={styles.fileSize}>{formatFileSize(image.fileSizeBytes)}</span>
                  <span className={styles.date}>{formatDate(image.createdAt)}</span>
                </div>
                <button
                  type="button"
                  className={styles.downloadBtn}
                  onClick={() => handleDownload(image)}
                  title="Tải xuống"
                >
                  ↓
                </button>
              </div>
            ))}
          </div>

          {data && data.totalPages > 1 && (
            <div className={styles.pagination}>
              <button
                type="button"
                disabled={page <= 1}
                onClick={() => handlePageChange(page - 1)}
              >
                ← Trước
              </button>
              <span>{page} / {data.totalPages}</span>
              <button
                type="button"
                disabled={page >= data.totalPages}
                onClick={() => handlePageChange(page + 1)}
              >
                Sau →
              </button>
            </div>
          )}
        </>
      )}

      {previewImage && (
        <div className={styles.previewOverlay} onMouseDown={(e) => {
          if (e.target === e.currentTarget) closePreview();
        }}>
          <button type="button" className={styles.previewClose} onClick={closePreview}>✕</button>
          {previewLoading ? (
            <div className={styles.loading}>Đang tải ảnh...</div>
          ) : (
            <img className={styles.previewImg} src={previewImage} alt="Preview" />
          )}
        </div>
      )}
    </div>
  );
}
