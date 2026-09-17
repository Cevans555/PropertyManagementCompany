# 0001: Business rules live in the entities

## Context
Most of the assessment is workflow. An application moves between statuses, and each move has conditions: who may do it, in which status, and whether both sections are saved or the unit is already leased. Those rules have to hold no matter which controller, service or seed path triggers the change.

## Decision
`RentalApplication` is the aggregate root, and it enforces its own rules. Its setters are private, and state changes only through methods such as `Submit`, `Claim`, `Return`, `Deny`, `Approve` and `SaveApplicantDetails`. A broken rule throws a `DomainException`. Applicants and residence history are changed only through the root. `Property` owns its units the same way, including the unit-type active rule.

The model is rich where the rules are, not everywhere. Lookups such as `UnitType` and the audit columns stay plain.

## Alternatives considered
- **Anemic entities with services holding the rules.** Rules spread across services, and nothing stops a new code path from skipping a check.
- **Full DDD** (domain events, a separate domain project, value objects everywhere). This is more structure than an assessment-sized domain needs.

## Consequences
- The rules are unit tested directly (`tests/PropertyManagement.Tests`), with no database.
- Services stay thin. They load the aggregate, pass in facts the entity can't look up itself (such as `unitHasActiveLease`), call one method and save.
- The seeder builds applications in every status through the same methods, so seeded data can't be in a state the app would reject.
- The entity can't query the database. Anything that needs one, like "is this unit leased?", is asked by the service and passed in.
