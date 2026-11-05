import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  vus: 1,
  duration: '30s',
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<200'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5043';

export default function () {
  const res = http.get(`${BASE_URL}/api/v1/books?page=1&pageSize=10`);
  check(res, {
    'status is 200': (r) => r.status === 200,
    'has books': (r) => {
      try {
        const body = JSON.parse(r.body);
        return body.items && body.items.length > 0;
      } catch (e) {
        return false;
      }
    },
  });
  sleep(1);
}
