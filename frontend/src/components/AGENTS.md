<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-04-19 | Updated: 2026-04-19 -->

# components

## Purpose
Reusable UI components in PascalCase folders. Each component usually has `.tsx` file plus paired `.module.css` file. Components used across pages, compose main layout (Header, Footer) and feature sections (Hero, Collection, Products, etc.).

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `AccessorySection/` | Accessories showcase section with `AccessorySection.tsx` + CSS Module |
| `AiSection/` | AI try-on promo section with `AiSection.tsx`, `AiVisual.tsx` (visual element), `AiCopy.tsx` (marketing copy), + CSS Module |
| `CategoryBanner/` | Category nav banner with `CategoryBanner.tsx` + CSS Module |
| `ChatWidget/` | Self-contained floating chat widget with `ChatWidget.tsx` + CSS Module. Connects to `api/chat.ts` for AI assistant |
| `CollectionSection/` | Collection showcase with `CollectionSection.tsx`, `CategoryPills.tsx` (category filter tabs), `DressShowcase.tsx` (dress display), + CSS Module |
| `FeaturesStrip/` | Horizontal features/USP strip with `FeaturesStrip.tsx` + CSS Module |
| `Footer/` | Site footer with `Footer.tsx`, `NewsletterForm.tsx` (email subscription), + CSS Module |
| `Header/` | Site header/nav with `Header.tsx` + CSS Module. Receives `onOpenAccount` callback prop |
| `HeroBlank/` | Full-width hero banner with `HeroBlank.tsx` + CSS Module |
| `MaterialSection/` | Material/fabric showcase section with `MaterialSection.tsx` + CSS Module |
| `ProductCard/` | Reusable product card component with `ProductCard.tsx` + CSS Module. Used in product listings |
| `ProductSection/` | Product showcase section with sub-components: `ProductSection.tsx` (main), `ProductSectionHero.tsx`, `ProductSectionCopy.tsx`, `ProductSectionPreview.tsx`, `productSectionData.ts` (data), + CSS Module |
| `StoreSection/` | Store location/info section with `StoreSection.tsx` + CSS Module |
| `Toast/` | Toast notification system: `ToastContext.tsx` (provider), `ToastContextValue.ts` (type), `useToast.ts` (hook), `Toast.css` (styles) |

## For AI Agents
### Component Conventions
- Every component folder uses PascalCase naming
- CSS Modules: `ComponentName.module.css` beside TSX file
- Sub-components co-located in same folder (e.g., `Footer/NewsletterForm.tsx`)
- Animation variants imported from `utils/motion.ts` (fadeUp, cardReveal, staggerContainer, etc.)
- framer-motion `motion` components used throughout for scroll-triggered and hover animations

### Key Integration Points
- `Header` receives `onOpenAccount` prop from `App.tsx` to open account modal
- `ChatWidget` is self-contained floating component rendered globally in `App.tsx`
- `ProductCard` is shared card component used in `ProductsPage` and feature sections
- `Toast` system: wrap calls with `useToast()` hook to show app-wide notifications

### Import Pattern
```tsx
import Header from './components/Header/Header';
import { useToast } from './components/Toast/useToast';
```