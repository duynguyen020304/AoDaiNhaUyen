import { createContext, useContext } from 'react';

export type AuthTab = 'login' | 'register';

interface AuthModalOptions {
  from?: string;
  tab?: AuthTab;
}

interface AuthModalContextValue {
  openAuthModal: (options?: AuthModalOptions) => void;
}

const AuthModalContext = createContext<AuthModalContextValue | null>(null);

export const AuthModalProvider = AuthModalContext.Provider;

export function useAuthModal(): AuthModalContextValue {
  const value = useContext(AuthModalContext);

  if (!value) {
    throw new Error('useAuthModal must be used within AuthModalProvider');
  }

  return value;
}
