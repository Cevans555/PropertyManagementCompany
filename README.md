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

## Solution structure
```
PropertyManagement.slnx
├─ src/
│  ├─ PropertyManagement.Web    MVC app: startup, controllers, views
│  ├─ PropertyManagement.Core   entities and business rules (no database or web code)
│  └─ PropertyManagement.Data   DbContext, migrations, seeding
└─ tests/
   ├─ PropertyManagement.Tests             unit tests for Core (no database)
   └─ PropertyManagement.IntegrationTests  services against a real LocalDB database
```
References point toward Core: Web → Core, Data; Data → Core; Tests → Core.

## Design notes

**Rich entities, thin services.** `RentalApplication` is the aggregate root and enforces the rules; nothing can change an application except through its methods. Services load it, answer the questions it can't (such as whether a unit is already leased), call the method and save.

**Queries vs services.** `Data/Queries` holds reusable or specialized reads, like the lease availability checks shared by starting, submitting and approving. Services own saving and transactions. A one-line existence check inside a service stays there rather than becoming a class of its own.

**Modal pattern.** Create, edit and remove happen in one shared modal in the layout, driven by `wwwroot/js/modal.js`:

- A trigger carries `data-modal-url` (the partial to load) and `data-modal-refresh` (the page sections to reload afterwards).
- `GET` returns a form partial. A failed `POST` returns **422** with the same partial re-rendered with its validation errors, so the modal updates in place. A successful `POST` returns JSON, so the modal closes and each refresh target reloads from its own `data-refresh-url`.
- Controllers express that with two helpers, `ModalInvalid` and `ModalSuccess`, and view components (`PropertyList`, `UnitTable`) both render a section and serve its refresh URL, so the markup exists once.

**The application page.** One page shows one section at a time (applicant information, residence history, summary). A single form posts to a single action, and the clicked button decides what happens: Continue validates and saves the current section then moves on, Back never saves, and Submit is only accepted from the summary. The same section partials render editable or read-only from a server-side decision, so the summary reuses them rather than duplicating markup, and a manager viewing an application gets the read-only rendering of the same views.

**Review workflow.** A property manager claims a submitted application from the review queue before completing it, which moves it to Under Review and records who holds it. Only the manager holding it can complete or release the review; anyone else sees who has it. Completing a review is one modal with three outcomes: approve (which creates a 12-month lease from a chosen start date), return, or deny. Return and deny require a comment, and every outcome is recorded in the status history.

**The application list.** Role scoping and both filters are applied to the `IQueryable` before it runs, so SQL Server does the filtering rather than the app loading every application and narrowing it in memory. Applicant names are resolved in a second lookup instead of a join per row. The filter form is a `GET`, so a filtered list can be bookmarked and the back button behaves. `ApplicationListQuery` sits in `Web/Services` rather than `Data/Queries` because it takes a `ClaimsPrincipal` and projects straight into view models — a presentation query, the same reasoning as `ApplicationPageBuilder`.

**Manager notes.** Notes are their own aggregate rather than part of the application, so loading an application never loads them and an applicant page cannot leak one. The notes endpoints and panel are behind the same `ManageNotes` authorization check, and the author and edit times come from the audit columns rather than being stored again.

**Stale saves.** Two applicants can edit one application at once, so each save carries the version it was loaded with: the applicant's row version for their own section, and a section version for the shared residence list. A save from a stale page is rejected with a message to reload rather than overwriting someone else's work.

**Expected failures aren't exceptions.** Services return a `ServiceResult`, so a broken rule, a stale save or a deadlock becomes a message a page can show.

**Testing.** Domain rules are covered by fast unit tests with no database. The services are covered by integration tests against a real LocalDB database, because what's worth proving there (concurrency tokens, the serializable approval transaction, EF includes) only behaves correctly against real SQL Server.