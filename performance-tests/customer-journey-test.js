import http from 'k6/http';
import { check, sleep } from 'k6';

const testType = __ENV.TEST_TYPE || 'load';

const testProfiles = {
  load: {
    stages: [
      { duration: '10s', target: 25 },
      { duration: '10s', target: 100 },
      { duration: '40s', target: 100 },
      { duration: '10s', target: 0 },
    ],
    thresholds: {
      checks: ['rate>0.99'],
      http_req_failed: ['rate<0.01'],
      http_req_duration: ['p(95)<1000'],
    },
  },
  extreme: {
    stages: [
      { duration: '5s', target: 200 },
      { duration: '10s', target: 500 },
      { duration: '10s', target: 1000 },
      { duration: '20s', target: 1000 },
      { duration: '5s', target: 0 },
    ],
    thresholds: {
      checks: ['rate>0.90'],
      http_req_failed: ['rate<0.10'],
      http_req_duration: ['p(95)<3000'],
    },
  },
};

if (!testProfiles[testType]) {
  throw new Error(`Unknown TEST_TYPE '${testType}'. Use 'load' or 'extreme'.`);
}

export const options = testProfiles[testType];

const baseUrl = __ENV.BASE_URL || 'http://localhost:8080';
const destination = __ENV.SEARCH_DESTINATION || 'Bethlehem';

function futureStay() {
  const checkIn = new Date();
  checkIn.setUTCDate(checkIn.getUTCDate() + 7);

  const checkOut = new Date(checkIn);
  checkOut.setUTCDate(checkOut.getUTCDate() + 3);

  return { checkIn: checkIn.toISOString(), checkOut: checkOut.toISOString() };
}

function searchUrl(stay) {
  const query = [
    `destination=${encodeURIComponent(destination)}`,
    `checkIn=${encodeURIComponent(stay.checkIn)}`,
    `checkOut=${encodeURIComponent(stay.checkOut)}`,
    'adults=2',
    'children=0',
    'rooms=1',
    'pageNumber=1',
  ].join('&');

  return `${baseUrl}/api/Search?${query}`;
}

export function setup() {
  if (!__ENV.USERNAME || !__ENV.PASSWORD) {
    throw new Error('USERNAME and PASSWORD environment variables are required.');
  }

  const loginResponse = http.post(
    `${baseUrl}/api/Auth/login`,
    JSON.stringify({ username: __ENV.USERNAME, password: __ENV.PASSWORD }),
    { headers: { 'Content-Type': 'application/json' }, tags: { endpoint: 'login' } },
  );

  const token = loginResponse.json('token');
  if (!check(loginResponse, {
    'login returns 200': (response) => response.status === 200,
    'login returns a token': () => Boolean(token),
  })) {
    throw new Error(`Login failed with status ${loginResponse.status}.`);
  }

  const stay = futureStay();
  let hotelId = Number(__ENV.HOTEL_ID || 0);

  if (!hotelId) {
    const discoveryResponse = http.get(searchUrl(stay), {
      headers: { Authorization: `Bearer ${token}` },
      tags: { endpoint: 'discovery' },
    });

    hotelId = Number(discoveryResponse.json('items.0.hotelId') || 0);
    if (discoveryResponse.status !== 200 || !hotelId) {
      throw new Error(
        `No available hotel was found for '${destination}'. Set SEARCH_DESTINATION or HOTEL_ID.`,
      );
    }
  }

  return { token, hotelId, stay };
}

function get(url, token, endpoint) {
  const response = http.get(url, {
    headers: { Authorization: `Bearer ${token}` },
    tags: { endpoint },
  });

  check(response, {
    [`${endpoint} returns 200`]: (result) => result.status === 200,
  });
}

export default function (data) {
  const choice = Math.random();

  if (choice < 0.30) {
    get(searchUrl(data.stay), data.token, 'search');
  } else if (choice < 0.48) {
    get(`${baseUrl}/api/hotels/${data.hotelId}`, data.token, 'hotel-details');
  } else if (choice < 0.60) {
    const dates = `checkIn=${encodeURIComponent(data.stay.checkIn)}&checkOut=${encodeURIComponent(data.stay.checkOut)}`;
    get(
      `${baseUrl}/api/hotels/${data.hotelId}/available-rooms?${dates}&adults=2&children=0`,
      data.token,
      'available-rooms',
    );
  } else if (choice < 0.68) {
    get(`${baseUrl}/api/hotels/${data.hotelId}/images`, data.token, 'hotel-images');
  } else if (choice < 0.75) {
    get(`${baseUrl}/api/hotels/${data.hotelId}/reviews?pageNumber=1`, data.token, 'reviews');
  } else if (choice < 0.81) {
    get(`${baseUrl}/api/hotels/${data.hotelId}/location`, data.token, 'hotel-location');
  } else if (choice < 0.88) {
    get(`${baseUrl}/api/FeaturedDeals`, data.token, 'featured-deals');
  } else if (choice < 0.94) {
    get(`${baseUrl}/api/hotels/recently-visited`, data.token, 'recently-visited');
  } else {
    get(`${baseUrl}/api/hotels/trending-destinations`, data.token, 'trending-destinations');
  }

  sleep(1);
}
