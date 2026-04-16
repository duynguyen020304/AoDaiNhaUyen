# Áo Dài Nhã Uyên - MVP Spec

## Overview
Premium Vietnamese áo dài (traditional dress) e-commerce landing page. Brand: "Áo Dài Nhã Uyên".
Tech: React 19 + TypeScript + Vite + react-router-dom + framer-motion. No Tailwind - raw CSS via PostCSS (nested, autoprefixer, cssnano).

---

## Pages & Routes

| Route | Page | Priority |
|---|---|---|
| `/` | Landing Page (Home) | MVP |
| `/collection/:slug` | Collection Detail | MVP |
| `/product/:slug` | Product Detail | MVP |
| `/cart` | Shopping Cart | MVP |
| `/login` | Login | MVP |
| `/register` | Register | MVP |
| `/account` | Account Dashboard | MVP |
| `/account/orders` | Order Management | MVP |
| `/account/addresses` | Address Management | MVP |
| `/stores` | Store Locator | Post-MVP |

---

## Landing Page (`/`) - Sections (top → bottom)

### 1. Navbar
- Logo: "Áo dài Nhã Uyên" (Italianno script font)
- Navigation links: Trang chủ, Bộ sưu tập, Áo dài, Phụ kiện, Hệ thống cửa hàng
- Right side: Search icon, Cart icon (with badge), User menu dropdown
- User dropdown items: Thông tin tài khoản, Quản lý đơn hàng, Danh sách địa chỉ, Đăng xuất
- Sticky on scroll, transparent → solid background transition
- Mobile: hamburger menu

### 2. Hero Section
- Full-width hero image (áo dài model photography)
- Brand headline overlay
- CTA button: "Khám phá bộ sưu tập"
- Decorative elements: gold accents, star shapes, circular frames

### 3. QuickAI / Featured Section
- Grid of featured product cards (4-up desktop, 2-up tablet, 1-up mobile)
- Each card: product image, name, price, quick-view overlay
- Hover: gold border glow effect, scale animation

### 4. Features Grid
- 3-column feature highlights
- Icons + text describing brand values (quality, tradition, craftsmanship)

### 5. Bộ Sưu Tập (Collections Showcase)
- Section title: "Bộ sưu tập" (decorative italic font, gold color, glow effect)
- Collection cards with:
  - Product image with decorative frame
  - Collection name (e.g., "Hồng Phấn", "Trắng kem", "Xanh Lục Bảo")
  - Decorative shapes (boolean SVG shapes, star accents)
  - CTA button per collection
- Carousel/grid layout

### 6. Chất Liệu (Materials Section)
- Section title: "Chất liệu"
- Feature card for fabric types (e.g., "Vải lụa")
- Description text about fabric qualities
- Decorative circular elements, dragon pattern overlay
- Image of fabric texture

### 7. Product Detail Preview (Áo Dài Cách Tân)
- Large product showcase
- Product title: "ÁO DÀI CÁCH TÂN"
- Detailed description sections:
  1. Đặc điểm thiết kế (design features: kiểu dáng, cổ áo, tay áo, họa tiết)
  2. Thành phần phối bộ (material composition, accessories)
  3. Phong cách chủ đạo (style direction)
- Decorative frames, gold borders, circular image containers
- Multiple product images in decorative layouts

### 8. Footer
- Multi-column layout:
  - Brand info + logo
  - Navigation links
  - Contact info
  - Social media: Facebook, Instagram, YouTube, TikTok (icon buttons)
- Copyright bar at bottom
- Divider line above copyright
- "Back to top" button

---

## Collection Detail Page (`/collection/:slug`)
- Collection hero banner
- Product grid (filterable by category: Áo dài, Phụ kiện)
- Product cards: thumbnail, name, price
- Pagination or infinite scroll

## Product Detail Page (`/product/:slug`)
- Product image gallery (multiple views: front, back, detail)
- Product name, price, description
- Size selector
- Color/variant selector
- "Add to cart" button
- Related products section

## Cart Page (`/cart`)
- Cart items list: image, name, size, quantity, price
- Quantity adjuster (+/-)
- Remove item button
- Order summary: subtotal, shipping, total
- "Thanh toán" (checkout) button
- Empty cart state: "Giỏ hàng trống" with illustration

## Auth Pages (`/login`, `/register`)
- **Login**: email/phone, password (show/hide toggle), "Remember me" checkbox, "Forgot password?" link, login button, link to register
- **Register**: name, email/phone, password, confirm password, register button, link to login
- Form validation with error messages
- Clean, centered card layout

