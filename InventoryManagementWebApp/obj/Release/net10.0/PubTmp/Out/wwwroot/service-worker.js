const CACHE_NAME = 'wine-inventory-v1';
const urlsToCache = [
  '/',
  '/css/site.css',
  '/js/site.js',
  '/lib/bootstrap/dist/css/bootstrap.min.css',
  '/lib/bootstrap/dist/js/bootstrap.bundle.min.js',
  '/lib/jquery/dist/jquery.min.js',
  '/icons/icon-128.png',
  '/icons/icon-256.png'
];

// Install event
self.addEventListener('install', (event) => {
    console.log('✅ Service Worker Installing...');
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then((cache) => {
                console.log('✅ Service Worker Cache Opened');
                return cache.addAll(urlsToCache);
            })
            .catch((error) => {
                console.error('❌ Service Worker Install Failed:', error);
            })
    );
});

// Activate event
self.addEventListener('activate', (event) => {
    console.log('✅ Service Worker Activated');
    event.waitUntil(
        caches.keys().then((cacheNames) => {
            return Promise.all(
                cacheNames.map((cacheName) => {
                    if (cacheName !== CACHE_NAME) {
                        console.log('🗑️ Deleting old cache:', cacheName);
                        return caches.delete(cacheName);
                    }
                })
            );
        })
    );
});

// Enhanced fetch event with proper redirect handling
self.addEventListener('fetch', (event) => {
    // Skip service worker for development and authentication requests
    if (event.request.url.includes('/Account/') || 
        event.request.url.includes('localhost') && event.request.mode === 'navigate') {
        console.log('🔄 Bypassing Service Worker for:', event.request.url);
        return; // Let the request go through normally
    }

    event.respondWith(
        caches.match(event.request)
            .then((response) => {
                if (response) {
                    console.log('📦 Serving from cache:', event.request.url);
                    return response;
                }

                // Clone the request because it can only be consumed once
                const fetchRequest = event.request.clone();

                return fetch(fetchRequest, {
                    redirect: 'follow', // Explicitly allow redirects
                    credentials: 'same-origin' // Include cookies for authentication
                })
                .then((response) => {
                    // Check if valid response
                    if (!response || response.status !== 200 || response.type !== 'basic') {
                        return response;
                    }

                    // Clone the response because it can only be consumed once
                    const responseToCache = response.clone();

                    // Only cache successful GET requests for static resources
                    if (event.request.method === 'GET' && 
                        (event.request.url.includes('.css') || 
                         event.request.url.includes('.js') || 
                         event.request.url.includes('.png') || 
                         event.request.url.includes('.jpg') || 
                         event.request.url.includes('.ico'))) {
                        
                        caches.open(CACHE_NAME)
                            .then((cache) => {
                                cache.put(event.request, responseToCache);
                            });
                    }

                    return response;
                })
                .catch((error) => {
                    console.error('🚫 Fetch failed:', error);
                    
                    // Fallback for navigation requests
                    if (event.request.mode === 'navigate') {
                        return caches.match('/').then((cachedResponse) => {
                            return cachedResponse || new Response('Offline', { 
                                status: 503, 
                                statusText: 'Service Unavailable' 
                            });
                        });
                    }
                    
                    throw error;
                });
            })
    );
});

// Handle messages from main thread
self.addEventListener('message', (event) => {
    if (event.data && event.data.type === 'SKIP_WAITING') {
        self.skipWaiting();
    }
});

console.log('✅ Service Worker Script Loaded');
