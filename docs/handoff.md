# Handoff

Where the project stands, what not to break, and where to make changes.

## Current state

- **`main` is the submitted assessment:** the full specification and all five bonuses.
- **The `extras` branch** holds improvements beyond the specification and is kept out of `main`. It uses its own database (`PropertyManagementCompany_Extras`), and its README lists each extra.
- **Limitations:** the items listed at the end of this page are scope decisions, not unfinished work.

## Getting oriented

1. Run the app ([README](../README.md)).
2. Walk one application end to end:
   1. As `applicant1@demo.com`, browse available units, apply, fill in applicant information, add a residence in the modal, and submit from the summary.
   2. As `manager1@demo.com`, claim it from the review queue, return it with a comment, and add a manager note.
   3. As the applicant, correct it and resubmit.
   4. As the manager, claim it again and approve it with a start date.
3. Read [architecture](architecture.md).
4. Read `Core/Entities/RentalApplication.cs`, where most of the rules are.
5. Read `Data/Services/ApplicationUpdater.cs` and `ApplicationReviewService.ApproveAsync`.
6. Read `Web/Controllers/ApplicationsController.cs` and `Web/Services/ApplicationPageBuilder.cs`.
7. Read [decisions](decisions.md) and [testing](testing.md).

## Don't accidentally break

| Invariant | Where |
| --- | --- |
| Application lifecycle rules live in `RentalApplication`; entities don't query the database, services ask and pass the answer in | `Core/Entities`, `Data/Services` |
| Never bypass resource authorization for an application, and keep 404 for applications a user can't view | `ApplicationPageBuilder.AuthorizeAsync`, `RentalApplicationAuthorizationHandler` |
| Approval keeps its serializable check-then-insert and deadlock handling | `ApplicationReviewService.ApproveAsync`, `ApplicationUpdater` |
| Concurrency stays per section, so co-applicants can save different sections at once | `Applicant` row version, `ResidenceSectionVersion` |
| Manager notes never reach applicant-facing pages, projections or the API; no notes navigation on `RentalApplication` | `ManagerNote`, `ManagerNotesController`, `Models/Api` |
| An inactive unit type stays on existing units but can't be newly selected | `Unit`, `UnitQueries` |
| Bonus 4 changes what may be saved, never what counts as valid | `Core/Validation`, `RentalApplication.Submit` |
| Date rules use `timeProvider.Today()`, not the UTC date | anywhere dates are compared |
| `options.Stores.MaxLengthForKeys = 128` stays; the migration was generated with it | `Program.cs` |
| The connection string is read lazily in `AddDbContext`, so tests can point the app at their own database | `Program.cs` |

## Where do I change X?

| To change… | Look in |
| --- | --- |
| Lifecycle rules and status transitions | `Core/Entities/RentalApplication.cs` |
| Section validation rules | `Core/Validation` |
| Lease term and start-date rules | `Core/Entities/Lease.cs` |
| Unit and property rules | `Core/Entities/Property.cs`, `Unit.cs` |
| A database-backed business question | `Data/Queries` |
| Workflow orchestration and transactions | `Data/Services` |
| Stale-save and deadlock handling | `Data/Services/ApplicationUpdater.cs` |
| Who can access something | `Web/Authorization`, controller `[Authorize]` attributes |
| What a page or list shows | `Web/Queries`, `Web/Models` |
| Application page composition | `Web/Services/ApplicationPageBuilder.cs`, `Views/Applications/_*Section.cshtml` |
| Continue, Back and Submit | `ApplicationsController.Details` (POST) |
| Review modal, claim and release | `ReviewsController`, `Views/Reviews/_ReviewForm.cshtml` |
| Application list columns and filters | `ApplicationListPageViewModel.Grid(...)`, `Queries/Applications/ApplicationListQuery.cs` |
| Theme colours | `wwwroot/css/site.css` (variables at the top) |
| Seed data | `Data/Seeding` |
| Schema | the entity and `Data/Configurations`, then `dotnet ef migrations add <Name> --project src/PropertyManagement.Data --startup-project src/PropertyManagement.Web` |

## Common tasks

**Add a modal (no JavaScript needed):**
1. A form partial that renders its own header, body and footer.
2. A GET action returning `PartialView(...)`.
3. A POST action returning `this.ModalInvalid(partial, model)` when input is invalid, or `this.ToModalResult(result, partial, model)` after calling the service.
4. Put the list being refreshed in a view component, inside a wrapper with an `id` and a `data-refresh-url` pointing at an action that returns `ViewComponent(...)` (see `UnitsController.Table`).
5. Give the trigger `data-modal-url` and `data-modal-refresh="#wrapper-id"`. Deletes reuse `_DeleteConfirm.cshtml`.

**Reuse the grid:**
1. A query returning `PagedResult<TRow>` that filters, sorts and pages in SQL, with every sort ending on a unique key. Model it on `ApplicationListQuery`.
2. An API action with a validated request model and `ProducesResponseType` attributes.
3. A `GridViewModel` describing the columns (`Text`, `Date`, `Badge` or `Link`).
4. `@await Component.InvokeAsync("Grid", new { grid = ... })`.

**Add a status:**
1. Update `ApplicationStatus` and its extensions.
2. Add it to the `HasData` in `ApplicationStatusTypeConfiguration`, with a migration.
3. Add the method on `RentalApplication`.
4. Add the service method, authorization operation, controller action and `StatusBadges` class.
5. Add tests at each level.

## Known limitations

- **Anyone can register as a property manager.** The specification asks for role choice at sign-up; a real system would invite managers instead.
- **Demo accounts with a known password are seeded in every environment,** because the specification requires seeding on start.
- **No email confirmation, password reset or account management pages.**
- **Unit types are switched between active and inactive only through seed data.**
- **One company-wide business time zone.**
- **No real-time updates between co-applicants.** Conflicts are detected when saving.
- **The property list and review queue aren't paged.**
- **No CI pipeline.** The setup assumes LocalDB; any SQL Server works by changing `DefaultConnection`.

## Conventions

- One type per file, block-bodied methods, explicit constructors.
- Services return `ServiceResult`.
- Reads go through query classes, not the `DbContext` in controllers.
- The README explains how to run the project, `docs/` explains why it's designed this way, and code comments explain one non-obvious local constraint.
- Changes reach `main` through pull requests; experiments go on `extras`.
