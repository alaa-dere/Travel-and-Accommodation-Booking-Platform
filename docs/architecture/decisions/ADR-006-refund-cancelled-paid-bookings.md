# ADR-006: Refund Cancelled Paid Bookings Independently

## Context

One Invoice may contain multiple Bookings from the same Hotel, while Stripe
processes one Payment for the complete Invoice. A Customer may cancel one
eligible Booking without cancelling the other Bookings on that Invoice.

Refunding the complete Payment would return money for Bookings that remain
active. Recording only one refund on the Payment also cannot represent several
Bookings being cancelled independently.

## Decision

When a Customer cancels a paid Booking before check-in, the system requests a
partial Stripe refund equal to that Booking's total price.

Refund information is tracked on the cancelled Booking, including the refunded
amount, Stripe refund identifier, status, and failure code. The original
Invoice total remains unchanged as a historical record of the checkout.

The Payment remains Paid while any Booking on its Invoice remains active. When
all Bookings covered by the Payment have been cancelled and refunded, the
Payment may transition to Refunded.

A Booking whose Payment is still being processed cannot be cancelled until the
Payment reaches a final state. Refund requests use the Booking identifier as an
idempotency key so retrying the same cancellation does not create another
refund.

## Consequences

- Cancelling one Booking does not refund unrelated Bookings.
- Booking, Invoice, and Stripe financial records remain auditable.
- Refund outcomes can complete asynchronously through Stripe webhooks.
- Customers may need to retry cancellation if Stripe is temporarily
  unavailable.
