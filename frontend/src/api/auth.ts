import { request } from './client';
import type { AuthUser } from '../types/auth';

const GOOGLE_CLIENT_ID = import.meta.env.PUBLIC_GOOGLE_CLIENT_ID || '';

export function login(email: string, password: string): Promise<AuthUser> {
  return request<AuthUser>('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify({ email, password }),
  });
}

export function googleLogin(code: string): Promise<AuthUser> {
  return request<AuthUser>('/api/auth/google', {
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
