<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-04-19 | Updated: 2026-04-19 -->

# components

## Purpose
Reusable UI components organized in PascalCase folders. Each component typically has a `.tsx` file and a paired `.module.css` file. Components are used across multiple pages and compose the main layout (Header, Footer) and feature sections (Hero, Collection, Products, etc.).

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `AccessorySection/` | Accessories showcase section with `AccessorySection.tsx` + CSS Module |
| `AiSection/` | AI try-on promotion section with `AiSection.tsx`, `AiVisual.tsx` (visual element), `AiCopy.tsx` (marketing copy), + CSS Module |
| `CategoryBanner/` | Category navigation banner with `CategoryBanner.tsx` + CSS Module |
| `ChatWidget/` | Self-contained floating chat widget with `ChatWidget.tsx` + CSS Module. Connects to `api/chat.ts` for AI assistant |
| `CollectionSection/` | Collection showcase with `CollectionSection.tsx`, `CategoryPills.tsx` (category filter tabs), `DressShowcase.tsx` (dress display), + CSS Module |
| `FeaturesStrip/` | Horizontal features/USP strip with `FeaturesStrip.tsx` + CSS Module |
| `Footer/` | Site footer with `Footer.tsx`, `NewsletterForm.tsx` (email subscription), + CSS Module |
| `Header/` | Site header/navigation with `Header.tsx` + CSS Module. Receives `onOpenAccount` callback prop |
| `HeroBlank/` | Full-width hero banner with `HeroBlank.tsx` + CSS Module |
| `MaterialSection/` | Material/fabric showcase section with `MaterialSection.tsx` + CSS Module |
| `ProductCard/` | Reusable product card component with `ProductCard.tsx` + CSS Module. Used in product listings |
| `ProductSection/` | Product showcase section with multiple sub-components: `ProductSection.tsx` (main), `ProductSectionHero.tsx`, `ProductSectionCopy.tsx`, `ProductSectionPreview.tsx`, `productSectionData.ts` (data), + CSS Module |
| `StoreSection/` | Store location/info section with `StoreSection.tsx` + CSS Module |
| `Toast/` | Toast notification system: `ToastContext.tsx` (provider), `ToastContextValue.ts` (type), `useToast.ts` (hook), `Toast.css` (styles) |

## For AI Agents
### Component Conventions
- Every component folder follows PascalCase naming
- CSS Modules: `ComponentName.module.css` alongside the TSX file
- Sub-components are co-located in the same folder (e.g., `Footer/NewsletterForm.tsx`)
- Animation variants imported from `utils/motion.ts` (fadeUp, cardReveal, staggerContainer, etc.)
- framer-motion `motion` components used throughout for scroll-triggered and hover animations

### Key Integration Points
- `Header` receives `onOpenAccount` prop from `App.tsx` to open the account modal
- `ChatWidget` is a self-contained floating component rendered globally in `App.tsx`
- `ProductCard` is the shared card component used in `ProductsPage` and feature sections
- `Toast` system: wrap calls with `useToast()` hook to show notifications across the app

### Import Pattern
```tsx
import Header from './components/Header/Header';
import { useToast } from './components/Toast/useToast';
```
