# ADR-002: Confirm Bookings Only After Successful Payment

## Context

Creating a Booking and completing its Payment are separate steps. A Stripe
Payment may succeed immediately, fail, or require additional Customer
authentication before reaching a final result.

The Booking status therefore needs to represent whether the reservation is
still waiting for Payment or has received a verified final payment outcome. A
client response alone is not sufficient because the Customer may close the
application before Payment finishes, and client-provided results cannot be
treated as trusted Payment confirmation.

## Decision

A Booking will start in the Pending state while Payment is incomplete.

The related Bookings will transition as follows:

- A verified successful Payment changes Pending Bookings to Confirmed.
- A failed or cancelled Payment changes Pending Bookings to Cancelled.
- A Payment that requires additional Customer action leaves the Bookings in
  Pending until a verified final result is received.

Payment success may be verified from the immediate Stripe PaymentIntent result
or from a signed Stripe webhook. The system will not confirm a Booking based
only on information supplied by the client.

This approach was selected to keep the Booking status consistent with the
trusted Payment result and to support Stripe Payments that complete
asynchronously.

The transition from Confirmed to Completed is outside this decision and will be
defined separately.

## Consequences

### Positive

- Booking and Payment statuses remain consistent.
- Unpaid Bookings are not presented as confirmed reservations.
- Payments requiring additional authentication are supported.
- Stripe remains the trusted source of the final payment outcome.

### Negative

- Booking confirmation must be handled in both immediate and webhook payment
  flows.
- Pending Bookings require a future expiration policy when payment is abandoned.
