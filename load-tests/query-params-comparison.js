import http from 'k6/http';
import { check } from 'k6';

export const options = {
  vus: 5,
  duration: '20s',
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5043';
const PAGE_SIZE = __ENV.PAGE_SIZE || '10';
const SORT_BY = __ENV.SORT_BY || 'title';

export default function () {
  // Bust cache slightly with unique search or test query path
  const res = http.get(`${BASE_URL}/api/v1/books?page=1&pageSize=${PAGE_SIZE}&sortBy=${SORT_BY}`);
  check(res, {
    'status is 200': (r) => r.status === 200,
  });
}