## Account Pages
- **Dashboard** (`/account`): Welcome message, quick links to orders/addresses
- **Orders** (`/account/orders`): Order list with status, date, total, order details link
- **Addresses** (`/account/addresses`): Saved addresses list, "Nhập địa chỉ mới" button, add/edit/delete address forms

---

## Design System

### Colors
| Token | Value | Usage |
|---|---|---|
| `--gold` | `#D4A853` (approx) | Accents, borders, decorative elements |
| `--gold-glow` | Gold with shadow/blur | Effects on stars, borders |
| `--cream` | `#F5F0E8` (approx) | Backgrounds |
| `--dark` | `#1A1A1A` | Text, dark backgrounds |
| `--white` | `#FFFFFF` | Cards, overlays |
| `--overlay` | `rgba(0,0,0,0.4)` | Image overlays |

### Typography
| Style | Font | Usage |
|---|---|---|
| `Italianno` | Italianno (Google Fonts) | Brand name, decorative headings |
| `italino` | Similar script font | Section titles ("Bộ sưu tập", "Chất liệu") |
| `Desktop Heading 3` | Serif/Sans-serif | Product names |
| `Destop Heading 6` | Sans-serif | Body text, descriptions |
| Default body | System sans-serif | UI text, labels |

### Spacing
- Section padding: `80px` vertical desktop, `48px` mobile
- Card gap: `24px`
- Container max-width: `1440px` centered

### Border Radius
- Cards: `16px`
- Buttons: `52px` (pill)
- Image frames: `8px`, `60px`, circular `50%`

### Effects
- Gold glow: `box-shadow` with gold color, blur, spread
- Image shadows: layered drop shadows on product images
- Hover: scale `1.05`, gold border transition

---

## Components (Reusable)

| Component | Props | Description |
|---|---|---|
| `Navbar` | - | Sticky nav with logo, links, search, cart, user menu |
| `Footer` | - | Multi-column footer with social links |
| `ProductCard` | `image`, `name`, `price`, `slug` | Product preview card with hover effect |
| `CollectionCard` | `image`, `name`, `slug`, `decorativeElements` | Collection showcase card |
| `Button` | `variant`, `size`, `children`, `onClick` | Primary (gold), secondary (outline), pill shape |
| `SectionTitle` | `children`, `decorative` | Decorative section heading with gold styling |
| `HeroSection` | `image`, `title`, `subtitle`, `cta` | Full-width hero with overlay |
| `ImageFrame` | `src`, `shape`, `decorative` | Decorative image container (circle, rounded, custom shape) |
| `BackToTop` | - | Floating button to scroll to top |
| `LoadingSpinner` | - | Loading animation ("loading in" component) |
| `EmptyCart` | - | Empty cart illustration + message |
| `FormField` | `label`, `type`, `value`, `onChange`, `error` | Reusable form input |
| `PasswordInput` | `value`, `onChange`, `error` | Password field with show/hide toggle |
| `QuantitySelector` | `value`, `onChange`, `min`, `max` | +/- quantity control |

---

## Animations (framer-motion)

| Element | Animation |
|---|---|
| Hero section | Fade-in on load, parallax on scroll |
| Product cards | Staggered fade-up on scroll into view |
| Collection cards | Scale-up on hover |
| Navbar | Background transition on scroll (transparent → solid) |
| Buttons | Scale on hover, color transition |
| Section titles | Fade-in with decorative elements |
| Page transitions | Fade between routes |
| Loading | Spinner animation |
| Back to top | Fade-in when scrolled, slide-up on hover |

---

## Assets Needed (from Figma)

### Images
- Hero image(s) - áo dài model photography
- Product images per collection (Hồng Phấn, Trắng kem, Xanh Lục Bảo)
- Fabric texture images
- Decorative pattern overlays (dragon pattern, Vietnamese cultural elements)
- Collection thumbnails
- Empty cart illustration

### Icons
- Search, Cart, User (navbar)
- Facebook, Instagram, YouTube, TikTok (footer)
- Back to top arrow
- Password show/hide eye icon
- Menu hamburger (mobile)

