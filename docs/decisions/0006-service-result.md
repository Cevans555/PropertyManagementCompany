# 0006: Expected failures are results, not exceptions

## Context
Many failures are a normal part of the workflow: submitting with an incomplete section, a unit leased in the meantime, a stale page, losing an approval race, removing a unit that has applications. The user needs a clear message for each, not an error page.

## Decision
Services return `ServiceResult` or `ServiceResult<T>`, which is either success or an error message.

- Entities throw `DomainException` for broken rules. `ApplicationUpdater` and the services catch it and return a failed result.
- `StaleDataException`, `DbUpdateConcurrencyException` and SQL deadlocks are also translated into failed results, each with a message telling the user what to do.
- After a failure the change tracker is cleared, so a half-applied change can't be saved by a later call in the same request.
- Controllers turn a result into a response. Modals re-render the form with the error (`ModalResults.ToModalResult`, returning 422), and pages redisplay with the error or show a flash message.

Unexpected failures, such as a lost database connection, still throw and reach the global exception handler.

## Alternatives considered
- **Throwing all the way to the controller.** Every action would need try/catch blocks, and it's easy to miss one and show an error page for an ordinary situation.
- **Exception middleware mapping exception types to responses.** This works for APIs, but here the same failure must re-render a specific form with the user's input kept.
- **A result library.** One small type covers the need.

## Consequences
- Controller code reads as "call, then branch on `Succeeded`".
- Message constants such as `ApplicationUpdater.StaleDataMessage` are public, so tests assert on the exact text.
- Refused changes are logged as warnings in one place, `ApplicationUpdater`.
