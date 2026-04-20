import { request } from './client';
import type { AuthUser } from '../types/auth';

const GOOGLE_CLIENT_ID = import.meta.env.PUBLIC_GOOGLE_CLIENT_ID || '';
const ZALO_APP_ID = import.meta.env.PUBLIC_ZALO_APP_ID || '';
const ZALO_OAUTH_STATE_KEY = 'aodai.zalo.oauth.state';
const ZALO_OAUTH_CODE_VERIFIER_KEY = 'aodai.zalo.oauth.code_verifier';

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

export function zaloLogin(code: string, codeVerifier: string): Promise<AuthUser> {
  return request<AuthUser>('/api/auth/zalo', {
    method: 'POST',
    body: JSON.stringify({ code, codeVerifier }),
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

export function buildZaloAuthorizeUrl(redirectUri: string, codeChallenge: string, state: string): string {
  if (!ZALO_APP_ID) {
    throw new Error('PUBLIC_ZALO_APP_ID is not configured.');
  }

  const url = new URL('https://oauth.zaloapp.com/v4/permission');
  url.searchParams.set('app_id', ZALO_APP_ID);
  url.searchParams.set('redirect_uri', redirectUri);
  url.searchParams.set('code_challenge', codeChallenge);
  url.searchParams.set('state', state);

  return url.toString();
}

export async function createZaloOAuthSession() {
  const state = generateRandomBase64Url(24);
  const codeVerifier = generateRandomBase64Url(64);
  const codeChallenge = await buildPkceCodeChallenge(codeVerifier);

  window.sessionStorage.setItem(ZALO_OAUTH_STATE_KEY, state);
  window.sessionStorage.setItem(ZALO_OAUTH_CODE_VERIFIER_KEY, codeVerifier);

  return { state, codeChallenge };
}

export function consumeZaloOAuthSession(returnedState: string | null): string | null {
  const expectedState = window.sessionStorage.getItem(ZALO_OAUTH_STATE_KEY);
  const codeVerifier = window.sessionStorage.getItem(ZALO_OAUTH_CODE_VERIFIER_KEY);
  window.sessionStorage.removeItem(ZALO_OAUTH_STATE_KEY);
  window.sessionStorage.removeItem(ZALO_OAUTH_CODE_VERIFIER_KEY);

  if (!returnedState || !expectedState || returnedState !== expectedState || !codeVerifier) {
    return null;
  }

  return codeVerifier;
}

function generateRandomBase64Url(byteLength: number): string {
  const bytes = new Uint8Array(byteLength);
  window.crypto.getRandomValues(bytes);
  return base64UrlEncode(bytes);
}

async function buildPkceCodeChallenge(codeVerifier: string): Promise<string> {
  const data = new TextEncoder().encode(codeVerifier);
  const digest = await window.crypto.subtle.digest('SHA-256', data);
  return base64UrlEncode(new Uint8Array(digest));
}

function base64UrlEncode(bytes: Uint8Array): string {
  let binary = '';
  bytes.forEach((byte) => {
    binary += String.fromCharCode(byte);
  });

  return window.btoa(binary)
    .replace(/\+/g, '-')
    .replace(/\//g, '_')
    .replace(/=+$/g, '');
}
