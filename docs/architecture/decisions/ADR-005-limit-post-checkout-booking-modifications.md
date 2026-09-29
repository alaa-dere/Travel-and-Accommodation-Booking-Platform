# ADR-005: Limit Booking Modifications After Checkout

## Context

A Booking, its Invoice, and its Stripe Payment must always represent the same
financial amount. Changing a room or stay dates after checkout can change the
Booking price while the existing Stripe Payment remains unchanged.

Supporting these changes safely would require collecting or refunding the
price difference and handling failures, retries, and grouped Invoice payments.

## Decision

After checkout, a Customer may update only the guest counts and special
requests for an upcoming Booking. Guest counts must remain within the selected
Room's capacity.

The Room and stay dates are not editable. A Customer who needs different dates
or a different Room must cancel the existing Booking and create a new one.

Cancellation and any required refund are handled as a separate operation and
will not be mixed with Booking modification.

This policy keeps the Booking price, Invoice total, and Stripe Payment amount
consistent without introducing a complex payment-adjustment workflow.

## Consequences

- Booking details can be updated without changing financial records.
- Room availability does not need to be recalculated for a details-only update.
- Customers must cancel and rebook to change dates or Rooms.
- Cancellation must define how paid Bookings are refunded.
