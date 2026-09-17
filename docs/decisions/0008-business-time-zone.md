# 0008: "Today" comes from a business time zone

## Context
Several rules depend on today's date: a unit is unavailable while a lease covers today, a lease may start today or later, and submit and approval check for an active lease. Timestamps are stored in UTC, but the UTC date isn't the company's date. After 8 PM Eastern the UTC date is already tomorrow. A manager approving in the evening then couldn't start a lease "today", and a lease ending today would free its unit a day early.

## Decision
- `Business:TimeZone` in `appsettings.json` (default `America/New_York`) sets the company's time zone.
- `BusinessTimeProvider` (in `Core/Common`) is a `TimeProvider` whose local time zone is that zone, registered as a singleton.
- `timeProvider.Today()` returns the calendar date in that zone. Every date rule uses it: availability, submit and approval lease checks, the earliest lease start date in the review form, and the dashboards.
- Timestamps such as `SubmittedAt` and the audit columns stay in UTC through `GetUtcNow()`.
- `Approve` takes `today` as a parameter instead of computing it from `now`, so the entity never guesses a time zone.

## Alternatives considered
- **The UTC date.** Wrong for several hours every evening, as described above.
- **The server's local time zone.** It changes with the host, for example UTC in most cloud deployments, so behaviour would depend on where the app runs.
- **The browser's time zone.** It can't be trusted for a server-side rule, and different users would see different "todays".
- **A time zone per property.** More accurate for a company spanning time zones, but beyond the specification. It's listed as an idea on the `extras` branch.

## Consequences
- Using `TimeProvider` also makes time controllable in tests. Integration tests take "today" from the app's own clock (`BusinessToday()`), so they don't fail near midnight UTC.
- One company-wide zone is a simplification that's documented in the README.
