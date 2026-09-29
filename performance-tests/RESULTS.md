# Performance test results

Test date: 2026-09-29

These tests ran locally on Windows against the Docker Compose environment. The
API, SQL Server, and RabbitMQ were running in containers. The results describe
this local environment and are not a production-capacity guarantee.

## Results summary

| Workload | Maximum VUs | Requests | Approx. requests/sec | Failed requests | Average | p95 | Maximum |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Search load | 5 | 207 | 4 | 0% | 11.06 ms | 13.99 ms | 203.91 ms |
| Search stress | 50 | 1,913 | 24 | 0% | 9.02 ms | 15.87 ms | 366.37 ms |
| Search spike | 200 | 5,050 | 164 | 0% | 8.98 ms | 21.87 ms | 119.94 ms |
| Customer journey load | 100 | 5,266 | 74 | 0% | 6.33 ms | 15.58 ms | 114.84 ms |
| Customer journey extreme | 1,000 | 32,082 | 629 | 0% | 76.48 ms | 479.01 ms | 1.87 s |

All configured k6 checks and thresholds passed. No iterations were interrupted.

## Scope

The search workloads exercised the authenticated hotel search endpoint. The
customer-journey workloads distributed traffic across search, hotel details,
availability, images, reviews, location, featured deals, recently visited
hotels, and trending destinations.

Checkout and payment were intentionally excluded because high-volume execution
would create bookings and invoices and call the external payment provider.

## Interpretation and limitations

The 1,000-VU mixed workload shows that the local environment remained stable at
approximately 629 requests per second with no failed requests. Its p95 latency
increased to 479.01 ms, compared with 15.58 ms for the 100-VU journey, showing
the expected effect of resource contention at high load.

The tests reused one authenticated customer and a limited local dataset. They
did not include internet latency, multiple application instances, a
production-sized database, geographically distributed traffic, or production
monitoring. A production capacity assessment should run against a production-
like staging environment using multiple accounts and distributed load
generators.
