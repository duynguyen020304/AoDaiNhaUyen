<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-04-19 | Updated: 2026-04-19 -->

# src

## Purpose
App source for Ao Dai Nha Uyen SPA. Has React components, pages, API modules, auth logic, types, utils, global styles.

## Key Files
| File | Description |
|------|-------------|
| `main.tsx` | React entry: renders `<BrowserRouter>` with `<AuthProvider>` and `<ToastProvider>` around `<App>` |
| `App.tsx` | Root component: defines routes, conditional Header/Footer, AccountPage modal overlay |
| `vite-env.d.ts` | Vite client types |

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `api/` | Fetch API client modules for backend communication (see `api/AGENTS.md`) |
| `auth/` | Auth context, protected route guard, useAuth hook (see `auth/AGENTS.md`) |
| `components/` | Reusable UI components in PascalCase folders (see `components/AGENTS.md`) |
| `pages/` | Route-level page components (see `pages/AGENTS.md`) |
| `styles/` | Global CSS: variables/design tokens, reset, typography, texture, transitions (see `styles/AGENTS.md`) |
| `types/` | TypeScript domain types (see `types/AGENTS.md`) |
| `utils/` | Utils: motion variants, image conversion (see `utils/AGENTS.md`) |

## For AI Agents
### Routes (defined in App.tsx)
| Path | Component | Notes |
|------|-----------|-------|
| `/` | `HomePage` | Landing page with hero, collection, product sections |
| `/collection` | `CollectionPage` | Brand story and gallery |
| `/ai-tryon` | `AiTryonPage` | AI virtual try-on feature |
| `/products` | `ProductsPage` | Product catalog listing |
| `/accessories` | `AccessoriesPage` | Accessories catalog |
| `/cart` | `CartPage` | Shopping cart |
| `/login` | `LoginPage` | Email/password + OAuth login |
| `/reset-password` | `ResetPasswordPage` | Password reset flow |
| `/auth/google/callback` | `AuthGoogleCallbackPage` | Google OAuth callback |
| `/auth/zalo/callback` | `AuthZaloCallbackPage` | Zalo OAuth callback |
| `/privacy-policy` | `PrivacyPolicyPage` | Privacy policy page |
| `/data-deletion` | `DataDeletionPage` | Data deletion request page |
| `/account/*` | Redirects to `HomePage` + AccountPage modal | Protected; redirects to `/login` if anonymous |

### Architecture Notes
- **Auth**: `AuthProvider` in `auth/AuthContext.tsx` manages session state (`loading` | `authenticated` | `anonymous`). Use `useAuth()` hook.
- **API client**: `api/client.ts` uses native `fetch` (not axios). Resolves regional API base URLs by hostname. Has `request<T>()` and `requestPaginated<T>()` helpers. All responses use `ApiEnvelope<T>` shape.
- **Account page**: Modal overlay in `App.tsx`, not routed page. Auth-protected.
- **Toast notifications**: `ToastProvider` wraps app; use `useToast()` hook.
- **Header/Footer**: Hidden on login and OAuth callback pages.
- **State management**: React Context only (auth + toast). No Redux/Zustand.
- **Component structure**: PascalCase folders with component TSX + CSS Module pair.