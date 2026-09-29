# ADR-007: Prevent Overlapping Active Hotel Promotions

## Context

Multiple active Promotions for the same Hotel and time period make the applied
discount ambiguous. Silently selecting the largest discount may not represent
the Admin's intended pricing decision.

## Decision

A Hotel may have only one active Promotion for any instant in time. Creating or
reactivating a Promotion is rejected when its period overlaps another active
Promotion for the same Hotel.

Promotion periods use the interval `[StartDate, EndDate)`: the start is
included and the end is excluded. A new Promotion may therefore start exactly
when the previous Promotion ends.

Inactive and historical Promotions remain stored for auditing and do not block
new Promotions.

## Consequences

- The discount applied during checkout is deterministic.
- Admin pricing mistakes are reported as a conflict instead of being hidden.
- Consecutive Promotions can be scheduled without a gap.
- Overlap checks are supported by a database lookup index.
