export interface AuthUser {
  id: string;
  fullName: string;
  email: string | null;
  avatarUrl: string | null;
  roles: string[];
}

export type AuthStatus = 'loading' | 'authenticated' | 'anonymous';
