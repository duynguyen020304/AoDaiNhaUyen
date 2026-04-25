const SW_VERSION = 'v2';
const MINUTE = 60 * 1000;
const DAY = 24 * 60 * MINUTE;

const STATIC_TTL = 30 * DAY;
const API_TTL = 2 * MINUTE;
const API_TIMEOUT = 2500;
const IMAGE_TTL = 7 * DAY;
const MAX_IMAGE_CACHE_ENTRIES = 120;

const CACHE_NAMES = {
  static: `app-static-${SW_VERSION}`,
  api: `app-api-${SW_VERSION}`,
  html: `app-html-${SW_VERSION}`,
  images: `app-images-${SW_VERSION}`,
  meta: `app-meta-${SW_VERSION}`,
};

let apiBaseUrl = self.location.origin;
let currentContentVersion = null;

self.addEventListener('install', (event) => {
  event.waitUntil(
    caches.open(CACHE_NAMES.static).then(() => self.skipWaiting()),
  );
});

self.addEventListener('activate', (event) => {
  event.waitUntil(Promise.all([
    cleanupOldCaches(),
    self.clients.claim(),
  ]));
});

self.addEventListener('message', (event) => {
  const message = event.data;
  if (!message || typeof message !== 'object') {
    return;
  }

  if (message.type === 'SET_API_BASE_URL' && typeof message.apiBaseUrl === 'string') {
    apiBaseUrl = message.apiBaseUrl.replace(/\/$/, '');
    return;
  }

  if (message.type === 'INVALIDATE_CACHE' && typeof message.version === 'string') {
    event.waitUntil(handleCacheVersionUpdate(message.version));
    return;
  }

  if (message.type === 'LANGUAGE_CHANGED') {
    event.waitUntil(invalidateAllRuntimeCaches());
  }
});

self.addEventListener('fetch', (event) => {
  const request = event.request;
  const url = new URL(request.url);

  if (request.method !== 'GET') {
    return;
  }

  if (isStaticAsset(request, url)) {
    event.respondWith(cacheFirst(request, CACHE_NAMES.static, STATIC_TTL));
    return;
  }

  if (isApiRequest(request, url)) {
    event.respondWith(networkFirstWithTimeout(request, CACHE_NAMES.api, API_TTL));
    return;
  }

  if (request.destination === 'image') {
    event.respondWith(staleWhileRevalidate(request, CACHE_NAMES.images, IMAGE_TTL));
    return;
  }

  if (request.mode === 'navigate') {
    event.respondWith(networkOnlyWithOfflineFallback(request));
  }
});

async function cleanupOldCaches() {
  const expected = new Set(Object.values(CACHE_NAMES));
  const names = await caches.keys();

  await Promise.all(
    names
      .filter((name) => name.startsWith('app-') && !expected.has(name))
      .map((name) => caches.delete(name)),
  );
}

async function cacheFirst(request, cacheName, fallbackMaxAge) {
  const cache = await caches.open(cacheName);
  const cachedResponse = await cache.match(request);
  const metadata = await readMetadata(request);

  if (cachedResponse && isFresh(metadata)) {
    return cachedResponse;
  }

  try {
    const response = await fetch(request);
    await cacheResponse(cache, request, response, fallbackMaxAge);
    return response;
  } catch {
    if (cachedResponse) {
      return cachedResponse;
    }

    return unavailableResponse('Asset unavailable offline', 'text/plain; charset=utf-8');
  }
}

async function networkFirstWithTimeout(request, cacheName, fallbackMaxAge) {
  const cache = await caches.open(cacheName);
  const cachedResponse = await cache.match(request);
  const metadata = await readMetadata(request);

  try {
    const response = await fetchWithTimeout(buildConditionalRequest(request, metadata), API_TIMEOUT);

    if (response.status === 304 && cachedResponse) {
      await writeMetadata(request, {
        ...metadata,
        cachedAt: Date.now(),
        maxAge: metadata?.maxAge ?? fallbackMaxAge,
        cacheVersion: currentContentVersion ?? metadata?.cacheVersion ?? SW_VERSION,
      });
      return cachedResponse;
    }

    await cacheResponse(cache, request, response, fallbackMaxAge);
    return response;
  } catch {
    if (cachedResponse && isUsableWhenOffline(metadata)) {
      return cachedResponse;
    }

    return new Response(
      JSON.stringify({
        success: false,
        message: 'Service temporarily unavailable',
        data: null,
        errors: ['offline'],
      }),
      {
        status: 503,
        headers: { 'Content-Type': 'application/json' },
      },
    );
  }
}

