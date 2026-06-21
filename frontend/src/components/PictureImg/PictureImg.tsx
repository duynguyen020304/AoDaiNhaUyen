import { useState } from 'react';
import { resolveAssetUrl } from '../../api/client';

interface PictureImgProps {
  src: string | null;
  alt: string;
  width: number;
  height: number;
  className?: string;
  lazy?: boolean;
  fetchPriority?: 'high' | 'low' | 'auto';
  sizes?: string;
}

export function PictureImg({ src, alt, width, height, className, lazy = true, fetchPriority = 'auto', sizes = '(max-width: 768px) 100vw, 800px' }: PictureImgProps) {
  const [failedSrc, setFailedSrc] = useState<string | null>(null);
  const resolved = resolveAssetUrl(src) ?? '/assets/footer-logo.png';
  const fallbackSrc = '/assets/footer-logo.png';
  const useFallback = failedSrc === resolved;
  const imageSrc = useFallback ? fallbackSrc : resolved;
  const base = resolved.replace(/\.(jpe?g|png|webp|avif)(\?.*)?$/i, '');
  const isRemoteAsset = /^https?:\/\//i.test(resolved);
  const canUseGeneratedSources = !useFallback && !isRemoteAsset;

  return (
    <picture>
      {canUseGeneratedSources ? (
        <>
          <source srcSet={`${base}-400.avif 400w, ${base}-800.avif 800w, ${base}-1200.avif 1200w`} type="image/avif" sizes={sizes} />
          <source srcSet={`${base}-400.webp 400w, ${base}-800.webp 800w, ${base}-1200.webp 1200w`} type="image/webp" sizes={sizes} />
        </>
      ) : null}
      <img
        className={className}
        src={imageSrc}
        alt={alt}
        width={width}
        height={height}
        loading={lazy ? 'lazy' : 'eager'}
        decoding={lazy ? 'async' : 'sync'}
        fetchPriority={fetchPriority}
        onError={() => setFailedSrc(resolved)}
      />
    </picture>
  );
}
