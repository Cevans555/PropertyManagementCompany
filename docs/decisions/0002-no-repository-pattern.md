# 0002: EF Core is used directly, without a repository layer

## Context
A common layout wraps EF Core in `IRepository<T>` or per-entity repositories. EF Core's `DbContext` is already a unit of work, and each `DbSet` is already a repository.

## Decision
Services and query classes use `PropertyManagementDbContext` directly. There is no generic repository, no MediatR and no CQRS infrastructure.

Reads that are reusable, complex or shaped for a page go into named query classes (see [0003](0003-queries-in-data-vs-web.md)). A small one-off lookup, like a single `AnyAsync` inside a service, stays where it is used.

## Alternatives considered
- **Generic repository.** It either hides `IQueryable` and loses database-side filtering, paging and projection, or it exposes `IQueryable` and adds nothing. It also invites loading whole entities for reads.
- **Per-entity repositories.** Methods like `GetById`, `Add` and `Update` duplicate `DbSet` and grow a method for every query shape.
- **MediatR and CQRS.** A handler per request adds a layer of indirection with no second front end or pipeline behaviour to justify it.

## Consequences
- Queries use `Include`, `AsSplitQuery`, projections, `ExecuteUpdate` and transactions directly, where they're needed.
- The integration tests run against real SQL Server rather than mocking a repository, which is also where concurrency and transaction behaviour can actually be proven.
- Switching ORMs would touch services and queries. That trade-off is accepted, since it isn't a realistic requirement here.
