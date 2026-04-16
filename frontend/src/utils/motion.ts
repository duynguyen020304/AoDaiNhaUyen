import type { Variants } from 'framer-motion';

export const easeOutQuart = [0.22, 1, 0.36, 1] as const;

export const viewportOnce = {
  once: true,
  margin: '-80px 0px -120px',
} as const;

export const sectionReveal: Variants = {
  hidden: { opacity: 0, y: 36 },
  show: {
    opacity: 1,
    y: 0,
    transition: {
      duration: 0.72,
      ease: easeOutQuart,
      staggerChildren: 0.12,
    },
  },
};

export const staggerContainer: Variants = {
  hidden: {},
  show: {
    transition: {
      staggerChildren: 0.1,
      delayChildren: 0.08,
    },
  },
};

export const fadeUp: Variants = {
  hidden: { opacity: 0, y: 24 },
  show: {
    opacity: 1,
    y: 0,
    transition: {
      duration: 0.58,
      ease: easeOutQuart,
    },
  },
};

export const fadeScale: Variants = {
  hidden: { opacity: 0, scale: 0.96 },
  show: {
    opacity: 1,
    scale: 1,
    transition: {
      duration: 0.62,
      ease: easeOutQuart,
    },
  },
};

export const maskReveal: Variants = {
  hidden: {
    opacity: 0,
    clipPath: 'inset(0 100% 0 0)',
  },
  show: {
    opacity: 1,
    clipPath: 'inset(0 0% 0 0)',
    transition: {
      duration: 0.82,
      ease: easeOutQuart,
    },
  },
};

export const coverReveal: Variants = {
  hidden: { scaleX: 1, transformOrigin: 'left center' },
  show: {
    scaleX: 0,
    transition: {
      duration: 0.78,
      ease: easeOutQuart,
    },
  },
};

export const listStagger: Variants = {
  hidden: {},
  show: {
    transition: {
      staggerChildren: 0.08,
      delayChildren: 0.12,
    },
  },
};

export const cardReveal: Variants = {
  hidden: { opacity: 0, y: 34, scale: 0.98 },
  show: {
    opacity: 1,
    y: 0,
    scale: 1,
    transition: {
      duration: 0.62,
      ease: easeOutQuart,
    },
  },
};

export const carouselCopy: Variants = {
  enter: { opacity: 0, y: 28, filter: 'blur(8px)' },
  center: {
    opacity: 1,
    y: 0,
    filter: 'blur(0px)',
    transition: {
      duration: 0.62,
      ease: easeOutQuart,
      staggerChildren: 0.07,
    },
  },
  exit: {
    opacity: 0,
    y: -18,
    filter: 'blur(6px)',
    transition: {
      duration: 0.28,
      ease: 'easeIn',
    },
  },
};

export const carouselCopyItem: Variants = {
  enter: { opacity: 0, y: 18 },
  center: {
    opacity: 1,
    y: 0,
    transition: {
      duration: 0.5,
      ease: easeOutQuart,
    },
  },
  exit: { opacity: 0, y: -10 },
};

export const imageParallaxHover = {
  rest: { scale: 1, y: 0 },
  hover: { scale: 1.045, y: -8 },
};

export const cardHover = {
  rest: { y: 0, scale: 1 },
  hover: { y: -10, scale: 1.015 },
};
