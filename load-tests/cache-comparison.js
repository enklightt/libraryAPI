import http from 'k6/http';
import { check } from 'k6';

export const options = {
  scenarios: {
    cold_cache: {
      executor: 'constant-vus',
      vus: 5,
      duration: '30s',
      exec: 'coldCacheTest',
    },
    warm_cache: {
      executor: 'constant-vus',
      vus: 5,
      duration: '30s',
      startTime: '35s',
      exec: 'warmCacheTest',
    },
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5043';

export function coldCacheTest() {
  // Bust the cache with random search parameter
  const uniqueSearch = `rand_${Math.random()}_${Date.now()}`;
  const res = http.get(`${BASE_URL}/api/v1/books?page=1&pageSize=10&search=${uniqueSearch}`);
  check(res, {
    'status 200': (r) => r.status === 200,
    'x-cache is MISS': (r) => r.headers['X-Cache'] === 'MISS',
  });
}

export function warmCacheTest() {
  // Repeatedly request same key to hit memory cache
  const res = http.get(`${BASE_URL}/api/v1/books?page=1&pageSize=10`);
  check(res, {
    'status 200': (r) => r.status === 200,
  });
}
