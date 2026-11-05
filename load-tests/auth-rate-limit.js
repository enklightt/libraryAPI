import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  vus: 1,
  iterations: 15,
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5043';

export default function (data) {
  const payload = JSON.stringify({
    email: 'test_ratelimit@example.com',
    password: 'wrong_password',
  });

  const params = {
    headers: {
      'Content-Type': 'application/json',
    },
  };

  const res = http.post(`${BASE_URL}/api/v1/auth/login`, payload, params);

  // iterations 0-9 (first 10 requests) should be allowed (401 unauthorized because credentials are wrong)
  // iterations 10-14 (requests 11+) should receive 429 Too Many Requests
  if (__ITER < 10) {
    check(res, {
      'first 10 requests allowed (not 429)': (r) => r.status !== 429,
    });
  } else {
    check(res, {
      'request > 10 throttled with 429': (r) => r.status === 429,
    });
  }

  sleep(0.1);
}
