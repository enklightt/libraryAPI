import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '30s', target: 30 },
    { duration: '1m', target: 50 },
    { duration: '30s', target: 0 },
  ],
  thresholds: {
    http_req_failed: ['rate<0.02'],
    http_req_duration: ['p(95)<400'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5043';
const KNOWN_BOOK_ID = '7559913e-f41b-497f-adb0-5494b1ff052f'; // 1984

export default function () {
  const rand = Math.random();

  if (rand < 0.80) {
    // 80% GET books
    const page = Math.floor(Math.random() * 3) + 1;
    const res = http.get(`${BASE_URL}/api/v1/books?page=${page}&pageSize=10`);
    check(res, {
      'books status is 200': (r) => r.status === 200,
    });
  } else if (rand < 0.95) {
    // 15% GET book by id
    const res = http.get(`${BASE_URL}/api/v1/books/${KNOWN_BOOK_ID}`);
    check(res, {
      'book by id status is 200': (r) => r.status === 200,
    });
  } else {
    // 5% GET genres
    const res = http.get(`${BASE_URL}/api/v1/genres`);
    check(res, {
      'genres status is 200': (r) => r.status === 200,
    });
  }

  sleep(0.1);
}
