<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-04-19 | Updated: 2026-04-19 -->

# public

## Purpose
Static assets served at root URL path by Vite. Contains images, SVGs, media used across app. Referenced in code via absolute paths (e.g., `/assets/hero-bg.jpg`).

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `assets/` | Root-level static assets (shared images, SVGs, decorative elements) |
| `assets/accessories/` | Accessories page imagery: hero images (tram, quat), dragon pattern, mask overlays |
| `assets/ai-tryon/` | AI try-on page assets: footer logo, logo SVG, upload icon, social icons |
| `assets/ai-tryon/products/` | AI try-on product thumbnails: ao dai variants (th, lt, ct, tt) and accessories (phukien) |
| `assets/collection/` | Collection page imagery: hero background, era sections (bst2-bst6), gallery images (gal-*), decorative patterns |
| `assets/products/` | Product catalog images organized by category: cach-tan, lua-tron, theu-hoa, truyen-thong (6 images each) |

## Key Files (root assets/)
| File | Description |
|------|-------------|
| `red-floral.png` | Floral texture used in `.red-texture` CSS class overlay |
| `dragon.png` | Dragon decorative element |
| `dress-green/pink/white/panel.png` | Dress showcase images for collection/product sections |
| `ai-scene.png` | AI section background scene |
| `ai-card-blue.jpg` / `ai-card-yellow.jpg` | AI feature card backgrounds |
| `ai-wave.svg` / `ai-visual-backdrop.svg` | AI section decorative SVGs |
| `footer-logo.png` | Footer brand logo |
| `store-map.png` / `store-overlay.png` | Store location section imagery |
| `product-model.png` | Product section model image |
| `material-swatch-*.svg` | Material section swatch icons |
| `material-preview-*.png` / `material-preview-*-thumb.png` | Material section preview images |
| `material-title-flourish.svg` / `material-section-backdrop.svg` | Material section decorative SVGs |
| `accessory-tram.gif` | Animated accessory showcase |
| `top-flourish.png` / `sparkle.png` | Decorative flourishes |

## For AI Agents
### Working In This Directory
- All assets referenced via absolute paths: `/assets/<subdir>/<filename>`
- No `login/` or `product-section/` subdirectories exist (login uses inline styles, product section uses assets from other dirs)
- Product images use naming convention: `product-{category}-{number}.png` (categories: cach-tan, lua-tron, theu-hoa, truyen-thong)
- AI try-on product thumbnails use convention: `aodai-{category}-{number}.png` and `phukien-{number}.png`
- Collection era images: `bst{N}-{type}.{ext}` (bg, product, street, cach-tan, reflection, texture)
- Gallery images: `gal-{category}-{number}.{ext}` (truyen-thong, theu-hoa, lua-tron, cach-tan, extra)
- Adding new assets: follow existing naming conventions and put in correct subdirectory