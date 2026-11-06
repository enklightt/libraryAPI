import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '20s', target: 50 },
    { duration: '30s', target: 100 },
    { duration: '30s', target: 200 },
    { duration: '30s', target: 300 },
    { duration: '20s', target: 0 },
  ],
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5043';

export default function () {
  const res = http.get(`${BASE_URL}/api/v1/books?page=1&pageSize=20`);
  check(res, {
    'status is 200': (r) => r.status === 200,
  });
  sleep(0.05);
}
