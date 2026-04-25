import { API_BASE_URL } from '../api/client';

type CacheVersionResponse = {
  success: boolean;
  data?: {
    version?: string;
  };
};

const CACHE_VERSION_KEY = 'app-cache-version';
const CACHE_VERSION_POLL_INTERVAL = 5 * 60 * 1000;

let pollIntervalId: number | undefined;

export function registerServiceWorker(): void {
  if (!('serviceWorker' in navigator)) {
    return;
  }

  window.addEventListener('load', async () => {
    try {
      const registration = await navigator.serviceWorker.register('/sw.js', {
        updateViaCache: 'none',
      });

      await registration.update();
      sendApiBaseUrlToServiceWorker();
      startCacheVersionPolling();
    } catch {
      return;
    }
  });

  navigator.serviceWorker.addEventListener('controllerchange', () => {
    sendApiBaseUrlToServiceWorker();
  });

  navigator.serviceWorker.addEventListener('message', (event) => {
    if (event.data?.type === 'CACHE_VERSION_UPDATE' && typeof event.data.version === 'string') {
      localStorage.setItem(CACHE_VERSION_KEY, event.data.version);
    }
  });
}

export async function checkCacheVersion(): Promise<void> {
  if (!('serviceWorker' in navigator)) {
    return;
  }

  let response: Response;
  try {
    response = await fetch(`${API_BASE_URL}/api/cache/version`, {
      cache: 'no-store',
      credentials: 'omit',
    });
  } catch {
    return;
  }

  if (!response.ok) {
    return;
  }

  let payload: CacheVersionResponse;
  try {
    payload = await response.json() as CacheVersionResponse;
  } catch {
    return;
  }

  const version = payload.data?.version;
  if (!version) {
    return;
  }

  const current = localStorage.getItem(CACHE_VERSION_KEY);
  if (current && current !== version) {
    postMessageToServiceWorker({
      type: 'INVALIDATE_CACHE',
      version,
    });
  }

  localStorage.setItem(CACHE_VERSION_KEY, version);
}

export function startCacheVersionPolling(): void {
  if (pollIntervalId !== undefined) {
    return;
  }

  void checkCacheVersion();
  pollIntervalId = window.setInterval(() => {
    void checkCacheVersion();
  }, CACHE_VERSION_POLL_INTERVAL);
}

function sendApiBaseUrlToServiceWorker(): void {
  postMessageToServiceWorker({
    type: 'SET_API_BASE_URL',
    apiBaseUrl: API_BASE_URL,
  });
}

function postMessageToServiceWorker(message: unknown): void {
  if (navigator.serviceWorker.controller) {
    navigator.serviceWorker.controller.postMessage(message);
  }
}
