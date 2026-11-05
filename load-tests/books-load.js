import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '30s', target: 50 },
    { duration: '60s', target: 50 },
    { duration: '30s', target: 100 },
    { duration: '30s', target: 100 },
    { duration: '30s', target: 0 },
  ],
  thresholds: {
    http_req_failed: ['rate<0.02'],
    http_req_duration: ['p(95)<500'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5043';

export default function () {
  const res = http.get(`${BASE_URL}/api/v1/books?page=1&pageSize=20`);
  check(res, {
    'status is 200': (r) => r.status === 200,
  });
  sleep(0.1);
}
