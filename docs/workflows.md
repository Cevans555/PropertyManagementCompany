# Workflows

What each role does, step by step, and which code handles it.

## Applicant

1. **Browse available units.** `Applications/Available` renders the `AvailableUnits` view component. Units with a lease covering today aren't listed. If the applicant already has an open application for a unit, the button says **Continue application** instead of **Apply**.
2. **Start.** `POST Applications/Start` either reopens an existing open application for that unit or calls `RentalApplicationService.StartAsync`. That creates a Draft with the applicant as primary, and refuses if the unit is leased.
3. **Applicant information.** `Applications/Details/{id}?section=Applicant`. Name and email are pre-filled from the account.
   - **Continue** validates with `ApplicantDetailsRules` and saves only if the section is valid. The save checks the applicant's row version, then moves on to Residence history. If it's invalid, the page re-renders with 422 and the field errors.
4. **Residence history.** Residences are added, edited and removed in the modal (`ApplicationResidencesController`), and the list refreshes in place. Every change checks and renews `ResidenceSectionVersion`.
   - **Continue** saves the section (at least one residence) and moves on to the Summary.
   - **Back** returns to the previous section **without saving**. It posts with `formnovalidate`.
5. **Co-applicants (optional).** **Add co-applicant** (`ApplicationApplicantsController`) takes the email of an existing applicant account. Every applicant must then save their own applicant information before the application can be submitted.
6. **Summary.** A read-only view of both sections and a list of blockers, each with a **Fix** link to its section. **Submit** is disabled while any blocker remains.
7. **Submit.** `RentalApplicationService.SubmitAsync` checks the sections again, and that the unit still has no active lease. The application becomes Submitted, and every section turns read-only.
8. **Withdraw.** Available from the page header until the application reaches a terminal status. It uses a confirmation modal and reloads the page.
9. **After a Return.** The status is Returned, the sections are editable again, the manager's comment is visible, and the applicant corrects and submits again.

One form posts to one action: `ApplicationsController.Details` POST, with a `command` of `continue`, `back` or `submit`. Any command that isn't allowed for the current section returns 400. A POST to an application the user can't edit returns 403, or 404 if they can't view it.

## Property manager

1. **Home dashboard.** Shows how many applications are waiting and claimed, the number of properties and units available today, with links to the review queue, applications and properties.
2. **Properties and units.** `Properties` lists properties. Add, edit and remove happen in modals, and the list refreshes in place. A property's page shows its unit table: add, edit and remove units in modals (`UnitsController`). Inactive unit types appear only as the current choice on units that already use them. Removal is refused while applications or leases exist.
3. **Review queue.** `Reviews/Queue` shows three groups: Waiting, My reviews, and Being reviewed by others.
4. **Claim.** `POST Reviews/Claim/{id}` moves the application to Under Review for this manager. The application page then shows **Complete review** and **Release to queue**.
5. **Complete review.** The modal (`Reviews/Review/{id}`) offers Approve, Return or Deny. The lease start date field appears only for Approve, and a comment is required for Return and Deny.
   - **Approve** runs `ApplicationReviewService.ApproveAsync` in a serializable transaction, rejects a conflicting lease, and creates the 12-month lease.
   - **Return** and **Deny** record the comment in the status history.
6. **Manager notes.** A panel on the application page, visible only to managers. Add, edit and remove notes in modals, and the panel refreshes in place.
7. **Applications list.** A grid with status and property filters, sorting and paging, all done in SQL. Managers see every application and who has claimed it. Applicants see only their own, without the claimed-by column.

## The modal pattern

Every create, edit and remove uses one modal in `_Layout.cshtml`, driven by `wwwroot/js/modal.js`. A new modal needs no JavaScript.

1. Clicking an element with `data-modal-url` fetches that partial into the modal.
2. A form inside the modal is posted with `fetch`.
   - **422:** validation failed. The response is the same partial with errors, and it replaces the modal content. Client-side validation is wired up again.
   - **200 JSON:** success. The modal closes, then each selector in `data-modal-refresh` reloads from its own `data-refresh-url`. If the trigger has `data-modal-reload="true"`, the whole page reloads instead; the review and withdraw modals do this.
   - **Redirect to `/Account/`:** the session expired, so the whole window goes to login.
3. Inside a modal, `data-show-when="Field" data-show-value="Value"` shows an element only while that field has that value. The review form uses it for the lease start date.

**To add a modal:**
1. A form partial that renders its own header, body and footer.
2. A GET action returning `PartialView(...)`.
3. A POST action returning `this.ModalInvalid(partial, model)` for invalid input, or `this.ToModalResult(serviceResult, partial, model)` after calling the service.
4. The refreshable list as a view component, inside a wrapper with an `id` and a `data-refresh-url` that points at an action returning `ViewComponent(...)`. `UnitsController.Table` is an example.
5. A trigger with `data-modal-url` and `data-modal-refresh="#wrapper-id"`.
6. For deletes, reuse `Views/Shared/_DeleteConfirm.cshtml` with a `DeleteConfirmViewModel`.

## The application grid

`Applications/Index` renders `GridViewComponent` from a `GridViewModel`: a data URL, columns with a key, title, sort key and format (`Text`, `Date`, `Badge` or `Link`), a default sort, empty text and a filter form id. `grid.js` calls `GET /api/applications` with `page`, `pageSize`, `sort`, `direction` and the filter form's fields. The endpoint returns `{ items, totalCount, page, pageSize }`.

- State lives in the URL, so refresh, bookmarks and Back/Forward all work.
- Cells are written with `textContent`.
- Invalid parameters return 400 problem details. A page past the end is clamped to the last page.
- Every sort ends with `Id`, so rows never move between pages.

**To reuse the grid for another list:** write a query returning `PagedResult<TRow>` (use `ApplicationListQuery` as the model), an API action with a validated request model and `ProducesResponseType` attributes, a `GridViewModel` describing the columns, and `@await Component.InvokeAsync("Grid", new { grid = ... })`.
