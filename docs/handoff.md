# Handoff

For someone picking this project up with no prior context: where it stands, what not to break, and where to make changes.

## Current state

- **`main` is the submitted technical assessment.** It implements the whole specification and all five bonuses: the grid with its API and OpenAPI, claim and release, manager notes, save-invalid sections, and co-applicants with concurrency.
- **Build and tests:** the build has no warnings, and the unit, integration and browser suites all pass.
- **Known limitations and deliberate omissions:** see below. None are unfinished work; each was a scope decision.
- **The `extras` branch** holds improvements beyond the specification and is kept out of `main`, so `main` remains the submitted assessment. It uses its own database (`PropertyManagementCompany_Extras`). Its README lists each extra. So far that's a warning when an applicant already holds a lease, and browser coverage of every screen. New experiments belong there, not on `main`.

## Getting oriented (about 30–45 minutes)

1. [README](../README.md): run the app, and sign in as `manager1@demo.com` and `applicant1@demo.com`.
2. Walk through one application end to end as both roles ([workflows](workflows.md)).
3. [architecture](architecture.md), then [domain](domain.md).
4. Read `Core/Entities/RentalApplication.cs`. Most of the rules are there.
5. Read `Data/Services/ApplicationUpdater.cs` and `ApplicationReviewService.ApproveAsync`.
6. Read `Web/Controllers/ApplicationsController.cs` (the one-form, one-action page) and `Web/Services/ApplicationPageBuilder.cs`.
7. [decisions](decisions.md) and [testing](testing.md).

## Don't accidentally break

| Invariant | Why it matters | Where |
| --- | --- | --- |
| Application lifecycle rules live in `RentalApplication` | Every code path, including the seeder, goes through the same checks | `Core/Entities/RentalApplication.cs` |
| Entities don't query the database; services ask and pass the answer in | Keeps Core free of EF and unit testable | services in `Data/Services` |
| Never bypass resource authorization for an application | Applicants may only reach applications they're on; ids must not leak (404, not 403) | `ApplicationPageBuilder.AuthorizeAsync`, `RentalApplicationAuthorizationHandler` |
| The UI flags come from the same authorization checks | Buttons must match what the server allows | `ApplicationPageBuilder.BuildAsync` |
| Approval keeps its serializable check-then-insert and deadlock handling | Otherwise two approvals can create two leases for one unit | `ApplicationReviewService.ApproveAsync`, `ApplicationUpdater.IsDeadlock` |
| Concurrency stays per section (applicant row version, residence section version) | Co-applicants must be able to save different sections at once, while same-section conflicts are rejected | `SaveApplicantDetailsAsync`, residence methods, EF configurations |
| Manager notes never appear in applicant-facing reads | Bonus 3: never rendered or returned. Don't add a notes navigation to `RentalApplication` or notes to any applicant projection or API | `ManagerNote`, `ManagerNotesController`, `ApplicationListItem` |
| An inactive unit type stays on existing units but can't be newly selected | Enforced in the domain, not just the dropdown | `Unit`, `UnitQueries` |
| Bonus 4 changes what may be saved, never what counts as valid | Validity is recalculated at the Summary and at Submit | `Core/Validation`, `ApplicationsController`, `RentalApplication.Submit` |
| All date rules use `timeProvider.Today()` | "Today" is the business date, not the UTC date | anywhere a date is compared |
| `options.Stores.MaxLengthForKeys = 128` stays in `Program.cs` | The migration was generated with it; removing it makes the model differ and the app refuses to start | `Program.cs`, `TestDatabase` |
| The connection string is read lazily inside `AddDbContext` | Otherwise tests silently run against the development database | `Program.cs` |
| `main` is the submitted assessment | Put experiments on `extras` | branches |

## Where do I change X?

| To change… | Look in |
| --- | --- |
| Application lifecycle rules, status transitions | `Core/Entities/RentalApplication.cs` |
| Section validation rules (applicant fields, residences) | `Core/Validation` |
| Lease term or start-date rules | `Core/Entities/Lease.cs` |
| Unit and property rules (unit numbers, rent, unit types) | `Core/Entities/Property.cs`, `Unit.cs` |
| A database-backed business question (is it leased? does it conflict?) | `Data/Queries` |
| Workflow orchestration, transactions, logging of events | `Data/Services` |
| Stale-save or deadlock handling, and their messages | `Data/Services/ApplicationUpdater.cs` |
| Who can access something | `Web/Authorization` (handler and policies) and the controller's `[Authorize]` |
| What a page or list shows | `Web/Queries` and the matching view model in `Web/Models` |
| How the application page is assembled | `Web/Services/ApplicationPageBuilder.cs`, `Views/Applications/_*Section.cshtml` |
| Continue, Back and Submit behaviour | `ApplicationsController.Details` (POST) |
| The review modal, claim and release | `Web/Controllers/ReviewsController.cs`, `Views/Reviews/_ReviewForm.cshtml` |
| The application list columns or filters | `ApplicationListPageViewModel.Grid(...)`, `Queries/Applications/ApplicationListQuery.cs`, `Models/Api` |
| The shared modal behaviour | `wwwroot/js/modal.js`, `Infrastructure/ModalResults.cs` |
| Styling and theme colours | `wwwroot/css/site.css` (CSS variables at the top) |
| Seed data | `Data/Seeding` |
| The database schema | the entity and `Data/Configurations`, then `dotnet ef migrations add <Name> --project src/PropertyManagement.Data --startup-project src/PropertyManagement.Web` |
| Feature switches, the business time zone, the connection string | `appsettings.json` |

## Common tasks

**Add a schema change.** Change the entity and its configuration, then add a migration with the command above. It applies automatically on the next start. The integration tests also migrate their own database, so a model the migration doesn't match fails immediately.

**Add a modal.** Follow [workflows → the modal pattern](workflows.md#the-modal-pattern). No JavaScript is needed.

**Add another grid.** Follow [workflows → the application grid](workflows.md#the-application-grid).

**Add a status or workflow step.** Update `ApplicationStatus` and its extensions, add it to the `HasData` in `ApplicationStatusTypeConfiguration` with a migration (it's a lookup table), add the method on `RentalApplication`, then the service method, authorization operation, controller action and badge class (`StatusBadges`), and tests at each level.

## Known limitations and deliberate omissions

- **Anyone can register as a property manager.** The specification asks for role choice at sign-up as a convenience. A real system would invite managers instead.
- **Demo accounts with a known password are seeded in every environment,** because the specification requires seeding on start. A real deployment would seed demo users only outside production.
- **No email confirmation, password reset or account management pages.** Identity supports them; they weren't in scope.
- **Unit types are switched between active and inactive only through seed data.** There's no admin screen.
- **One company-wide business time zone,** not one per property.
- **No real-time updates between co-applicants.** Conflicts are detected when saving, which the specification allows.
- **The property list and review queue aren't paged.** Only the application list uses the grid.
- **Logging goes to the default console providers only.**
- **No CI pipeline or container setup.**
- **The setup assumes SQL Server LocalDB on Windows.** Any SQL Server works by changing `DefaultConnection`.

## Conventions

- One type per file. Block-bodied methods. Explicit constructors. Member order: constants, fields, properties, constructors, methods.
- Services return `ServiceResult`; controllers branch on `Succeeded`.
- No controller or view component injects the `DbContext` for reads; use a query class.
- Comments only explain a non-obvious local constraint. The README says how to run the project, and `docs/` says why it's designed this way.
- Branches: one pull request per change into `main`, merged with a merge commit. Experiments go on `extras`.