async function staleWhileRevalidate(request, cacheName, fallbackMaxAge) {
  const cache = await caches.open(cacheName);
  const cachedResponse = await cache.match(request);

  const update = fetch(request)
    .then(async (response) => {
      await cacheImageResponse(cache, request, response, fallbackMaxAge);
      await trimImageCache();
      return response;
    })
    .catch(() => undefined);

  if (cachedResponse) {
    return cachedResponse;
  }

  const response = await update;
  if (response) {
    return response;
  }

  return unavailableResponse('Image unavailable offline', 'text/plain; charset=utf-8');
}

async function networkOnlyWithOfflineFallback(request) {
  const cache = await caches.open(CACHE_NAMES.html);

  try {
    const networkRequest = new Request(request, { cache: 'no-store' });
    const response = await fetch(networkRequest);

    if (response.ok && isCacheableResponse(response)) {
      await cache.put(request, response.clone());
      await writeMetadata(request, {
        cachedAt: Date.now(),
        maxAge: API_TTL,
        etag: response.headers.get('ETag') ?? undefined,
        cacheVersion: currentContentVersion ?? SW_VERSION,
      });
    }

    return response;
  } catch {
    const cachedResponse = await cache.match(request);
    if (cachedResponse) {
      return cachedResponse;
    }

    return unavailableResponse('You are offline. Please reconnect and try again.', 'text/plain; charset=utf-8');
  }
}

async function fetchWithTimeout(request, timeoutMs) {
  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), timeoutMs);

  try {
    return await fetch(new Request(request, { signal: controller.signal }));
  } finally {
    clearTimeout(timeoutId);
  }
}

function buildConditionalRequest(request, metadata) {
  if (!metadata?.etag) {
    return request;
  }

  const headers = new Headers(request.headers);
  headers.set('If-None-Match', metadata.etag);
  return new Request(request, { headers });
}

async function cacheResponse(cache, request, response, fallbackMaxAge) {
  if (!response || response.status === 304 || !isCacheableResponse(response)) {
    return;
  }

  await cache.put(request, response.clone());
  await writeMetadata(request, {
    cachedAt: Date.now(),
    maxAge: getMaxAge(response, fallbackMaxAge),
    etag: response.headers.get('ETag') ?? undefined,
    cacheVersion: currentContentVersion ?? SW_VERSION,
  });
}

async function cacheImageResponse(cache, request, response, fallbackMaxAge) {
  if (!response || response.status === 304 || !isCacheableImageResponse(response)) {
    return;
  }

  await cache.put(request, response.clone());
  await writeMetadata(request, {
    cachedAt: Date.now(),
    maxAge: response.type === 'opaque' ? fallbackMaxAge : getMaxAge(response, fallbackMaxAge),
    etag: response.type === 'opaque' ? undefined : response.headers.get('ETag') ?? undefined,
    cacheVersion: currentContentVersion ?? SW_VERSION,
  });
}

function isCacheableResponse(response) {
  if (!response.ok) {
    return false;
  }

  const cacheControl = response.headers.get('Cache-Control')?.toLowerCase() ?? '';
  return !cacheControl.includes('no-store') && !cacheControl.includes('private');
}

function isCacheableImageResponse(response) {
  if (response.type === 'opaque') {
    return true;
  }

  return isCacheableResponse(response);
}

function getMaxAge(response, fallback) {
  const cacheControl = response.headers.get('Cache-Control');
  const match = cacheControl?.match(/max-age=(\d+)/);

  if (!match) {
    return fallback;
  }

  return Number(match[1]) * 1000;
}

async function readMetadata(request) {
  const metaCache = await caches.open(CACHE_NAMES.meta);
  const response = await metaCache.match(metaKey(request));

  if (!response) {
    return null;
  }

  try {
    return await response.json();
  } catch {
    await metaCache.delete(metaKey(request));
    return null;
  }
}

