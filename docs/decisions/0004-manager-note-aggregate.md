# 0004: Manager notes are their own aggregate

## Context
Bonus 3 says that notes are visible and editable only to property managers and are "never rendered or returned to an applicant". The application page is shared by applicants and managers.

## Decision
`ManagerNote` is a separate aggregate with a foreign key to `RentalApplication` and no navigation property on the application. `RentalApplication` has no notes collection.

- Notes go through `ManagerNoteService` and `ManagerNoteQueries`.
- `ManagerNotesController` requires the property manager policy, and every action also checks the `ManageNotes` operation against the application.
- The panel renders only when `CanManageNotes` is true, and it loads through its own view component.
- The author and edit times come from the audit columns rather than being stored again.

## Alternatives considered
- **A `Notes` collection on `RentalApplication`.** Any `Include` or future serialization of the application could carry notes into an applicant response. Keeping notes out is then a rule someone has to remember in every read.
- **Filtering notes in the view.** This hides them in the HTML, but the data would still be loaded, and one mistake in a view or the JSON would expose it.

## Consequences
- Loading an application never loads notes, so no applicant page or API response can leak one by accident.
- Adding or removing a note doesn't touch the application's concurrency tokens, so a manager writing a note can't make an applicant's save stale.
- The application's rules don't cover notes. A note's rules are only "not empty" and "within the length limit", so nothing is lost.
- Tests confirm the note text is absent from the applicant's page and JSON.
