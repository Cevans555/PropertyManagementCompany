# Documentation

The main [README](../README.md) covers setup and a summary of the design. These pages explain the reasoning behind the design in more depth.

## Architecture
- [Architecture overview](architecture.md): the projects, which way references point, and how a write and a read flow through the app

## Decisions
Each record explains the context, the decision, the alternatives considered and what follows from it.

| # | Decision |
| --- | --- |
| [0001](decisions/0001-rich-domain-model.md) | Business rules live in the entities |
| [0002](decisions/0002-no-repository-pattern.md) | EF Core is used directly, without a repository layer |
| [0003](decisions/0003-queries-in-data-vs-web.md) | Where a query class lives follows what it returns |
| [0004](decisions/0004-manager-note-aggregate.md) | Manager notes are their own aggregate |
| [0005](decisions/0005-concurrency.md) | Concurrency: row versions, a section version and a serializable approval |
| [0006](decisions/0006-service-result.md) | Expected failures are results, not exceptions |
| [0007](decisions/0007-authorization.md) | Authorization in three layers, and not-found instead of forbidden |
| [0008](decisions/0008-business-time-zone.md) | "Today" comes from a business time zone |
| [0009](decisions/0009-save-invalid-sections.md) | The save-invalid switch changes what may be saved, never what is valid |
| [0010](decisions/0010-testing-strategy.md) | Unit, integration and browser tests each prove something different |

## Guides
- [Adding a modal](guides/modal-pattern.md)
- [Reusing the grid](guides/grid.md)
