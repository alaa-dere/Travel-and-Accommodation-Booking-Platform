# ADR-001: Restrict a Cart and Checkout to One Hotel

## Context

The system allows a Customer to select multiple Rooms before completing
checkout. If a single cart contains Rooms from different Hotels, checkout must
create multiple Invoices and initiate multiple payment operations.

Those payment operations cannot be committed atomically with each other or with
the local database transaction. One Hotel payment could succeed while another
fails, leaving the Customer with a partially completed checkout and requiring
more complex confirmation, cancellation, and refund handling.

Each Hotel has its own Rooms, and each checkout produces a separate
Invoice, booking confirmation, and payment outcome for that Hotel.

## Decision

A Customer may add multiple Rooms to a cart only when all selected Rooms belong
to the same Hotel.

A checkout therefore processes:

- One Hotel.
- One Invoice.
- One Payment.
- One or more Bookings for Rooms belonging to that Hotel.

When a Customer attempts to add a Room from another Hotel while the cart is not
empty, the API will reject the operation with a conflict response. The Customer
must complete or clear the current cart before selecting a Room from another
Hotel.

## Consequences

### Positive

- Checkout has one predictable payment outcome.
- Partial payment across Hotels is prevented.
- Confirmation, cancellation, and refund behavior is simpler.
- Invoice and Payment ownership remains clear.
- The implementation and user-facing API remain readable and maintainable.

### Negative

- Customers booking different Hotels must complete separate checkouts.
- Cart and checkout tests must enforce the single-Hotel invariant.


