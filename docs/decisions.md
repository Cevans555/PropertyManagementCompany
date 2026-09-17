# Design decisions

The decisions that aren't obvious from reading one class. Each one covers what was chosen, why, and what was considered instead.

- [AppUser and Applicant are separate](#appuser-and-applicant-are-separate)
- [A selectively rich domain model](#a-selectively-rich-domain-model)
- [No repository layer](#no-repository-layer)
- [Query classes without CQRS or MediatR](#query-classes-without-cqrs-or-mediatr)
- [Manager notes are their own aggregate](#manager-notes-are-their-own-aggregate)
- [Optimistic concurrency for edits, a serializable transaction for approval](#optimistic-concurrency-for-edits-a-serializable-transaction-for-approval)
- [Expected failures are results, not exceptions](#expected-failures-are-results-not-exceptions)
- [Not found instead of forbidden](#not-found-instead-of-forbidden)
- ["Today" comes from a business time zone](#today-comes-from-a-business-time-zone)
- [Saving invalid sections is behind a feature switch](#saving-invalid-sections-is-behind-a-feature-switch)
- [Applicant addresses are separate nullable fields](#applicant-addresses-are-separate-nullable-fields)
- [Browser tests are a small smoke suite](#browser-tests-are-a-small-smoke-suite)

---

## AppUser and Applicant are separate

**Decision.** `AppUser` (in Data) is the Identity account: login, name, email, role. `Applicant` (in Core) is one person's place on one application, holding the details they submitted for it.

**Why.**
- **What was submitted must stay as submitted.** If someone later changes their account name or email, applications they already submitted shouldn't change.
- **Each application gets its own details.** The same person can apply for two units with different current addresses or phone numbers.
- **Per-applicant state lives with the application.** `Applicant` carries its own row version and "saved without errors" state, which is what lets two co-applicants edit at the same time.
- **Core stays free of Identity.** Core has no dependency on Identity, and `AppUser` extends `IdentityUser`, so it has to live in Data.

**Considered.** Storing application details on the account. That would couple every application to the account's current values, and co-applicants would need a join table carrying section state anyway.

## A selectively rich domain model

**Decision.** `RentalApplication` is rich: private setters, and state changes only through methods (`Submit`, `Claim`, `Approve`, `SaveApplicantDetails`, …) that throw `DomainException` when a rule is broken. `Property` enforces unit rules the same way. Lookups (`UnitType`, `ApplicationStatusType`), `Lease` and the audit columns stay simple.

**Why.** The application lifecycle is where the rules are: who may act, in which status, whether sections are complete, whether a claim is held. Those rules must hold whether a controller, a service or the seeder triggers the change. Where there are no real rules, a rich model adds ceremony without protecting anything.

**Considered.**
- **Anemic entities with rules in services.** The rules spread out, and any new code path can skip a check.
- **Full DDD** with domain events, more value objects and a separate application layer. More structure than this domain needs.

**Consequence.** The entity can't query the database. The service asks database questions ("is this unit leased?") and passes the answer in, for example `Submit(userId, unitHasActiveLease, now)`.

## No repository layer

**Decision.** Services and queries use `PropertyManagementDbContext` directly.

**Why.** The `DbContext` is already a unit of work, and each `DbSet` is already a repository. A generic repository either hides `IQueryable`, losing database-side filtering, projection and paging, or exposes it and adds little. Using EF directly keeps `Include`, projections, `ExecuteUpdate` and transactions available where they're needed.

**Considered.** A generic `IRepository<T>`, and per-entity repositories. Both duplicate `DbSet` and gain a method for every query shape.

**Consequence.** Services aren't unit tested against mocks. They're integration tested against real SQL Server, which is where concurrency behaviour can actually be exercised. See [testing](testing.md).

## Query classes without CQRS or MediatR

**Decision.** Reads that are reusable, complex or shaped for a page live in named query classes, grouped by concept. **`Data/Queries`** answers questions about the data and returns plain values (`LeaseQueries`, `ApplicationQueries`, `ManagerNoteQueries`). **`Web/Queries`** projects into view models (`PropertyQueries`, `ReviewQueries`, `Applications/ApplicationListQuery`, …). A one-line lookup used in a single place can stay in the service that uses it.

**Why.** Controllers and view components stay focused on handling a request. Where a query lives follows the direction of references: Data can't return Web's view models without a parallel DTO for every page, and with one front end that DTO layer would add cost without much benefit.

**Considered.**
- **MediatR and full CQRS.** A handler per request and a pipeline, with no second front end or cross-cutting behaviour to justify them.
- **All queries in Data.** Duplicate DTOs for every page.
- **Queries inline in controllers.** Mixed responsibilities, and duplicated reads.

## Manager notes are their own aggregate

**Decision.** `ManagerNote` has a foreign key to the application but no navigation property from it. Notes go through `ManagerNoteService` and `ManagerNoteQueries` and a controller that requires the manager role plus the `ManageNotes` check. The panel renders only when `CanManageNotes` is true.

**Why.** Bonus 3 says notes are never rendered or returned to an applicant. If notes were a collection on the application, any `Include` or serialization of the application could carry them into an applicant response, and keeping them out would be a rule every read has to remember. As a separate aggregate, loading an application never loads its notes. A bonus effect: writing a note doesn't change the application's concurrency tokens, so it can't make an applicant's save stale.

**Considered.** A `Notes` collection with filtering in views. That hides the notes but still loads them, so one mistake exposes them.

## Optimistic concurrency for edits, a serializable transaction for approval

**Decision.**
- **An applicant's own details:** a SQL `rowversion` on `Applicant`.
- **The shared residence list:** a `ResidenceSectionVersion` GUID on the application. It's a concurrency token, checked and renewed on every add, edit, remove and save.
- **Claims and reviews:** `Status` is a concurrency token.
- **Approval:** checks for a lease covering today or overlapping the new term, then inserts the lease, all inside a `Serializable` transaction. If two approvals race, SQL Server makes one a deadlock victim. `ApplicationUpdater` walks the inner-exception chain for error 1205, because EF wraps it several levels deep, and returns a "try again" message.

**Why the difference.**
- **Edits:** the risk is overwriting someone else's work, and conflicts are rare. Optimistic checks cost very little until they fail, then tell the user to reload, which matches bonus 5's "rejected as stale with a message to reload".
- **Tokens per section:** saves to different sections must not interfere, so each section has its own token. Residences are separate rows, so no single row version covers "the list"; a section-level version does.
- **Approval:** the risk is two leases for one unit. That's a rule across rows ("no overlapping lease for this unit"), which an optimistic token on one row can't enforce. Overlapping date ranges can't be a unique index either. A serializable transaction makes check-then-insert atomic.

**Considered.**
- **Last write wins.** It breaks bonus 5.
- **Pessimistic locks while a page is open.** A page can stay open indefinitely, and a lock would block the co-applicant's other section.
- **A unique index on leases.** It can't express overlapping ranges.

## Expected failures are results, not exceptions

**Decision.** Services return `ServiceResult` or `ServiceResult<T>`. `DomainException`, `StaleDataException`, `DbUpdateConcurrencyException` and deadlocks are caught in `ApplicationUpdater` or the service, the change tracker is cleared, and a message is returned. Unexpected failures still throw and reach the global exception handler.

**Why.** A stale page or an incomplete section is a normal outcome that needs a specific message in a specific form, such as re-rendering a modal with 422. It isn't an error page. Returning a result makes every controller follow the same pattern: call, then branch on `Succeeded`.

**Considered.** Try/catch in every action, which is easy to miss. Exception-to-status middleware, which can't re-render the right form with the user's input.

## Not found instead of forbidden

**Decision.** When a user can't view an application, the response is 404. When they can view it but not perform the action, it's 403.

**Why.** Returning 403 for someone else's application would confirm that the id exists. The handler (`View`, `Edit`, `Withdraw`, `Review`, `ManageNotes`) answers "may this user attempt this?", while the entity still answers "is it valid now?". Rules aren't duplicated between the two.

## "Today" comes from a business time zone

**Decision.** `Business:TimeZone` (default `America/New_York`) feeds `BusinessTimeProvider`. `timeProvider.Today()` is used for every date rule: availability, lease checks, the earliest lease start date and the dashboards. Timestamps stay in UTC. `Approve` receives `today` as a parameter.

**Why.** After 8 PM Eastern the UTC date is already tomorrow. A manager approving in the evening then couldn't start a lease "today", and a lease ending today would free its unit early. The server's own time zone depends on where the app is hosted, and the browser's can't be trusted for a server rule.

**Considered.** A time zone per property. More accurate for a company spanning time zones, but beyond the specification, so it's an idea for the `extras` branch.

## Saving invalid sections is behind a feature switch

**Decision.** Bonus 4 runs behind `Features:SaveInvalidSections`, off by default and read per request. The switch only decides whether a section with errors may be **saved**. What counts as **valid** is recalculated from the stored data by the same `Core/Validation` rules for the form, the entity, the Summary's blocker list and Submit. Errors marked `BlocksSaving`, such as a value too long for its column, are refused regardless.

**Why.**
- **The two behaviours conflict.** The base specification says Continue saves only a valid section; bonus 4 says the opposite. A switch lets the app match the base specification by default and show the bonus on demand.
- **Validity is never stored.** Recalculating means an application saved with errors can never be submitted, even after the switch is turned off or the rules change.

**Considered.**
- **Storing an "is valid" flag.** It can go stale.
- **Always on.** That would change the base behaviour reviewers expect.

## Applicant addresses are separate nullable fields

**Decision.** `Applicant` stores `Street`, `City`, `State` and `PostalCode` as separate nullable columns instead of the `Address` value object, which properties and residences use.

**Why.** `Address` refuses to be created incomplete, which is right for a property or a saved residence. With bonus 4, though, an applicant's section can be saved with errors, so it must be able to hold a half-typed address. Completeness is checked by `ApplicantDetailsRules` instead. A comment in `Applicant.cs` records this.

**Considered.** Making `Address` allow empty parts, which would weaken it everywhere. A separate draft copy of the form, which would be a second storage path for the same data.

## Browser tests are a small smoke suite

**Decision.** On `main`, Playwright covers only behaviour that needs JavaScript: modal validation and in-place refresh, the applicant journey, the review modal, the grid, and saving sections with errors.

**Why.** Everything server-side (authorization, validation, concurrency, database filtering, the API) is proven faster and more reliably by the integration tests. A large browser suite would repeat that coverage while being slower and more fragile. The `extras` branch expands browser coverage to every screen for anyone who wants it. See [testing](testing.md).
