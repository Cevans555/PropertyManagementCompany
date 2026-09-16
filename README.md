# PropertyManagementCompany
A web application for submitting and reviewing rental applications.

Built with ASP.NET Core MVC and Razor on .NET 10, using ASP.NET Identity, SQL Server and Entity Framework Core.

## Getting started

### Prerequisites
- .NET 10 SDK
- SQL Server LocalDB (installed with Visual Studio's "ASP.NET and web development" workload)

### Database
The connection string is in `src/PropertyManagement.Web/appsettings.json` and points at LocalDB, so no setup is needed. To use a different SQL Server, change `DefaultConnection`.

### Run
Open `PropertyManagement.slnx` in Visual Studio and press F5, or run:
```
dotnet run --project src/PropertyManagement.Web
```

### Demo accounts
The database is seeded on start-up, so these accounts already exist. They are here rather than in the app, because the application itself never displays credentials.

| Role | Email | Password |
| --- | --- | --- |
| Property manager | `manager1@demo.com` | `Password123!` |
| Applicant | `applicant1@demo.com` | `Password123!` |

The seed creates `manager1@demo.com` and `manager2@demo.com`, and `applicant1@demo.com` through `applicant8@demo.com`, all with the same password. You can also register a new account and choose either role.

### Feature switches
`Features` in `src/PropertyManagement.Web/appsettings.json`:

| Setting | Default | What it does |
| --- | --- | --- |
| `SaveInvalidSections` | `false` | **Bonus 4.** Set to `true` to let an applicant save a section that fails validation, continue through the application, see those errors listed on the summary, and come back to fix them. Submit stays blocked until every error is fixed. |

The value is read per request, so changing it takes effect without restarting the app. It's off by default so the app behaves exactly as the base specification describes; turn it on to see the bonus.

## Solution structure
```
PropertyManagement.slnx
├─ src/
│  ├─ PropertyManagement.Web    MVC app: startup, controllers, views
│  ├─ PropertyManagement.Core   entities and business rules (no database or web code)
│  └─ PropertyManagement.Data   DbContext, migrations, seeding
└─ tests/
   ├─ PropertyManagement.Tests             unit tests for Core (no database)
   ├─ PropertyManagement.IntegrationTests  services and pages against a real LocalDB database
   └─ PropertyManagement.BrowserTests      Playwright smoke tests for the client-side flows
```
References point toward Core: Web → Core, Data; Data → Core; Tests → Core.

## Design notes

**Rich entities, thin services.** `RentalApplication` is the aggregate root and enforces the rules; nothing can change an application except through its methods. Services load it, answer the questions it can't (such as whether a unit is already leased), call the method and save.

**Queries vs services.** Reusable, complex or presentation-heavy reads are grouped into named query classes, so controllers and view components stay focused on handling a request and rendering a result. Which project a query class lives in follows the shape it returns:

- `Data/Queries` answers questions about the data itself, with no presentation types involved — lease availability, "does this applicant already have an open application on this unit", "which application does this note belong to". Services and controllers both use them.
- `Web/Queries` holds reads that project into view models. They belong to Web because that's what they're coupled to; references only point one way (`Web → Data → Core`), so a query in Data cannot return a view model without a parallel DTO for every list on every page. With one front end that would be cost without benefit.

This isn't a rule that every read needs a class of its own. A small one-off lookup can stay next to the workflow that uses it — a one-line `AnyAsync` inside a service is clearer where it is than wrapped in an abstraction. Mutations, transactions and orchestration belong to services; business invariants belong to the entities.

**Modal pattern.** Create, edit and remove happen in one shared modal in the layout, driven by `wwwroot/js/modal.js`:

- A trigger carries `data-modal-url` (the partial to load) and `data-modal-refresh` (the page sections to reload afterwards).
- `GET` returns a form partial. A failed `POST` returns **422** with the same partial re-rendered with its validation errors, so the modal updates in place. A successful `POST` returns JSON, so the modal closes and each refresh target reloads from its own `data-refresh-url`.
- Controllers express that with two helpers, `ModalInvalid` and `ModalSuccess`, and view components (`PropertyList`, `UnitTable`) both render a section and serve its refresh URL, so the markup exists once.

**The application page.** One page shows one section at a time (applicant information, residence history, summary). A single form posts to a single action, and the clicked button decides what happens: Continue validates and saves the current section then moves on, Back never saves, and Submit is only accepted from the summary. The same section partials render editable or read-only from a server-side decision, so the summary reuses them rather than duplicating markup, and a manager viewing an application gets the read-only rendering of the same views.

**Review workflow.** A property manager claims a submitted application from the review queue before completing it, which moves it to Under Review and records who holds it. Only the manager holding it can complete or release the review; anyone else sees who has it. Completing a review is one modal with three outcomes: approve (which creates a 12-month lease from a chosen start date), return, or deny. Return and deny require a comment, and every outcome is recorded in the status history.

**The application list and grid.** Role scoping, both filters, the sort and `Skip`/`Take` are all applied to the `IQueryable` before it runs, and the filtered total is counted in the same request, so SQL Server does the work rather than the app loading every application and narrowing it in memory. Applicant names are resolved in a second lookup for just the rows on the page, instead of a join per row. Every sort ends with `Id`, so rows with equal sort values can't swap between pages and appear twice; a page past the end is clamped to the last page. `ApplicationListQuery` lives in `Web/Queries/Applications` rather than `Data/Queries` because it takes a `ClaimsPrincipal` and projects directly into presentation view models.

**The grid is reusable.** `GridViewComponent` renders the table shell, headers and sort buttons from a `GridViewModel` that names a JSON endpoint and describes its columns (key, title, sort key, and a format: text, date, badge or link). `wwwroot/js/grid.js` fetches the rows. Nothing in `Models/Grid` knows about applications, so any endpoint that accepts `page`, `pageSize`, `sort` and `direction` and returns a `PagedResult<T>` can use it by describing its columns. Grid state lives in the page URL, so refreshing, bookmarking and Back/Forward all work, and cells are written with `textContent`, so JSON data is never parsed as markup.

**The JSON endpoint.** `GET /api/applications` returns the page and the filtered total. Cookie auth normally redirects to the login page, which would hand a `fetch` a page of HTML with a 200; for `/api` paths the app returns **401** and **403** instead, so the grid can react to being signed out. Invalid parameters (an unknown sort or status, `pageSize` outside 1–100, `page` below 1) return **400** with problem details, and `pageSize` is capped so a caller can't request the whole table. `claimedBy` is computed only for managers, and sorting by it falls back to the submitted date for applicants, so it can't leak through the sort order either. The endpoint is documented with OpenAPI at `/openapi/v1.json`, browsable at `/scalar` outside production; the descriptions come from XML doc comments on the API controller and its models, which is why those files keep them.

**Manager notes.** Notes are their own aggregate rather than part of the application, so loading an application never loads them and an applicant page cannot leak one. The notes endpoints and panel are behind the same `ManageNotes` authorization check, and the author and edit times come from the audit columns rather than being stored again.

**Saving sections with errors.** Behind `Features:SaveInvalidSections`, Continue saves a section that fails validation instead of rejecting it. The switch decides what may be *saved*, never what counts as *valid*: the rules run again when the summary lists what's blocking submission and again when Submit is attempted, so an application saved with errors can't be submitted, even after the switch is turned back off. The one thing the switch can't relax is a value too long for its column — those rules carry `BlocksSaving`, and the entity rejects them whatever the caller asks for. The rules live in `Core/Validation` and are used by the form, the entity, the summary and Submit, so there's one definition of what a complete section looks like.

**Stale saves.** Two applicants can edit one application at once, so each save carries the version it was loaded with: the applicant's row version for their own section, and a section version for the shared residence list. A save from a stale page is rejected with a message to reload rather than overwriting someone else's work.

**Expected failures aren't exceptions.** Services return a `ServiceResult`, so a broken rule, a stale save or a deadlock becomes a message a page can show.

**Testing.** Domain rules are covered by fast unit tests with no database. The services are covered by integration tests against a real LocalDB database, because what's worth proving there (concurrency tokens, the serializable approval transaction, EF includes) only behaves correctly against real SQL Server.

**Browser tests.** A small Playwright suite (`tests/PropertyManagement.BrowserTests`) covers only what needs JavaScript: the applicant journey with the residence modal, the review modal's approve/deny rules, the shared modal pattern on Properties, the reusable grid's paging/sorting/filtering and URL state, and saving sections with errors. The app runs on a real Kestrel port against its own temporary database, and Chromium is installed on the first run. Everything else is proven faster by the integration tests.

**Logging.** Services log each status change (started, submitted, withdrawn, claimed, released, returned, denied, approved with the lease start date) at `Information`, and refused changes, stale saves, approval deadlocks and failed sign-ins at `Warning`. Entries carry ids only, never names, addresses or incomes. The domain entities don't log; the services log the outcome. EF Core's per-command SQL logging is set to `Warning` so these events are readable.