### Existing Assets (in project root)
- `bst2-product.png`, `bst2-street.jpg`
- `bst3-bg.jpg`, `bst3-product.png`, `bst3-street.jpg`
- `bst4-bg.jpg`, `bst4-cach-tan.png`, `bst4-product.png`
- `bst5-cach-tan.png`, `bst5-product.png`, `bst5-reflection.png`
- `collection-home.png`, `collection-page-bottom.png`, `collection-page-top.png`
- `gal-cach-tan.png`, `gal-extra.png`, `gal-lua-tron.png`
- `gal-theu-hoa-1.png`, `gal-theu-hoa-2.jpg`
- `gal-truyen-thong-1.png`, `gal-truyen-thong-2.png`
- `gallery-pattern.png`, `gallery-texture.jpg`
- `texture-bg.jpg`, `thumb-1-view.png`, `thumb-2-view.png`, `thumb-3-view.png`
- `vector-decor.svg`

---

## Data Model (MVP - Mock/Local)

```typescript
interface Product {
  id: string;
  slug: string;
  name: string;
  price: number;
  description: string;
  images: string[];
  collection: string;
  category: 'ao-dai' | 'phu-kien';
  variants: ProductVariant[];
}

interface ProductVariant {
  id: string;
  name: string; // e.g., "Hồng Phấn", "Trắng kem"
  color: string;
  sizes: string[];
  image: string;
}

interface CartItem {
  product: Product;
  variant: ProductVariant;
  size: string;
  quantity: number;
}

interface Collection {
  slug: string;
  name: string;
  description: string;
  heroImage: string;
  products: Product[];
}
```

---

## MVP Scope Rules

### IN Scope
- Landing page with all sections
- Collection browsing
- Product detail view
- Add to cart (client-side state)
- Cart management (add, remove, quantity)
- Login/Register UI (no backend - mock auth)
- Account pages UI (mock data)
- Responsive design (desktop, tablet, mobile)
- All animations and decorative elements
- Footer with social links

### OUT of Scope (Post-MVP)
- Backend / API integration
- Real authentication
- Payment processing / checkout
- Store locator with map
- Search functionality
- Product filtering/sorting
- Wishlist
- Order tracking
- Admin dashboard
- CMS for products

---

## File Structure

```
frontend/src/
├── main.tsx
├── App.tsx                          # Router setup
├── pages/
│   ├── Home.tsx                     # Landing page (all sections)
│   ├── Collection.tsx               # Collection detail
│   ├── Product.tsx                  # Product detail
│   ├── Cart.tsx                     # Shopping cart
│   ├── Login.tsx                    # Login form
│   ├── Register.tsx                 # Register form
│   ├── Account.tsx                  # Account dashboard
│   ├── Orders.tsx                   # Order management
│   └── Addresses.tsx                # Address management
├── components/
│   ├── layout/
│   │   ├── Navbar.tsx
│   │   ├── Footer.tsx
│   │   └── BackToTop.tsx
│   ├── sections/
│   │   ├── HeroSection.tsx
│   │   ├── FeaturedSection.tsx
│   │   ├── FeaturesGrid.tsx
│   │   ├── CollectionsShowcase.tsx
│   │   ├── MaterialsSection.tsx
│   │   └── ProductPreview.tsx
│   ├── ui/
│   │   ├── Button.tsx
│   │   ├── ProductCard.tsx
│   │   ├── CollectionCard.tsx
│   │   ├── SectionTitle.tsx
│   │   ├── ImageFrame.tsx
│   │   ├── FormField.tsx
│   │   ├── PasswordInput.tsx
│   │   ├── QuantitySelector.tsx
│   │   ├── LoadingSpinner.tsx
│   │   └── EmptyCart.tsx
│   └── index.ts
├── styles/
│   ├── globals.css                  # CSS variables, resets, base
│   ├── navbar.css
│   ├── footer.css
│   ├── hero.css
│   ├── cards.css
│   ├── forms.css
│   └── animations.css
├── utils/
│   ├── data.ts                      # Mock data (products, collections)
│   ├── cart.ts                      # Cart state management (context)
│   └── constants.ts                 # Design tokens, config
└── vite-env.d.ts
```

---

## Implementation Order

1. **Setup**: Design tokens (CSS variables), global styles, font imports
2. **Layout shell**: Navbar + Footer + route structure
3. **Landing page sections**: Hero → Featured → Features → Collections → Materials → Product Preview
4. **Product pages**: Collection listing → Product detail
5. **Cart**: Cart page + cart state (React Context)
6. **Auth**: Login/Register forms
7. **Account**: Dashboard, Orders, Addresses (mock)
8. **Polish**: Animations, responsive, hover effects, decorative elements
