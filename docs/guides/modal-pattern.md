# Adding a modal

Create, edit and remove actions all use one modal in `_Layout.cshtml` (`#app-modal`), driven by `wwwroot/js/modal.js`. A new modal needs no JavaScript.

## How it works
1. A click on any element with `data-modal-url` fetches that URL and puts the returned partial into the modal.
2. A form submitted inside the modal is posted with `fetch`.
3. **422** means validation failed. The response is the same partial with its errors, and it replaces the modal content in place. Client-side validation is wired up again for the new form.
4. **200 with JSON** means success. The modal closes, then either:
   - each selector in `data-modal-refresh` is reloaded from that element's own `data-refresh-url`, or
   - the whole page reloads, if the trigger has `data-modal-reload="true"`. The review and withdraw modals do this because they change the page header and status.
5. If the session has expired, a redirect to `/Account/` sends the whole window to the login page.

Inside a modal, `data-show-when="FieldName" data-show-value="Value"` shows an element only while that field has that value. The review form uses it to show the lease start date only for Approve.

## Steps
1. **A form partial** in the controller's view folder. It renders its own `modal-header`, `modal-body` and `modal-footer`, posts to the action, and includes a validation summary.
2. **A GET action** that returns `PartialView("_YourForm", model)`.
3. **A POST action** that returns:
   - `this.ModalInvalid("_YourForm", model)` when `ModelState` is invalid (422)
   - `this.ToModalResult(result, "_YourForm", model)` after calling the service. This gives JSON on success, or the form with the service's error on failure.
4. **A refreshable section.** Put the list in a view component, render it inside a wrapper with an id and a refresh URL, and add an action that returns the view component:
   ```cshtml
   <div id="unit-table" data-refresh-url="@Url.Action("Table", "Units", new { id = Model.Id })">
       @await Component.InvokeAsync("UnitTable", new { propertyId = Model.Id })
   </div>
   ```
   ```csharp
   [HttpGet]
   public IActionResult Table(int id)
   {
       return ViewComponent("UnitTable", new { propertyId = id });
   }
   ```
5. **The trigger:**
   ```cshtml
   <button type="button" class="btn btn-primary"
           data-modal-url="@Url.Action("Create", "Units", new { propertyId = Model.Id })"
           data-modal-refresh="#unit-table">
       Add unit
   </button>
   ```
6. **For a delete,** reuse `Views/Shared/_DeleteConfirm.cshtml` with a `DeleteConfirmViewModel` (title, message, post URL, and optional confirm text).

## Testing
- An integration test posts an invalid form and asserts **422** with the partial and no layout (see `Create_InvalidForm_Returns422WithErrorsInPartial`).
- A browser test can use `MarkPageAsync` and `AssertNotReloadedAsync` to prove the section refreshed without a full reload.
