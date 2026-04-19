<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-04-19 | Updated: 2026-04-19 -->

# src

## Purpose
Application source code for the Ao Dai Nha Uyen SPA. Contains all React components, pages, API modules, authentication logic, type definitions, utility functions, and global styles.

## Key Files
| File | Description |
|------|-------------|
| `main.tsx` | React entry point: renders `<BrowserRouter>` with `<AuthProvider>` and `<ToastProvider>` wrapping `<App>` |
| `App.tsx` | Root component: defines all routes, renders Header/Footer conditionally, manages AccountPage as modal overlay |
| `vite-env.d.ts` | Vite client type declarations |

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `api/` | API client modules (fetch-based) for backend communication (see `api/AGENTS.md`) |
| `auth/` | Authentication context, protected route guard, and useAuth hook (see `auth/AGENTS.md`) |
| `components/` | Reusable UI components organized in PascalCase folders (see `components/AGENTS.md`) |
| `pages/` | Route-level page components (see `pages/AGENTS.md`) |
| `styles/` | Global CSS: variables (design tokens), reset, typography, texture, transitions (see `styles/AGENTS.md`) |
| `types/` | TypeScript type definitions per domain (see `types/AGENTS.md`) |
| `utils/` | Utility functions: motion variants, image conversion (see `utils/AGENTS.md`) |

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
| `/auth/facebook/callback` | `AuthFacebookCallbackPage` | Facebook OAuth callback |
| `/privacy-policy` | `PrivacyPolicyPage` | Privacy policy page |
| `/data-deletion` | `DataDeletionPage` | Data deletion request page |
| `/account/*` | Redirects to `HomePage` + AccountPage modal | Protected; redirects to `/login` if anonymous |

### Architecture Notes
- **Auth**: `AuthProvider` in `auth/AuthContext.tsx` manages session state (`loading` | `authenticated` | `anonymous`). Uses `useAuth()` hook for access.
- **API client**: `api/client.ts` uses native `fetch` (not axios). Resolves regional API base URLs based on hostname. Includes `request<T>()` and `requestPaginated<T>()` helpers. All responses follow `ApiEnvelope<T>` shape.
- **Account page**: Rendered as a modal overlay in `App.tsx`, not as a routed page. Protected by auth status check.
- **Toast notifications**: `ToastProvider` wraps the app; use `useToast()` hook to show notifications.
- **Header/Footer**: Hidden on login and OAuth callback pages.
- **State management**: React Context only (auth + toast). No Redux/Zustand.
- **Component structure**: PascalCase folders with component TSX + CSS Module pair.
