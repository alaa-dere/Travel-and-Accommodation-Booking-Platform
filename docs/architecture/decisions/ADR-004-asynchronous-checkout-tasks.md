# ADR-004: Process Checkout Side Effects Asynchronously

## Context

Booking confirmation must not depend on the availability or response time of
the SMTP server. Failed or cancelled Stripe refunds also require controlled
retries without blocking the pending-booking expiration worker.

## Decision

RabbitMQ will be used through MassTransit for checkout work that can be
completed after the HTTP request or expiration cycle has finished.

Two message contracts are used:

- `SendBookingConfirmation` sends the confirmation email.
- `RetryPaymentRefund` retries a Stripe refund that Stripe rejected or cancelled.

Consumers use bounded retry intervals. Messages that still fail are moved by
MassTransit to an error queue. Refund retries use the stable Stripe idempotency
key `expired-booking-refund:{providerPaymentId}`, so duplicate delivery cannot
create duplicate refunds.

Core room availability, Pending Booking creation, invoicing, and payment-state
updates remain synchronous. Invoice PDFs are generated on demand by the
authenticated download endpoint because the PDF is the requested HTTP response.

Production and Docker environments use RabbitMQ. Automated tests use an
in-memory transport where messaging behavior is required.

A transactional EF outbox is not enabled in this .NET 8 version because the
selected compatible MassTransit line would otherwise introduce EF Core 9.
Mixing EF Core major versions was rejected. A compatible outbox should be added
before requiring guaranteed email publication across a database-commit and
broker-outage boundary.

## Consequences

### Positive

- SMTP delays do not extend checkout response time.
- Refund failures receive bounded retries and an observable error queue.
- MassTransit handles broker connections, acknowledgements, and retries.
- Application and Domain code do not depend on MassTransit.

### Negative

- RabbitMQ is a runtime dependency outside Testing.
- Error queues must be monitored.
- Without a transactional outbox, a crash exactly between database commit and
  email publication can require manual recovery.
