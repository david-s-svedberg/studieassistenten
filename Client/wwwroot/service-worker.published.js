// Service Worker for Studieassistenten PWA
// Enhanced with API caching and offline support

self.importScripts('./service-worker-assets.js');
self.addEventListener('install', event => event.waitUntil(onInstall(event)));
self.addEventListener('activate', event => event.waitUntil(onActivate(event)));
self.addEventListener('fetch', event => event.respondWith(onFetch(event)));

const cacheNamePrefix = 'offline-cache-';
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;
const apiCacheName = 'api-cache-v1';
const offlineAssetsInclude = [ /\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.blat$/, /\.dat$/ ];
const offlineAssetsExclude = [ /^service-worker\.js$/ ];

// API endpoints that can be cached
const apiCachePatterns = [
    /\/api\/documents\/\d+$/,  // Document details
    /\/api\/ContentGeneration\/document\/\d+$/,  // Generated content list
    /\/api\/ContentGeneration\/\d+$/  // Individual content
];

async function onInstall(event) {
    console.info('Service worker: Install');

    // Fetch and cache all matching items from the assets manifest
    const assetsRequests = self.assetsManifest.assets
        .filter(asset => offlineAssetsInclude.some(pattern => pattern.test(asset.url)))
        .filter(asset => !offlineAssetsExclude.some(pattern => pattern.test(asset.url)))
        .map(asset => new Request(asset.url, { integrity: asset.hash, cache: 'no-cache' }));
    await caches.open(cacheName).then(cache => cache.addAll(assetsRequests));
}

async function onActivate(event) {
    console.info('Service worker: Activate');

    // Delete unused caches
    const cacheKeys = await caches.keys();
    await Promise.all(cacheKeys
        .filter(key => key.startsWith(cacheNamePrefix) && key !== cacheName && key !== apiCacheName)
        .map(key => caches.delete(key)));
}

async function onFetch(event) {
    let cachedResponse = null;
    const url = new URL(event.request.url);

    // Handle API requests with network-first strategy
    if (event.request.method === 'GET' && url.pathname.startsWith('/api/')) {
        const shouldCache = apiCachePatterns.some(pattern => pattern.test(url.pathname));
        
        if (shouldCache) {
            try {
                // Try network first
                const response = await fetch(event.request);
                if (response.ok) {
                    const cache = await caches.open(apiCacheName);
                    cache.put(event.request, response.clone());
                }
                return response;
            } catch (error) {
                // Network failed, try cache
                const cache = await caches.open(apiCacheName);
                cachedResponse = await cache.match(event.request);
                if (cachedResponse) {
                    console.log('Serving from cache (offline):', url.pathname);
                    return cachedResponse;
                }
                // Return offline response
                return new Response(JSON.stringify({ error: 'Offline', message: 'Data not available offline' }), {
                    status: 503,
                    headers: { 'Content-Type': 'application/json' }
                });
            }
        }
    }

    // Handle static assets and navigation with cache-first strategy
    if (event.request.method === 'GET') {
        const shouldServeIndexHtml = event.request.mode === 'navigate';
        const request = shouldServeIndexHtml ? 'index.html' : event.request;
        const cache = await caches.open(cacheName);
        cachedResponse = await cache.match(request);
    }

    return cachedResponse || fetch(event.request);
}
