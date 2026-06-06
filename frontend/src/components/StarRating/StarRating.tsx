import { useState, useCallback } from 'react';
import styles from './StarRating.module.css';

interface StarRatingProps {
  rating: number;
  size?: 'sm' | 'md' | 'lg';
  interactive?: boolean;
  onRate?: (rating: number) => void;
  showValue?: boolean;
}

const sizeMap = {
  sm: 14,
  md: 20,
  lg: 28,
} as const;

let starUidCounter = 0;

function StarSvg({
  size,
  filled,
  fraction,
}: {
  size: number;
  filled: boolean;
  fraction?: number;
}) {
  const fillColor = filled ? '#FFD400' : 'none';
  const strokeColor = 'var(--muted)';
  const gradientId = `star-half-${++starUidCounter}`;

  const isPartial = fraction !== undefined && !filled && fraction > 0 && fraction < 1;

  const defs = isPartial ? (
    <defs>
      <linearGradient id={gradientId}>
        <stop offset={`${fraction! * 100}%`} stopColor="#FFD400" />
        <stop offset={`${fraction! * 100}%`} stopColor="transparent" />
      </linearGradient>
    </defs>
  ) : null;

  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 20 20"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
      aria-hidden="true"
    >
      {defs}
      <path
        d="M10 1.5L12.67 6.97L18.63 7.82L14.32 12.07L15.34 18.01L10 15.2L4.66 18.01L5.68 12.07L1.37 7.82L7.33 6.97L10 1.5Z"
        fill={isPartial ? `url(#${gradientId})` : fillColor}
        stroke={filled ? '#FFD400' : strokeColor}
        strokeWidth="1"
        strokeLinejoin="round"
      />
    </svg>
  );
}

export default function StarRating({
  rating,
  size = 'md',
  interactive = false,
  onRate,
  showValue = false,
}: StarRatingProps) {
  const px = sizeMap[size];
  const [hoverIndex, setHoverIndex] = useState<number | null>(null);

  const handleClick = useCallback(
    (star: number) => {
      if (interactive && onRate) {
        onRate(star);
      }
    },
    [interactive, onRate],
  );

  const displayRating = hoverIndex !== null ? hoverIndex : rating;
  const fullStars = Math.floor(displayRating);
  const fraction = displayRating - fullStars;

  const stars = Array.from({ length: 5 }, (_, i) => {
    const starNum = i + 1;
    const filled = starNum <= fullStars;
    let starFraction: number | undefined;

    if (!filled && starNum === fullStars + 1 && fraction > 0) {
      starFraction = fraction;
    }

    return (
      <button
        key={i}
        type="button"
        className={`${styles.starBtn} ${interactive ? styles.interactive : ''}`}
        style={{ width: px, height: px }}
        disabled={!interactive}
        onClick={() => handleClick(starNum)}
        onMouseEnter={() => interactive && setHoverIndex(starNum)}
        onMouseLeave={() => interactive && setHoverIndex(null)}
        aria-label={`${starNum} sao`}
        tabIndex={interactive ? 0 : -1}
      >
        <StarSvg size={px} filled={filled} fraction={starFraction} />
      </button>
    );
  });

  return (
    <div
      className={`${styles.wrapper} ${styles[size]}`}
      onMouseLeave={() => interactive && setHoverIndex(null)}
    >
      <div className={styles.stars}>{stars}</div>
      {showValue && (
        <span className={styles.value}>{displayRating.toFixed(1)}</span>
      )}
    </div>
  );
}
