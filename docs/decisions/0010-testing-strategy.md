# 0010: Unit, integration and browser tests each prove something different

## Context
The specification requires unit tests for business logic. The riskiest behaviour, though, lives in places a unit test can't reach: concurrency tokens, a serializable transaction, authorization over HTTP, antiforgery, and JavaScript modals that refresh part of a page.

## Decision
| Suite | Runs against | Proves |
| --- | --- | --- |
| `PropertyManagement.Tests` | Plain objects, no database | Entity rules and status transitions, validation rules, the authorization handler |
| `PropertyManagement.IntegrationTests` | The real app through `WebApplicationFactory`, and a real LocalDB database created for the run and dropped afterwards | Services, controllers, authorization results, antiforgery, 422 modal responses, database-side filtering, concurrency and the approval race, the JSON API and OpenAPI |
| `PropertyManagement.BrowserTests` | The app on a real Kestrel port, driven by headless Chromium through Playwright | Only what needs JavaScript: modal validation and in-place refresh without a reload, the applicant journey, the review modal, the grid's paging, sorting, filtering and URL state, and saving sections with errors |

- No service unit tests with a mocked `DbContext`. What's worth testing in a service is how it interacts with the database, and a mock would only prove the mock.
- No EF in-memory provider. It ignores row versions, concurrency tokens, transactions and SQL translation, which is exactly the behaviour at risk.
- The browser suite is deliberately small on `main` and doesn't repeat integration tests. The `extras` branch expands it to cover every screen.
- Integration tests assert on the app's own message constants and use the app's business clock for "today" ([0008](0008-business-time-zone.md)).

## Alternatives considered
- **Only unit tests.** They'd miss every concurrency, authorization and rendering bug. The integration tests found a real one: a deadlock that escaped as an exception because EF wrapped it more deeply than expected.
- **A large end-to-end suite.** Slower and more fragile, and it would re-prove server behaviour the integration tests already cover.

## Consequences
- `dotnet test` needs LocalDB, and the first browser run downloads Chromium.
- Each suite shares one app and database across its tests, so tests create their own data and look for their own records rather than assuming an empty database.
