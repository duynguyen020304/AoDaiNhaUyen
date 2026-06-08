import {
  createContext,
  startTransition,
  useState,
  type ReactNode,
} from 'react';
import { useQueryClient } from '@tanstack/react-query';
import * as authApi from '../api/auth';
import { useCurrentUserQuery } from '../hooks/auth/useAuthQueries';
import { queryKeys } from '../lib/queryKeys';
import { clearPersistedQueryCache } from '../lib/queryPersist';
import type { AuthStatus, AuthUser } from '../types/auth';

interface AuthContextValue {
  status: AuthStatus;
  user: AuthUser | null;
  login: (email: string, password: string) => Promise<AuthUser>;
  completeGoogleLogin: (code: string) => Promise<AuthUser>;
  completeZaloLogin: (code: string, codeVerifier: string) => Promise<AuthUser>;
  logout: () => Promise<void>;
  refreshSession: () => Promise<AuthUser>;
  startGoogleLogin: () => void;
  startZaloLogin: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const currentUserQuery = useCurrentUserQuery();
  const [user, setUser] = useState<AuthUser | null>(null);
  const effectiveUser = user ?? currentUserQuery.data ?? null;
  const effectiveStatus: AuthStatus = currentUserQuery.isPending && !user
    ? 'loading'
    : effectiveUser
      ? 'authenticated'
      : 'anonymous';

  async function login(email: string, password: string) {
    const authenticatedUser = await authApi.login(email, password);
    queryClient.setQueryData(queryKeys.auth.me, authenticatedUser);
    startTransition(() => {
      setUser(authenticatedUser);
    });
    await queryClient.invalidateQueries({ queryKey: queryKeys.auth.me });
    return authenticatedUser;
  }

  async function completeGoogleLogin(code: string) {
    const authenticatedUser = await authApi.googleLogin(code);
    queryClient.setQueryData(queryKeys.auth.me, authenticatedUser);
    startTransition(() => {
      setUser(authenticatedUser);
    });
    await queryClient.invalidateQueries({ queryKey: queryKeys.auth.me });
    return authenticatedUser;
  }

  async function completeZaloLogin(code: string, codeVerifier: string) {
    const authenticatedUser = await authApi.zaloLogin(code, codeVerifier);
    queryClient.setQueryData(queryKeys.auth.me, authenticatedUser);
    startTransition(() => {
      setUser(authenticatedUser);
    });
    await queryClient.invalidateQueries({ queryKey: queryKeys.auth.me });
    return authenticatedUser;
  }

  async function logout() {
    try {
      await authApi.logout();
    } finally {
      queryClient.clear();
      await clearPersistedQueryCache();
      startTransition(() => {
        setUser(null);
      });
    }
  }

  async function refreshSession() {
    const refreshedUser = await authApi.refreshSession();
    queryClient.setQueryData(queryKeys.auth.me, refreshedUser);
    startTransition(() => {
      setUser(refreshedUser);
    });
    await queryClient.invalidateQueries({ queryKey: queryKeys.auth.me });
    return refreshedUser;
  }

  function startGoogleLogin() {
    const redirectUri = `${window.location.origin}/auth/google/callback`;
    window.location.assign(authApi.buildGoogleAuthorizeUrl(redirectUri));
  }

  function startZaloLogin() {
    void (async () => {
      const redirectUri = `${window.location.origin}/auth/callback/zalo`;
      const { state, codeChallenge } = await authApi.createZaloOAuthSession();
      window.location.assign(authApi.buildZaloAuthorizeUrl(redirectUri, codeChallenge, state));
    })();
  }

  const value: AuthContextValue = {
    status: effectiveStatus,
    user: effectiveUser,
    login,
    completeGoogleLogin,
    completeZaloLogin,
    logout,
    refreshSession,
    startGoogleLogin,
    startZaloLogin,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export { AuthContext };
