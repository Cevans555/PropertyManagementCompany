# Testing

## Running

```
dotnet test                                         all three suites
dotnet test tests/PropertyManagement.Tests          unit only (no database)
dotnet test tests/PropertyManagement.IntegrationTests
dotnet test tests/PropertyManagement.BrowserTests
```

Requirements:
- **LocalDB** for the integration and browser tests.
- **Internet access on the first browser run**, which downloads Playwright's Chromium.
- Nothing else. Each run creates its own uniquely named database and drops it afterwards, so tests never touch the development database.

## What each suite is responsible for

| Suite | Runs against | Responsible for |
| --- | --- | --- |
| `PropertyManagement.Tests` | Plain objects | Entity rules and status transitions, approval and lease dates, section validation rules, units, the business time zone, the authorization handler |
| `PropertyManagement.IntegrationTests` | The whole app through `WebApplicationFactory` and a real LocalDB database | HTTP results and redirects, authorization (404/403), antiforgery, 422 modal responses, services end to end, database-side filtering and paging, the JSON API and OpenAPI, concurrency (stale saves, status token, the approval race), save-invalid sections, account flows, home dashboards |
| `PropertyManagement.BrowserTests` | The app on a real Kestrel port, driven by headless Chromium | Behaviour that needs JavaScript: modal validation and in-place refresh without a reload, the applicant journey, the review modal, the grid's paging, sorting, filtering and URL state, save-invalid sections |

**Rule of thumb:**
- A rule inside an entity gets a unit test.
- Anything involving HTTP, EF or SQL gets an integration test.
- Only something that needs JavaScript or real browser behaviour gets a browser test.

**Why no mocked `DbContext` and no EF in-memory provider?** What's worth testing in a service is its interaction with SQL Server: row versions, concurrency tokens, serializable transactions and query translation. A mock or the in-memory provider ignores exactly those, so a passing test would prove nothing. Real LocalDB found a real bug: a deadlock escaped as an exception because EF wrapped it more deeply than expected.

## Infrastructure

**Unit tests** (`tests/PropertyManagement.Tests/TestSupport`)
- `TestData` has builders: `Details(...)`, `Residence()`, `Draft`, `ReadyToSubmit`, `Submitted`, `Claimed`.
- `FixedClock` is a controllable `TimeProvider`.

**Integration tests** (`tests/PropertyManagement.IntegrationTests/Infrastructure`)
- **`PropertyManagementWebFactory`** runs the real `Program` in the `Testing` environment with a unique database name. The connection string is read lazily in `Program.cs` so this override works.
- **`WebCollection`** shares one factory, and so one app and one database, across the suite.
- **`TestHelpers`:**
  - `CreateSignedInClientAsync(email)`: a client with cookies that has logged in through the real login page
  - `PostFormAsync` and `PostFormWithTokenAsync`, which handle antiforgery tokens
  - `CreateUnitAsync` and `CreateApplicationAsync(unitId, applicantId, submit, claimedBy)`, which build data through the domain
  - `QueryDbAsync`, `UserIdAsync`, `BusinessToday()` (today from the app's own clock) and `UniqueName`
  - `EnableSaveInvalidSections()`, which turns bonus 4 on and restores it on dispose
- **`TestDatabase`** builds a `DbContext` and services directly, for service-level tests. It supplies `IdentityOptions` with `MaxLengthForKeys = 128`, so its model matches the migration.
- **`TestAccounts`** holds the seeded logins (`manager1@demo.com`, `applicant1@demo.com`, …).

**Browser tests** (`tests/PropertyManagement.BrowserTests/Infrastructure`)
- **`KestrelWebFactory`** extends the integration factory to listen on a real port. It calls `UseStaticWebAssets()`, because the `Testing` environment doesn't serve `wwwroot` otherwise.
- **`BrowserFixture`** installs Chromium if needed, starts the app and one browser, and signs in through the real login page with `SignInAsync(email)`.
- **`PageExtensions`:**
  - `Modal()`
  - `MarkPageAsync()` and `AssertNotReloadedAsync()`, which prove a section refreshed without a full page load

## Writing tests that don't interfere

- Suites share one database, and seeded data is present. **Create your own data** (`CreateUnitAsync` makes a fresh property) and assert on records you created, never on totals.
- Use `TestHelpers.UniqueName(...)` for names that must be unique.
- Use `BusinessToday()`, not `DateTime.Today`, for anything date-related.
- Assert on the app's public message constants (for example `ApplicationUpdater.StaleDataMessage`) rather than copying text.
- In browser tests, prefer role and label locators, and scope them (`#main-nav`, `page.Modal()`) when the same text appears twice on a page.
