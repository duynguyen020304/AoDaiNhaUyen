<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-04-19 | Updated: 2026-04-19 -->

# auth

## Purpose
Authentication layer using React Context. Manages user session state, provides login/logout/OAuth methods, and offers a route guard for protected pages.

## Key Files
| File | Description |
|------|-------------|
| `AuthContext.tsx` | `AuthProvider` component and `AuthContext`. Manages auth status (`loading` \| `authenticated` \| `anonymous`), bootstraps session on mount by calling `getCurrentUser()` then `refreshSession()` as fallback. Provides: `login()`, `logout()`, `completeGoogleLogin()`, `completeFacebookLogin()`, `startGoogleLogin()`, `startFacebookLogin()`, `refreshSession()`. Wraps the entire app in `main.tsx` |
| `ProtectedRoute.tsx` | Route guard component: redirects anonymous users to `/login` with `state.from` for return navigation. Returns `null` while loading. Renders `<Outlet />` for authenticated users |
| `useAuth.ts` | Custom hook: `useAuth()` returns the `AuthContextValue` (status, user, login, logout, OAuth methods). Throws if used outside `AuthProvider` |

## For AI Agents
### Auth Status Flow
1. App mounts -> `AuthProvider` calls `bootstrapSession()`
2. Tries `getCurrentUser()` (cookie-based session check)
3. If that fails, tries `refreshSession()` (refresh token rotation)
4. If both fail, status becomes `anonymous`
5. Status transitions use `startTransition()` for concurrent React features

### OAuth Flow
- `startGoogleLogin()` / `startFacebookLogin()` redirect the browser to the OAuth provider
- On callback, `AuthGoogleCallbackPage` / `AuthFacebookCallbackPage` extract the `code` from URL params
- They call `completeGoogleLogin(code)` / `completeFacebookLogin(code)` which exchanges the code for a session

### Usage Pattern
```tsx
const { status, user, login, logout } = useAuth();
if (status === 'loading') return <LoadingSpinner />;
if (status === 'anonymous') return <LoginPage />;
// status === 'authenticated' -> render protected content
```

### Types
- `AuthUser`: `{ id, fullName, email, avatarUrl, roles }`
- `AuthStatus`: `'loading' | 'authenticated' | 'anonymous'`
- Defined in `types/auth.ts`