async function writeMetadata(request, metadata) {
  const metaCache = await caches.open(CACHE_NAMES.meta);
  await metaCache.put(
    metaKey(request),
    new Response(JSON.stringify(metadata), {
      headers: { 'Content-Type': 'application/json' },
    }),
  );
}

function metaKey(request) {
  return new Request(`${request.url}__meta`);
}

function isFresh(metadata) {
  return Boolean(metadata && Date.now() - metadata.cachedAt <= metadata.maxAge);
}

function isUsableWhenOffline(metadata) {
  if (!metadata) {
    return true;
  }

  const staleWindow = Math.max(metadata.maxAge, API_TTL) * 10;
  return Date.now() - metadata.cachedAt <= staleWindow;
}

function unavailableResponse(message, contentType) {
  return new Response(message, {
    status: 503,
    headers: { 'Content-Type': contentType },
  });
}

function isStaticAsset(request, url) {
  if (url.origin !== self.location.origin) {
    return false;
  }

  if (url.pathname === '/sw.js') {
    return false;
  }

  if (request.destination === 'script' || request.destination === 'style' || request.destination === 'font') {
    return true;
  }

  return /\.(?:js|css|woff2?|ttf|otf|eot|svg|ico|webmanifest)$/i.test(url.pathname);
}

function isApiRequest(request, url) {
  if (request.method !== 'GET' || request.headers.has('Authorization')) {
    return false;
  }

  if (!isConfiguredApiOrigin(url)) {
    return false;
  }

  if (isPrivateApiPath(url.pathname)) {
    return false;
  }

  return (
    url.pathname.startsWith('/api/v1/products') ||
    url.pathname.startsWith('/api/v1/categories') ||
    url.pathname === '/api/v1/ai-tryon/catalog' ||
    url.pathname === '/api/cache/version'
  );
}

function isConfiguredApiOrigin(url) {
  try {
    return url.origin === new URL(apiBaseUrl).origin || url.origin === self.location.origin;
  } catch {
    return url.origin === self.location.origin;
  }
}

function isPrivateApiPath(pathname) {
  return (
    pathname.startsWith('/api/auth') ||
    pathname.startsWith('/api/admin') ||
    pathname.startsWith('/api/users/me') ||
    pathname.startsWith('/api/v1/chat')
  );
}

async function handleCacheVersionUpdate(version) {
  if (currentContentVersion === version) {
    return;
  }

  currentContentVersion = version;
  await invalidateAllRuntimeCaches();
  await broadcast('CACHE_VERSION_UPDATE', { version });
}

async function invalidateAllRuntimeCaches() {
  await Promise.all([
    caches.delete(CACHE_NAMES.api),
    caches.delete(CACHE_NAMES.html),
  ]);

  await Promise.all([
    caches.open(CACHE_NAMES.api),
    caches.open(CACHE_NAMES.html),
    clearRuntimeMetadata(),
  ]);
}

async function clearRuntimeMetadata() {
  await caches.delete(CACHE_NAMES.meta);
  await caches.open(CACHE_NAMES.meta);
}

async function broadcast(type, payload = {}) {
  const clients = await self.clients.matchAll();
  await Promise.all(clients.map((client) => client.postMessage({ type, ...payload })));
}

async function trimImageCache() {
  const cache = await caches.open(CACHE_NAMES.images);
  const requests = await cache.keys();

  if (requests.length <= MAX_IMAGE_CACHE_ENTRIES) {
    return;
  }

  const withMetadata = await Promise.all(
    requests.map(async (request) => ({
      request,
      metadata: await readMetadata(request),
    })),
  );

  withMetadata.sort((a, b) => (a.metadata?.cachedAt ?? 0) - (b.metadata?.cachedAt ?? 0));

  await Promise.all(
    withMetadata
      .slice(0, requests.length - MAX_IMAGE_CACHE_ENTRIES)
      .map(async ({ request }) => {
        await cache.delete(request);
        const metaCache = await caches.open(CACHE_NAMES.meta);
        await metaCache.delete(metaKey(request));
      }),
  );
}
