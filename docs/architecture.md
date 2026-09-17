# Architecture overview

## Projects

| Project | Holds | Depends on |
| --- | --- | --- |
| `PropertyManagement.Core` | Entities, business rules, validation rules, value objects, DTOs | nothing |
| `PropertyManagement.Data` | `PropertyManagementDbContext`, EF configurations, migrations, seeding, auditing, services, data queries | Core |
| `PropertyManagement.Web` | Controllers, views, view components, view models, presentation queries, authorization, startup | Data, Core |

```mermaid
flowchart LR
    Web --> Data
    Web --> Core
    Data --> Core
    Tests[PropertyManagement.Tests] --> Core
    Tests --> Web
    Integration[PropertyManagement.IntegrationTests] --> Web
    Integration --> Data
    Integration --> Core
    Browser[PropertyManagement.BrowserTests] --> Integration
```

Core has no reference to EF Core or ASP.NET Core, so the rules can be unit tested without a database or a web server.

There is deliberately no separate application layer. The use cases (start, submit, claim, approve and so on) are services in Data, next to the `DbContext` they coordinate. With one front end, another project would add indirection without separating anything that changes independently.

## A write

```mermaid
sequenceDiagram
    participant C as Controller
    participant A as Authorization
    participant S as Service
    participant U as ApplicationUpdater
    participant E as RentalApplication
    participant DB as SQL Server
    C->>A: may this user attempt it?
    C->>S: e.g. SubmitAsync(id, userId)
    S->>U: UpdateAsync(id, change)
    U->>DB: load the aggregate
    S->>DB: ask what the entity can't know (does the unit have an active lease?)
    U->>E: application.Submit(...)
    E-->>U: rule broken → DomainException
    U->>DB: SaveChanges (concurrency tokens checked)
    U-->>S: ServiceResult
    S-->>C: ServiceResult (success, or a message to show)
```

- **Authorization** answers "may this user attempt this?" ([0007](decisions/0007-authorization.md)).
- **The entity** answers "is it valid now?" and changes its own state ([0001](decisions/0001-rich-domain-model.md)).
- **The service** supplies facts the entity can't look up, saves, and turns expected failures into a `ServiceResult` ([0006](decisions/0006-service-result.md)).
- **`ApplicationUpdater`** keeps load → change → save → translate errors in one place, including stale saves and deadlocks ([0005](decisions/0005-concurrency.md)).
- **The audit interceptor** stamps who changed what and when, so no service sets audit columns by hand.

## A read

```mermaid
sequenceDiagram
    participant C as Controller / view component / API
    participant Q as Query class
    participant DB as SQL Server
    C->>Q: e.g. ListAsync(user, filter, paging)
    Q->>DB: filter, sort, page and project in one query
    DB-->>Q: only the rows and columns needed
    Q-->>C: view models
```

Reads don't load entities. They project straight into the shape a page needs, with `AsNoTracking`, so filtering, sorting and paging happen in SQL ([0002](decisions/0002-no-repository-pattern.md), [0003](decisions/0003-queries-in-data-vs-web.md)).

The one exception is the application page. It loads the aggregate without tracking, because the authorization handler and the editable-or-read-only decision both need the real entity.

## Pages and partial rendering
- Pages are server-rendered Razor. JavaScript is limited to `modal.js` (the shared modal) and `grid.js` (the reusable grid).
- Sections that refresh in place are view components with their own refresh URL, so the same markup renders on first load and after a change ([modal guide](guides/modal-pattern.md)).
- The application page is one view model and one form. The clicked button decides what the POST does.
