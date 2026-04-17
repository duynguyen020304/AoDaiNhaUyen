import {
  createContext,
  startTransition,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react';
import * as authApi from '../api/auth';
import type { AuthStatus, AuthUser } from '../types/auth';

interface AuthContextValue {
  status: AuthStatus;
  user: AuthUser | null;
  login: (email: string, password: string) => Promise<AuthUser>;
  completeGoogleLogin: (code: string) => Promise<AuthUser>;
  logout: () => Promise<void>;
  refreshSession: () => Promise<AuthUser>;
  startGoogleLogin: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [status, setStatus] = useState<AuthStatus>('loading');
  const [user, setUser] = useState<AuthUser | null>(null);

  useEffect(() => {
    void bootstrapSession();
  }, []);

  async function bootstrapSession() {
    try {
      const currentUser = await authApi.getCurrentUser();
      startTransition(() => {
        setUser(currentUser);
        setStatus('authenticated');
      });
      return;
    } catch {
      try {
        const refreshedUser = await authApi.refreshSession();
        startTransition(() => {
          setUser(refreshedUser);
          setStatus('authenticated');
        });
        return;
      } catch {
        startTransition(() => {
          setUser(null);
          setStatus('anonymous');
        });
      }
    }
  }

  async function login(email: string, password: string) {
    const authenticatedUser = await authApi.login(email, password);
    startTransition(() => {
      setUser(authenticatedUser);
      setStatus('authenticated');
    });
    return authenticatedUser;
  }

  async function completeGoogleLogin(code: string) {
    const authenticatedUser = await authApi.googleLogin(code);
    startTransition(() => {
      setUser(authenticatedUser);
      setStatus('authenticated');
    });
    return authenticatedUser;
  }

  async function logout() {
    try {
      await authApi.logout();
    } finally {
      startTransition(() => {
        setUser(null);
        setStatus('anonymous');
      });
    }
  }

  async function refreshSession() {
    const refreshedUser = await authApi.refreshSession();
    startTransition(() => {
      setUser(refreshedUser);
      setStatus('authenticated');
    });
    return refreshedUser;
  }

  function startGoogleLogin() {
    const redirectUri = `${window.location.origin}/auth/google/callback`;
    window.location.assign(authApi.buildGoogleAuthorizeUrl(redirectUri));
  }

  const value = useMemo<AuthContextValue>(() => ({
    status,
    user,
    login,
    completeGoogleLogin,
    logout,
    refreshSession,
    startGoogleLogin,
  }), [status, user]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export { AuthContext };
