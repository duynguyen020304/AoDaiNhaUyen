export interface AuthUser {
  id: number;
  fullName: string;
  email: string | null;
  avatarUrl: string | null;
  roles: string[];
}

export type AuthStatus = 'loading' | 'authenticated' | 'anonymous';
