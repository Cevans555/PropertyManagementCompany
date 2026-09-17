# Testing

Commands and prerequisites are in the [README](../README.md#tests). The integration and browser test runs each create a uniquely named LocalDB database and drop it afterwards.

## What each suite is responsible for

| Suite | Runs against | Responsible for |
| --- | --- | --- |
| `PropertyManagement.Tests` | Plain objects | Entity rules and status transitions, approval and lease dates, section validation rules, units, the business time zone, the authorization handler |
| `PropertyManagement.IntegrationTests` | The app through `WebApplicationFactory` and a real LocalDB database | HTTP results, authorization (404/403), antiforgery, 422 modal responses, services end to end, database-side filtering and paging, the JSON API and OpenAPI, concurrency and the approval race, save-invalid sections, account flows |
| `PropertyManagement.BrowserTests` | The app on a real Kestrel port, driven by headless Chromium | Behaviour that needs JavaScript: modal validation and in-place refresh, the applicant journey, the review modal, the grid, save-invalid sections |

**Rule of thumb:**
- A rule inside an entity gets a unit test.
- Anything involving HTTP, EF or SQL gets an integration test.
- Only behaviour that needs a browser gets a browser test.

Services are tested against real SQL Server rather than a mocked `DbContext`. A mock or the in-memory provider would not exercise the SQL Server behaviours this suite is intended to verify, such as row versions, transaction isolation and query translation.

## Helpers

- **Unit tests** (`TestSupport`):
  - `TestData` has builders (`Draft`, `ReadyToSubmit`, `Submitted`, `Claimed`, `Details(...)`, `Residence()`)
  - `FixedClock` is a controllable clock
- **Integration tests** (`Infrastructure`):
  - `PropertyManagementWebFactory` runs the real app on its own database
  - `TestHelpers`:
    - `CreateSignedInClientAsync(email)`
    - `PostFormAsync`, which handles antiforgery
    - `CreateUnitAsync` and `CreateApplicationAsync(...)`, which build data through the domain
    - `QueryDbAsync`
    - `BusinessToday()`
    - `EnableSaveInvalidSections()`
  - `TestAccounts` has the seeded logins
- **Browser tests** (`Infrastructure`):
  - `KestrelWebFactory` listens on a real port and serves static files
  - `BrowserFixture.SignInAsync(email)` signs in through the real login page
  - `page.Modal()`
  - `MarkPageAsync()` and `AssertNotReloadedAsync()` prove a section refreshed without a page load

## Writing tests that don't interfere

- **Create your own data.** Tests in a suite share one database that also holds seeded data, so assert only on records the test created.
- **Use `BusinessToday()`** for dates, and `TestHelpers.UniqueName(...)` for names that must be unique.
- **Assert on the app's message constants** (for example `ApplicationUpdater.StaleDataMessage`) rather than copied text.
- **In browser tests, use role and label locators,** scoped (`#main-nav`, `page.Modal()`) when the same text appears twice on a page.
