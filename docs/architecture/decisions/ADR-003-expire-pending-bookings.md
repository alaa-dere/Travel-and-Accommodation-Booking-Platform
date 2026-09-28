# ADR-003: Expire Pending Bookings After a Limited Payment Window

## Context

A Pending Booking temporarily prevents other Customers from booking the same
Room for overlapping dates while its Payment is being completed.

A Customer may leave checkout, close the application, or never complete the
required Payment action. Pending Bookings therefore need a limited lifetime so
that an abandoned checkout does not keep a Room unavailable indefinitely.

## Decision

A Pending Booking will hold its Room for 15 minutes from the time the Booking is
created.

If a verified successful Payment is received during this period, the Booking
will become Confirmed.

If the Payment is not completed before the payment window expires, the Booking
will become Cancelled and the Room will become available for other Customers.

The payment-window duration will be configurable rather than hard-coded in the
application logic. The initial configuration will be:

```json
{
  "Booking": {
    "PendingExpirationMinutes": 15
  }
}
```

Each Booking will store its calculated `PendingExpiresAt` value in UTC when it
is created. Expiration will not be recalculated from `CreatedAt` during later
queries. This ensures that changing the configured payment window affects new
Bookings only and does not unexpectedly shorten or extend the window of an
existing Pending Booking.

Application services will obtain the current UTC time through the .NET
`TimeProvider` abstraction. Production will use `TimeProvider.System`, while
tests can provide a controlled clock. The Application layer will calculate the
expiration time and pass it to the Booking rather than allowing the Domain
entity to read the system clock or contain a hard-coded 15-minute duration.

The Booking status and `PendingExpiresAt` fields will be indexed together so
that expired Pending Bookings can be located without scanning all historical
Bookings.

Expiration will be processed by a background operation that periodically
cancels expired Pending Bookings. Availability checks will also ignore an
expired Pending Booking so that a delayed background execution does not keep a
Room unavailable beyond the configured payment window.

The periodic operation will be implemented with a .NET `BackgroundService`.
The BackgroundService will only schedule the work and create the required
dependency-injection scope. The expiration rules and processing logic will
remain in an Application service so that they can be tested independently and
reused if the scheduling mechanism changes.

This option was selected because the expiration work is small and the database
remains the source of truth. It avoids introducing an external job-processing
library for the current project scope. If the system later requires distributed
job coordination, persistent job history, or operational dashboards, the
scheduler can be replaced without moving the business logic out of the
Application layer.

When the payment window expires, the system will attempt to cancel the related
Stripe PaymentIntent. If a successful Payment result is received after the
Booking has already expired and become Cancelled, the Booking will remain
Cancelled and the captured amount will be refunded automatically. The Payment
will be marked as Refunded only after Stripe confirms that the refund was
created successfully.

This behavior was selected because the Room may have become available to other
Customers after expiration. A late Payment must not restore an expired Booking
or allow the system to keep money for a reservation it can no longer guarantee.

## Consequences

### Positive

- Abandoned checkouts do not block Rooms indefinitely.
- Customers receive a defined period in which to complete Payment.
- The timeout can be adjusted without changing application code.
- Room availability remains correct if background processing is delayed.
- Customers are not charged for Bookings that expired before Payment was
  confirmed.
- The scheduling mechanism can be replaced without changing the expiration
  business rules.

### Negative

- The system needs a reliable background expiration process.
- Multiple application instances require coordination so that the same expired
  Booking is not processed concurrently.
- Availability queries must account for the Pending Booking expiration time.
- PaymentIntent cancellation and automatic refund operations must be reliable
  and idempotent.

Failed or cancelled refund attempts are published for bounded asynchronous
retry according to ADR-004. Pending or action-required refunds are reconciled
through Stripe's signed webhook events.
