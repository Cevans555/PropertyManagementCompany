# 0003: Where a query class lives follows what it returns

## Context
Once reads are pulled out of controllers into query classes, each one needs a home. References only point one way, Web → Data → Core, so Data can't return Web's view models.

## Decision
- **`Data/Queries`** answers questions about the data itself and returns plain values or entities. Examples: `LeaseQueries` ("does this unit have an active lease?", "does a lease conflict with this term?"), `ApplicationQueries` and `ManagerNoteQueries`. Services and controllers both use them.
- **`Web/Queries`** projects straight into view models for a page. Examples: `PropertyQueries`, `UnitQueries`, `ReviewQueries`, `HomeQueries`, `ApplicationSectionQueries`, and `Applications/ApplicationListQuery`, which also takes a `ClaimsPrincipal` to scope rows by role.
- Query classes are grouped by concept, never one class per SQL statement.
- A small one-off lookup can stay in the service or controller that uses it.

## Alternatives considered
- **All queries in Data.** Every page-shaped read would need a parallel DTO in Data plus a mapping into the view model. With one front end, that doubles the types for no benefit.
- **Queries inline in controllers and view components.** Controllers would mix request handling with data access, and reused reads would be duplicated.

## Consequences
- No controller or view component injects the `DbContext` for reads.
- A read's location tells you its purpose: domain question or page shape.
- If a second front end ever appeared, the Web queries would be the part to split into DTOs.
