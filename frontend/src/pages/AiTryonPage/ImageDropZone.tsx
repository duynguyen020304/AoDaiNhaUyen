import { useRef, useState, type DragEvent } from 'react';
import { AnimatePresence, motion } from 'framer-motion';
import { convertToSupportedFormat } from '../../utils/imageConversion';
import styles from './ImageDropZone.module.css';

interface ImageDropZoneProps {
  onFileSelect: (file: File) => void;
  compact?: boolean;
  photoUrl?: string | null;
  fileName?: string | null;
  source?: 'file' | 'paste';
}

export default function ImageDropZone({
  onFileSelect,
  compact,
  photoUrl,
  fileName,
}: ImageDropZoneProps) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [dragOver, setDragOver] = useState(false);
  const [previewOpen, setPreviewOpen] = useState(false);

  const handleFiles = async (files: FileList | null) => {
    if (!files?.length) return;
    const file = files[0];
    if (!isAllowedImage(file)) return;
    const processedFile = await convertToSupportedFormat(file);
    onFileSelect(processedFile);
  };

  const onDragOver = (e: DragEvent) => {
    e.preventDefault();
    setDragOver(true);
  };

  const onDragLeave = () => setDragOver(false);

  const onDrop = (e: DragEvent) => {
    e.preventDefault();
    setDragOver(false);
    handleFiles(e.dataTransfer.files);
  };

  const downloadName = fileName ?? 'uploaded-image.png';

  const handleDownload = () => {
    if (!photoUrl) return;
    if (photoUrl.startsWith('data:')) {
      const blobUrl = dataUrlToObjectUrl(photoUrl);
      triggerDownload(blobUrl, downloadName);
      window.setTimeout(() => URL.revokeObjectURL(blobUrl), 0);
      return;
    }
    triggerDownload(photoUrl, downloadName);
  };

  if (compact && fileName) {
    return (
      <>
        <div className={styles.compact}>
          {photoUrl ? (
            <button
              type="button"
              className={styles.previewButton}
              onClick={() => setPreviewOpen(true)}
              aria-label="Xem trước ảnh đã tải lên"
            >
              <img src={photoUrl} alt="Ảnh đã tải lên" />
            </button>
          ) : null}
          <div className={styles.compactInfo}>
            <span
              className={styles.fileName}
              onClick={() => setPreviewOpen(true)}
            >
              {fileName}
            </span>
            <button
              type="button"
              className={styles.changeBtn}
              onClick={() => inputRef.current?.click()}
            >
              Thay đổi
            </button>
          </div>
          <input
            ref={inputRef}
            type="file"
            accept="image/*"
            className={styles.hiddenInput}
            onChange={(e) => handleFiles(e.target.files)}
          />
        </div>

        <AnimatePresence>
          {previewOpen && photoUrl && (
            <motion.div
              className={styles.previewOverlay}
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              onClick={() => setPreviewOpen(false)}
            >
              <motion.img
                src={photoUrl}
                alt="Preview"
                className={styles.previewImage}
                initial={{ scale: 0.9, opacity: 0 }}
                animate={{ scale: 1, opacity: 1 }}
                exit={{ scale: 0.9, opacity: 0 }}
                onClick={(e) => e.stopPropagation()}
              />
              <div className={styles.previewActions} onClick={(e) => e.stopPropagation()}>
                <button
                  type="button"
                  className={styles.previewAction}
                  onClick={handleDownload}
                >
                  Tải xuống
                </button>
              </div>
              <button
                type="button"
                className={styles.previewClose}
                onClick={() => setPreviewOpen(false)}
              >
                ✕
              </button>
            </motion.div>
          )}
        </AnimatePresence>
      </>
    );
  }

  return (
    <div
      className={`${styles.dropZone} ${dragOver ? styles.dragOver : ''}`}
      onDragOver={onDragOver}
      onDragLeave={onDragLeave}
      onDrop={onDrop}
      onClick={() => inputRef.current?.click()}
    >
      <img src="/assets/ai-tryon/upload-icon.svg" alt="" className={styles.uploadIcon} />
      <p>Nhấn để tải ảnh lên</p>
      <span>Hỗ trợ mọi định dạng ảnh trừ GIF (Khuyến khích ảnh chụp chân dung)</span>
      <input
        ref={inputRef}
        type="file"
        accept="image/*"
        className={styles.hiddenInput}
        onChange={(e) => handleFiles(e.target.files)}
      />
    </div>
  );
}

function isAllowedImage(file: File) {
  return file.type.startsWith('image/') && file.type !== 'image/gif';
}

function triggerDownload(href: string, filename: string) {
  const link = document.createElement('a');
  link.href = href;
  link.download = filename;
  link.rel = 'noopener';
  document.body.appendChild(link);
  link.click();
  link.remove();
}

function dataUrlToObjectUrl(dataUrl: string) {
  const commaIndex = dataUrl.indexOf(',');
  const meta = dataUrl.slice(0, commaIndex);
  const data = dataUrl.slice(commaIndex + 1);
  const mimeMatch = meta.match(/^data:(.*?)(;base64)?$/);
  const mimeType = mimeMatch?.[1] || 'image/png';
  const isBase64 = meta.includes(';base64');
  const raw = isBase64 ? atob(data) : decodeURIComponent(data);
  const bytes = new Uint8Array(raw.length);
  for (let index = 0; index < raw.length; index += 1) {
    bytes[index] = raw.charCodeAt(index);
  }
  return URL.createObjectURL(new Blob([bytes], { type: mimeType }));
}
