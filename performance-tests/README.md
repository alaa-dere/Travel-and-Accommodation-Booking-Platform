# Performance tests

See [RESULTS.md](RESULTS.md) for the latest recorded local test results and
their limitations.

The search performance test logs in once, reuses the returned customer JWT, and
sends hotel-search requests. It supports two profiles:

- `load`: ramps up to five concurrent users and verifies normal expected load.
- `stress`: progressively increases the load to 50 concurrent users.
- `spike`: rapidly jumps to 200 concurrent users to test a sudden traffic surge.

It fails when more than 1% of requests fail, fewer than 99% of checks pass, or
the 95th-percentile response time is 750 ms or more.

Start the application with Docker before running the test:

```powershell
docker compose up --detach
```

Run the test with an existing Customer account:

```powershell
& "C:\Program Files\k6\k6.exe" run `
  -e USERNAME="<customer-username>" `
  -e PASSWORD="<customer-password>" `
  -e SEARCH_DESTINATION="Bethlehem" `
  performance-tests/search-load-test.js
```

Run the stress profile with the same credentials:

```powershell
& "C:\Program Files\k6\k6.exe" run `
  -e TEST_TYPE="stress" `
  -e USERNAME="<customer-username>" `
  -e PASSWORD="<customer-password>" `
  -e SEARCH_DESTINATION="Bethlehem" `
  performance-tests/search-load-test.js
```

The stress profile allows up to 2% failed requests and requires a
95th-percentile response time below 1500 ms.

Run the short, high-load spike profile:

```powershell
& "C:\Program Files\k6\k6.exe" run `
  -e TEST_TYPE="spike" `
  -e USERNAME="<customer-username>" `
  -e PASSWORD="<customer-password>" `
  -e SEARCH_DESTINATION="Bethlehem" `
  performance-tests/search-load-test.js
```

The spike profile reaches 200 concurrent users in five seconds and lasts 30
seconds. It allows up to 5% failed requests and requires a 95th-percentile
response time below 2000 ms.

Set `BASE_URL` only when the API is not running at `http://localhost:8080`.
Do not commit real credentials to this file or any test script.

## Customer journey test

`customer-journey-test.js` generates a mixed, read-heavy workload across hotel
search, details, availability, images, reviews, location, featured deals,
recently visited hotels, and trending destinations. It ramps up to 100
concurrent users and avoids checkout and payment mutations.

The setup discovers an available hotel from the configured destination. Set
`HOTEL_ID` only when you want to test a specific active hotel.

```powershell
& "C:\Program Files\k6\k6.exe" run `
  -e USERNAME="<customer-username>" `
  -e PASSWORD="<customer-password>" `
  -e SEARCH_DESTINATION="Bethlehem" `
  performance-tests/customer-journey-test.js
```

The optional `extreme` profile rises to 1,000 concurrent users in 25 seconds,
holds that load for 20 seconds, and then ramps down. It is intended to expose
the approximate limit of a local environment, so failures under this profile
are diagnostic rather than a production-capacity guarantee.

```powershell
& "C:\Program Files\k6\k6.exe" run `
  -e TEST_TYPE="extreme" `
  -e USERNAME="<customer-username>" `
  -e PASSWORD="<customer-password>" `
  -e SEARCH_DESTINATION="Bethlehem" `
  performance-tests/customer-journey-test.js
```
