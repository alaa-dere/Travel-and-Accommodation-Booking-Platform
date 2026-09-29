import http from 'k6/http';
import { check, sleep } from 'k6';

const testType = __ENV.TEST_TYPE || 'load';

const testProfiles = {
  load: {
    stages: [
      { duration: '10s', target: 5 },
      { duration: '30s', target: 5 },
      { duration: '10s', target: 0 },
    ],
    thresholds: {
      checks: ['rate>0.99'],
      http_req_failed: ['rate<0.01'],
      http_req_duration: ['p(95)<750'],
    },
  },
  stress: {
    stages: [
      { duration: '15s', target: 10 },
      { duration: '20s', target: 25 },
      { duration: '30s', target: 50 },
      { duration: '15s', target: 0 },
    ],
    thresholds: {
      checks: ['rate>0.98'],
      http_req_failed: ['rate<0.02'],
      http_req_duration: ['p(95)<1500'],
    },
  },
  spike: {
    stages: [
      { duration: '5s', target: 200 },
      { duration: '20s', target: 200 },
      { duration: '5s', target: 0 },
    ],
    thresholds: {
      checks: ['rate>0.95'],
      http_req_failed: ['rate<0.05'],
      http_req_duration: ['p(95)<2000'],
    },
  },
};

if (!testProfiles[testType]) {
  throw new Error(`Unknown TEST_TYPE '${testType}'. Use 'load', 'stress', or 'spike'.`);
}

export const options = testProfiles[testType];

const baseUrl = __ENV.BASE_URL || 'http://localhost:8080';
const destination = __ENV.SEARCH_DESTINATION || 'Bethlehem';

export function setup() {
  if (!__ENV.USERNAME || !__ENV.PASSWORD) {
    throw new Error('USERNAME and PASSWORD environment variables are required.');
  }

  const response = http.post(
    `${baseUrl}/api/Auth/login`,
    JSON.stringify({
      username: __ENV.USERNAME,
      password: __ENV.PASSWORD,
    }),
    { headers: { 'Content-Type': 'application/json' } },
  );

  const loginSucceeded = check(response, {
    'login returns 200': (result) => result.status === 200,
    'login returns a token': (result) => Boolean(result.json('token')),
  });

  if (!loginSucceeded) {
    throw new Error(`Login failed with status ${response.status}.`);
  }

  return { token: response.json('token') };
}

export default function (data) {
  const checkIn = new Date();
  checkIn.setUTCDate(checkIn.getUTCDate() + 7);

  const checkOut = new Date(checkIn);
  checkOut.setUTCDate(checkOut.getUTCDate() + 3);

  const query = [
    `destination=${encodeURIComponent(destination)}`,
    `checkIn=${encodeURIComponent(checkIn.toISOString())}`,
    `checkOut=${encodeURIComponent(checkOut.toISOString())}`,
    'adults=2',
    'children=0',
    'rooms=1',
    'pageNumber=1',
  ].join('&');

  const response = http.get(`${baseUrl}/api/Search?${query}`, {
    headers: { Authorization: `Bearer ${data.token}` },
  });

  check(response, {
    'search returns 200': (result) => result.status === 200,
    'search returns a paged result': (result) =>
      Array.isArray(result.json('items')) && result.json('pageNumber') === 1,
  });

  sleep(1);
}
