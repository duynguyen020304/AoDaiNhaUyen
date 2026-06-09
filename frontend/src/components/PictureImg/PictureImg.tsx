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
  const resolved = resolveAssetUrl(src) ?? '/assets/footer-logo.png';
  const base = resolved.replace(/\.(jpe?g|png|webp|avif)(\?.*)?$/i, '');

  return (
    <picture>
      <source srcSet={`${base}-400.avif 400w, ${base}-800.avif 800w, ${base}-1200.avif 1200w`} type="image/avif" sizes={sizes} />
      <source srcSet={`${base}-400.webp 400w, ${base}-800.webp 800w, ${base}-1200.webp 1200w`} type="image/webp" sizes={sizes} />
      <img
        className={className}
        src={resolved}
        alt={alt}
        width={width}
        height={height}
        loading={lazy ? 'lazy' : 'eager'}
        decoding={lazy ? 'async' : 'sync'}
        fetchPriority={fetchPriority}
        onError={(event) => {
          if (event.currentTarget.src.endsWith('/assets/footer-logo.png')) return;
          event.currentTarget.src = '/assets/footer-logo.png';
        }}
      />
    </picture>
  );
}
