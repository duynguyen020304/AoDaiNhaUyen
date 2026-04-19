<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-04-19 | Updated: 2026-04-19 -->

# pages

## Purpose
Route-level page components. Each page is a self-contained folder with the page component, sub-components, CSS Modules, and optional data files. Pages are imported and rendered by `App.tsx` via react-router-dom `<Routes>`.

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `AccessoriesPage/` | Accessories catalog page: `AccessoriesPage.tsx` + CSS Module |
| `AccountPage/` | Account management (modal overlay, not a routed page): `AccountPage.tsx` (main with `AccountView` type), `AccountSidebar.tsx`, `AccountInfo.tsx`, `AccountEditForm.tsx`, `AddressList.tsx`, `OrderList.tsx` + CSS Modules |
| `AiTryonPage/` | AI virtual try-on feature page: `AiTryonPage.tsx` (main), `CategoryTabs.tsx`, `ClothingPanel.tsx`, `AccessoryPanel.tsx`, `ImageDropZone.tsx`, `ResultPanel.tsx` + CSS Modules |
| `AuthCallbackPage/` | Shared OAuth callback base: `AuthCallbackPage.tsx` + CSS Module |
| `AuthFacebookCallbackPage/` | Facebook OAuth callback: `AuthFacebookCallbackPage.tsx` (uses AuthCallbackPage) |
| `AuthGoogleCallbackPage/` | Google OAuth callback: `AuthGoogleCallbackPage.tsx` (uses AuthCallbackPage) |
| `CartPage/` | Shopping cart page: `CartPage.tsx` (main), `CartItem.tsx`, `CartSummary.tsx`, `CustomerNotes.tsx`, `currency.ts` (VND formatting) + CSS Modules |
| `CollectionPage/` | Brand collection page: `CollectionPage.tsx` (main), `CollectionHero.tsx`, `BrandStorySection.tsx`, `EraSection.tsx`, `GallerySection.tsx`, `data.ts` (collection data) + CSS Modules |
| `DataDeletionPage/` | Data deletion request page: `DataDeletionPage.tsx` + CSS Module |
| `HomePage/` | Landing page composing multiple sections: `HomePage.tsx` + CSS Module |
| `LoginPage/` | Login page with email/password form and OAuth buttons: `LoginPage.tsx` + CSS Module |
| `NotFoundPage/` | 404 page: `NotFoundPage.tsx` + CSS Module |
| `PrivacyPolicyPage/` | Privacy policy page: `PrivacyPolicyPage.tsx` + CSS Module |
| `ProductsPage/` | Product catalog listing: `ProductsPage.tsx`, `data.ts` (filter/category data) + CSS Module |
| `ResetPasswordPage/` | Password reset page: `ResetPasswordPage.tsx` + CSS Module |

## For AI Agents
### Page Conventions
- Each page folder is self-contained: main component, sub-components, CSS Modules, and data files
- CSS Modules: `PageName.module.css` for page-level styles, `SubComponent.module.css` for sub-components
- `AccountPage` is special: rendered as a modal overlay in `App.tsx`, not via router. Accepts `activeView`, `onClose`, and `onViewChange` props. `AccountView` type is exported: `'profile' | 'profile/edit' | 'orders' | 'addresses'`
- Cart uses VND currency formatting via `CartPage/currency.ts`
- Collection and Products pages include `data.ts` files for static configuration
- OAuth callback pages (`AuthGoogleCallbackPage`, `AuthFacebookCallbackPage`) extract `code` from URL search params and call auth API

### Notable Sub-component Patterns
- `AiTryonPage` is the most complex page: has category tabs, clothing/accessory panels, image upload drop zone, and result panel
- `CartPage` separates concerns into CartItem, CartSummary, and CustomerNotes sub-components
- `CollectionPage` is divided into hero, brand story, era sections, and gallery
