import { request } from './client';
import type { AuthUser } from '../types/auth';

const GOOGLE_CLIENT_ID = import.meta.env.PUBLIC_GOOGLE_CLIENT_ID || '';
const FACEBOOK_CLIENT_ID = import.meta.env.PUBLIC_FACEBOOK_CLIENT_ID || '';

export interface RegisterPayload {
  fullName: string;
  email: string;
  phone?: string;
  password: string;
  confirmPassword: string;
}

export interface ResetPasswordPayload {
  userId: number;
  token: string;
  newPassword: string;
}

interface FlagResponse {
  registered?: boolean;
  sent?: boolean;
  reset?: boolean;
}

export function login(email: string, password: string): Promise<AuthUser> {
  return request<AuthUser>('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify({ email, password }),
  });
}

export function register(payload: RegisterPayload): Promise<FlagResponse> {
  return request<FlagResponse>('/api/auth/register', {
    method: 'POST',
    body: JSON.stringify(payload),
  });
}

export function googleLogin(code: string): Promise<AuthUser> {
  return request<AuthUser>('/api/auth/google', {
    method: 'POST',
    body: JSON.stringify({ code }),
  });
}

export function facebookLogin(code: string): Promise<AuthUser> {
  return request<AuthUser>('/api/auth/facebook', {
    method: 'POST',
    body: JSON.stringify({ code }),
  });
}

export function refreshSession(): Promise<AuthUser> {
  return request<AuthUser>('/api/auth/refresh', {
    method: 'POST',
    body: JSON.stringify({}),
  });
}

export function logout(): Promise<{ loggedOut: boolean }> {
  return request<{ loggedOut: boolean }>('/api/auth/logout', {
    method: 'POST',
    body: JSON.stringify({}),
  });
}

export function getCurrentUser(): Promise<AuthUser> {
  return request<AuthUser>('/api/auth/me');
}

export function forgotPassword(email: string): Promise<FlagResponse> {
  return request<FlagResponse>('/api/auth/forgot-password', {
    method: 'POST',
    body: JSON.stringify({ email }),
  });
}

export function resetPassword(payload: ResetPasswordPayload): Promise<FlagResponse> {
  return request<FlagResponse>('/api/auth/reset-password', {
    method: 'POST',
    body: JSON.stringify(payload),
  });
}

export function buildGoogleAuthorizeUrl(redirectUri: string): string {
  if (!GOOGLE_CLIENT_ID) {
    throw new Error('PUBLIC_GOOGLE_CLIENT_ID is not configured.');
  }

  const url = new URL('https://accounts.google.com/o/oauth2/v2/auth');
  url.searchParams.set('client_id', GOOGLE_CLIENT_ID);
  url.searchParams.set('redirect_uri', redirectUri);
  url.searchParams.set('response_type', 'code');
  url.searchParams.set('scope', 'openid email profile');
  url.searchParams.set('access_type', 'online');
  url.searchParams.set('prompt', 'select_account');

  return url.toString();
}

export function buildFacebookAuthorizeUrl(redirectUri: string): string {
  if (!FACEBOOK_CLIENT_ID) {
    throw new Error('PUBLIC_FACEBOOK_CLIENT_ID is not configured.');
  }

  const url = new URL('https://www.facebook.com/v21.0/dialog/oauth');
  url.searchParams.set('client_id', FACEBOOK_CLIENT_ID);
  url.searchParams.set('redirect_uri', redirectUri);
  url.searchParams.set('response_type', 'code');
  url.searchParams.set('scope', 'public_profile,email');

  return url.toString();
}
