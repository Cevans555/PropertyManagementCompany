# 0005: Concurrency with row versions, a section version and a serializable approval

## Context
Three situations need protection:
1. **Bonus 5.** Two applicants edit one application at once. Saves to different sections must both succeed. When both save the same section, the second must be rejected as stale rather than silently overwrite the first.
2. **Reviewers racing.** Two managers claim or review the same application.
3. **Approvals racing.** Two approvals for the same unit must not both create a lease.

## Decision
**Each applicant's own section** is protected by a SQL Server `rowversion` on `Applicant`. The page carries the row version it loaded, and `SaveApplicantDetailsAsync` compares it before saving. A mismatch throws `StaleDataException`, and EF's own check also raises `DbUpdateConcurrencyException`.

**The shared residence section** carries a `ResidenceSectionVersion` GUID on `RentalApplication`, marked as a concurrency token. Adding, editing, removing or saving residences checks the version the page loaded and assigns a new one. Residences are separate rows, so no single row version covers "the list as a whole". A section-level version does.

Because these are separate tokens, one applicant saving their details and another saving the residence list don't interfere. Two saves to the same section do.

**Status** is also a concurrency token, so a claim or review made from stale data fails instead of overwriting another manager's action.

**Approval** runs in a `Serializable` transaction. It checks for a lease covering today or overlapping the new term, then inserts the lease. Two approvals for the same unit can't both pass the check. SQL Server picks one as a deadlock victim, and `ApplicationUpdater` searches the whole inner-exception chain for error 1205 (EF wraps it several levels deep), then returns "another change to this unit was saved at the same time" instead of throwing.

All three failure kinds end up as a `ServiceResult` with a message that tells the user to reload ([0006](0006-service-result.md)).

## Alternatives considered
- **Last write wins.** Rejected: the specification requires the second save of a section to be refused.
- **Pessimistic locks while a page is open.** A web page can be left open indefinitely, and a lock would block the co-applicant's unrelated section.
- **A unique index on active leases per unit.** Leases have date ranges, and "overlapping" isn't expressible as a unique index.
- **Real-time synchronisation.** Explicitly not expected by the specification.

## Consequences
- The rules are proven against real SQL Server by integration tests, including `ConcurrentSavesToDifferentSections_BothSucceed`, the stale-page tests for each section, `StatusConcurrencyToken_RejectsAClaimMadeFromStaleData` and `TwoApprovalsForTheSameUnit_OnlyOneCreatesALease`.
- An in-memory database couldn't prove any of this, which is one reason the integration tests use LocalDB ([0010](0010-testing-strategy.md)).
- Users sometimes have to reload and redo a change. That's the intended trade-off against silent data loss.
