import { useRef, useState, type DragEvent } from 'react';
import { AnimatePresence, motion } from 'framer-motion';
import styles from './ImageDropZone.module.css';

interface ImageDropZoneProps {
  onFileSelect: (file: File) => void;
  compact?: boolean;
  photoUrl?: string | null;
  fileName?: string | null;
}

export default function ImageDropZone({ onFileSelect, compact, photoUrl, fileName }: ImageDropZoneProps) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [dragOver, setDragOver] = useState(false);
  const [previewOpen, setPreviewOpen] = useState(false);

  const handleFiles = (files: FileList | null) => {
    if (!files?.length) return;
    const file = files[0];
    if (file.type !== 'image/jpeg' && file.type !== 'image/png') return;
    onFileSelect(file);
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

  if (compact && fileName) {
    return (
      <>
        <div className={styles.compact}>
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
          <input
            ref={inputRef}
            type="file"
            accept="image/jpeg,image/png"
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
      <span>Hỗ trợ JPG, PNG (Khuyến khích ảnh chụp chân dung)</span>
      <input
        ref={inputRef}
        type="file"
        accept="image/jpeg,image/png"
        className={styles.hiddenInput}
        onChange={(e) => handleFiles(e.target.files)}
      />
    </div>
  );
}
