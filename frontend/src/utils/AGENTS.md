<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-04-19 | Updated: 2026-04-19 -->

# utils

## Purpose
Shared utility functions and constants used across components and pages. Provides reusable framer-motion animation variants and image format conversion.

## Key Files
| File | Description |
|------|-------------|
| `motion.ts` | Reusable framer-motion animation variants and constants. Exports: `easeOutQuart` (cubic bezier [0.22, 1, 0.36, 1]), `viewportOnce` (once: true, margin config), `sectionReveal` (opacity + y with stagger), `staggerContainer` (stagger children), `fadeUp` (opacity + y), `fadeScale` (opacity + scale), `maskReveal` (clipPath horizontal reveal), `coverReveal` (scaleX wipe), `listStagger` (faster stagger), `cardReveal` (opacity + y + scale), `carouselCopy` (enter/center/exit with blur), `carouselCopyItem` (carousel child), `imageParallaxHover` (rest/hover scale + y), `cardHover` (rest/hover y + scale) |
| `imageConversion.ts` | Image format converter for AI try-on compatibility. `convertToSupportedFormat(file: File)` converts unsupported image formats to JPEG (quality 0.92) using canvas. Supported formats: JPEG, PNG, WebP, HEIC, HEIF. Used before uploading images to the Gemini-based AI try-on API |

## For AI Agents
### Motion Variant Usage Pattern
```tsx
import { fadeUp, staggerContainer, viewportOnce } from '../utils/motion';

<motion.div
  variants={staggerContainer}
  initial="hidden"
  whileInView="show"
  viewport={viewportOnce}
>
  <motion.div variants={fadeUp}>Content</motion.div>
</motion.div>
```

### Animation Design Language
- All variants use `easeOutQuart` ([0.22, 1, 0.36, 1]) for smooth deceleration
- Scroll-triggered animations use `viewportOnce` (triggers once, with margin)
- Stagger patterns: `staggerContainer` (0.1s stagger), `listStagger` (0.08s stagger, faster)
- Card interactions use `cardHover` or `imageParallaxHover` for rest/hover states
- Carousel transitions include blur effects for depth

### Image Conversion
- Only needed for AI try-on file uploads
- Handles HEIC/HEIF conversion (common on iOS devices)
- Falls back to returning the original file if conversion fails
- Output: JPEG at 92% quality with `.jpg` extension
